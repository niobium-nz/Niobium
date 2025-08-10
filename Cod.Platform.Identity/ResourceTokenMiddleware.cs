using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Net;
using System.Web;

namespace Cod.Platform.Identity
{
    internal sealed class ResourceTokenMiddleware(
        ISignatureService signatureService,
        PrincipalParser tokenHelper,
        IOptions<IdentityServiceOptions> options)
        : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            HttpRequest req = context.Request;
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

            if (!req.Query.TryGetValue("type", out Microsoft.Extensions.Primitives.StringValues typeString)
                || typeString.Count == 0
                || !int.TryParse(typeString.Single(), out int type)
                || !req.Query.TryGetValue("resource", out Microsoft.Extensions.Primitives.StringValues resource)
                || resource.Count == 0
                || string.IsNullOrWhiteSpace(resource.Single()))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            string res = HttpUtility.UrlDecode(resource.Single())!;
            req.Query.TryGetValue("partition", out Microsoft.Extensions.Primitives.StringValues partition);
            req.Query.TryGetValue("id", out Microsoft.Extensions.Primitives.StringValues id);

            System.Security.Claims.ClaimsPrincipal principal = await tokenHelper.ParseAsync(req, context.RequestAborted);
            if (principal == null)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return;
            }

            StorageSignature result = await signatureService.IssueAsync(principal, (ResourceType)type, res, partition.SingleOrDefault(), id.SingleOrDefault());
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            await context.Response.WriteAsJsonAsync(result, cancellationToken: context.RequestAborted);
        }
    }
}
