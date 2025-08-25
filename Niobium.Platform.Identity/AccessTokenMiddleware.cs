using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Niobium.Identity;
using System.Data;
using System.Net;
using System.Security.Claims;

namespace Niobium.Platform.Identity
{
    internal sealed class AccessTokenMiddleware(
        Lazy<IRepository<Role>> repository,
        Lazy<IEnumerable<IEntitlementDescriptor>> descriptors,
        ITokenBuilder tokenBuilder,
        PrincipalParser principalParser,
        IOptions<IdentityServiceOptions> options) : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            HttpRequest req = context.Request;
            if (!req.Path.HasValue || !req.Path.Value.Equals($"/{options.Value.AccessTokenEndpoint}", StringComparison.OrdinalIgnoreCase))
            {
                await next(context);
                return;
            }

            ClaimsPrincipal principal = await principalParser.ParseIDPrincipalAsync(req, options.Value.IDTokenAudience, context.RequestAborted);
            if (principal == null)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return;
            }

            if (!principal.TryGetClaim(ClaimTypes.NameIdentifier, out Guid user))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return;
            }

            Guid tenant;
            if (req.Headers.TryGetValue(Niobium.Identity.Constants.TenantIDHeaderKey, out Microsoft.Extensions.Primitives.StringValues tenantHeader))
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

            List<string> roles = [options.Value.DefaultRole];
            Role? entity = await repository.Value.RetrieveAsync(Role.BuildPartitionKey(tenant), Role.BuildRowKey(user), cancellationToken: context.RequestAborted);
            if (entity != null)
            {
                roles.AddRange(entity.GetRoles());
            }

            List<EntitlementDescription> entitlements = [];
            IOrderedEnumerable<IEntitlementDescriptor> orderedDescriptors = descriptors.Value.OrderBy(d => d.IsHighOverhead);

            foreach (IEntitlementDescriptor? descriptor in orderedDescriptors)
            {
                foreach (string role in roles)
                {
                    if (descriptor.CanDescribe(tenant, user, role))
                    {
                        IEnumerable<EntitlementDescription> es = await descriptor.DescribeAsync(tenant, user, role);
                        entitlements.AddRange(es);
                    }
                }
            }

            List<KeyValuePair<string, string>> claims = [.. entitlements.Select(e =>
                new
                {
                    Resource = $"COD-{(int)e.Type}://{e.Resource}",
                    e.Permission
                })
                .GroupBy(e => e.Resource)
                .Select(e => new KeyValuePair<string, string>(e.Key, string.Join(Entitlements.ValueSplitor[0], e.Select(x => x.Permission))))];

            string sid = user.ToKey();
            string token = await tokenBuilder.BuildAsync(sid, claims, roles: roles);
            req.DeliverToken(token, AuthenticationScheme.BearerLoginScheme);
            context.Response.StatusCode = (int)HttpStatusCode.OK;
        }
    }
}
