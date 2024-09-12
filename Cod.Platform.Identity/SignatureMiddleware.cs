using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using System.Net;
using System.Web;

namespace Cod.Platform.Identity
{
    internal class SignatureMiddleware(
        ISignatureService signatureService,
        IdentityServiceOptions options)
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

            var parameters = new RouteValueDictionary();
            var templateMatcher = new TemplateMatcher(TemplateParser.Parse(options.ResourceSharedAccessSignatureEndpoint), []);
            if (!templateMatcher.TryMatch(req.Path.Value, parameters))
            {
                await next(context);
                return;
            }

            if (!parameters.ContainsKey("type")
                || !parameters.TryGetValueAsInt32("type", out var type)
                || !parameters.TryGetValueAsString("resource", out var resource)
                || string.IsNullOrWhiteSpace(resource))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            resource = HttpUtility.UrlDecode(resource);
            parameters.TryGetValueAsString("partition", out string? partition);
            parameters.TryGetValueAsString("id", out string? id);

            var principal = await req.TryParsePrincipalAsync();
            if (principal == null)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return;
            }


            var result = await signatureService.IssueAsync(principal, (ResourceType)type, resource, partition, id);
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            await context.Response.WriteAsJsonAsync(result);
        }
    }
}
