using Cod.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Data;
using System.Net;
using System.Security.Claims;

namespace Cod.Platform.Identity
{
    internal class AccessTokenMiddleware(
        Lazy<IRepository<Role>> repository,
        Lazy<IEnumerable<IEntitlementDescriptor>> descriptors,
        ITokenBuilder tokenBuilder,
        PrincipalParser principalParser,
        IOptions<IdentityServiceOptions> options) : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var req = context.Request;
            if (!req.Path.HasValue || !req.Path.Value.Equals($"/{options.Value.AccessTokenEndpoint}", StringComparison.OrdinalIgnoreCase))
            {
                await next(context);
                return;
            }

            var principal = await principalParser.ParseIDPrincipalAsync(req, context.RequestAborted);
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

            List<string> roles = [options.Value.DefaultRole];
            var entity = await repository.Value.RetrieveAsync(Role.BuildPartitionKey(tenant), Role.BuildRowKey(user), cancellationToken: context.RequestAborted);
            if (entity != null)
            {
                roles.AddRange(entity.GetRoles());
            }

            List<EntitlementDescription> entitlements = [];
            var orderedDescriptors = descriptors.Value.OrderBy(d => d.IsHighOverhead);

            foreach (var descriptor in orderedDescriptors)
            {
                foreach (var role in roles)
                {
                    if (descriptor.CanDescribe(tenant, user, role))
                    {
                        var es = await descriptor.DescribeAsync(tenant, user, role);
                        entitlements.AddRange(es);
                    }
                }
            }

            var claims = entitlements.Select(e =>
                new
                {
                    Resource = $"COD-{(int)e.Type}://{e.Resource}",
                    e.Permission
                })
                .GroupBy(e => e.Resource)
                .Select(e => new KeyValuePair<string, string>(e.Key, string.Join(Entitlements.ValueSplitor[0], e.Select(x => x.Permission)))).ToList();

            var sid = user.ToKey();
            var token = await tokenBuilder.BuildAsync(sid, claims, roles: roles);
            req.DeliverToken(token, AuthenticationScheme.BearerLoginScheme);
            context.Response.StatusCode = (int)HttpStatusCode.OK;
        }
    }
}
