using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Cod.Platform.Identity
{
    internal class AccessTokenMiddleware(
        Lazy<IRepository<Role>> repository,
        Lazy<IEnumerable<IEntitlementDescriptor>> descriptors,
        ITokenBuilder tokenBuilder,
        IdentityServiceOptions options,
        ILogger<AccessTokenMiddleware> logger) : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var req = context.Request;
            if (!req.Path.HasValue || req.Path.Value.Equals(options.AuthenticateEndpoint, StringComparison.OrdinalIgnoreCase))
            {
                await next(context);
                return;
            }

            var authHeader = req.Headers.Authorization.SingleOrDefault();
            if (!AuthenticationHeaderValue.TryParse(authHeader, out var value))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return;
            }

            if (value.Scheme != AuthenticationScheme.BearerLoginScheme)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            if (string.IsNullOrEmpty(options.IDTokenPublicKey))
            {
                logger.LogError("ID token public key must be configured.");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return;
            }

            var rsa = RSA.Create();
            rsa.ImportFromPem(options.IDTokenPublicKey);
            var key = new RsaSecurityKey(rsa);
            var principal = await HttpRequestExtensions.ValidateAndDecodeJWTAsync(value.Parameter, key, options.IDTokenIssuer, options.IDTokenAudience);
            if (principal == null)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return;
            }

            if (!principal.TryGetClaim<Guid>(ClaimTypes.NameIdentifier, out var user))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return;
            }

            Guid tenant;
            if (req.Headers.TryGetValue(Constants.TenantIDHeaderKey, out var tenantHeader))
            {
                if (!Guid.TryParse(tenantHeader, out tenant))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return;
                }
            }
            else
            {
                tenant = Guid.Empty;
            }

            List<string> roles = [Constants.DefaultRole];
            var entity = await repository.Value.RetrieveAsync(Role.BuildPartitionKey(tenant), Role.BuildRowKey(user));
            if (entity != null)
            {
                roles.AddRange(entity.GetRoles());
            }

            List<EntitlementDescription> entitlements = [];
            var orderedDescriptors = descriptors.Value.OrderBy(d => d.IsHighOverhead);
            List<string> describedRoles = [];

            foreach (var descriptor in orderedDescriptors)
            {
                foreach (var role in roles)
                {
                    if (descriptor.CanDescribe(tenant, user, role))
                    {
                        var es = await descriptor.DescribeAsync(tenant, user, role);
                        entitlements.AddRange(es);
                        describedRoles.Add(role);
                    }
                }

                roles.RemoveAll(describedRoles.Contains);
                if (roles.Count == 0)
                {
                    break;
                }
            }

            if (roles.Count != 0)
            {
                logger.LogError($"Unknown role(s) found: {string.Join(',', roles)}");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return;
            }

            var claims = entitlements.Select(e =>
                new
                {
                    Resource = $"{(int)e.Type}{Entitlements.ScopeSplitor[0]}{e.Resource}",
                    e.Permission
                })
                .GroupBy(e => e.Resource)
                .Select(e => new KeyValuePair<string, string>(e.Key, string.Join(Entitlements.ValueSplitor[0], e)));

            var token = await tokenBuilder.BuildAsync(
                user.ToKey(),
                claims: claims,
                symmetricSecurityKey: options.AccessTokenSecret,
                audience: options.IDTokenAudience,
                issuer: options.IDTokenAudience);
            req.DeliverAuthenticationToken(token, AuthenticationScheme.BearerLoginScheme);
            context.Response.StatusCode = (int)HttpStatusCode.OK;
        }
    }
}
