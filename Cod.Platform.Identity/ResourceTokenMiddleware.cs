using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Net;
using System.Web;

namespace Cod.Platform.Identity
{
    internal class ResourceTokenMiddleware(
        ISignatureService signatureService,
        IOptions<IdentityServiceOptions> options)
        : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var req = context.Request;
            if (!req.Path.HasValue)
            {
                await next(context);
                return;
            }

            if (!req.Path.HasValue || !req.Path.Value.Equals($"/{options.Value.ResourceTokenEndpoint}", StringComparison.OrdinalIgnoreCase))
            {
                await next(context);
                return;
            }

            if (!req.Query.TryGetValue("type", out var typeString)
                || typeString.Count == 0
                || !int.TryParse(typeString.Single(), out var type)
                || !req.Query.TryGetValue("resource", out var resource)
                || resource.Count == 0
                || string.IsNullOrWhiteSpace(resource.Single()))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            var res = HttpUtility.UrlDecode(resource.Single())!;
            req.Query.TryGetValue("partition", out var partition);
            req.Query.TryGetValue("id", out var id);

            var principal = await req.TryParsePrincipalAsync();
            if (principal == null)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return;
            }

            var result = await signatureService.IssueAsync(principal, (ResourceType)type, res, partition.SingleOrDefault(), id.SingleOrDefault());
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            await context.Response.WriteAsJsonAsync(result);
        }
    }
}
