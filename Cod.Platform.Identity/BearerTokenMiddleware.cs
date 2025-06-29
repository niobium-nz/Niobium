using Microsoft.AspNetCore.Http;

namespace Cod.Platform.Identity
{
    internal class BearerTokenMiddleware(PrincipalParser principalParser) : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var principal = await principalParser.TryParseAsync(context.Request, cancellationToken: context.RequestAborted);

            if (principal != null)
            {
                context.User = principal;
            }

            await next(context);
        }
    }
}
