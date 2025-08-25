using Microsoft.AspNetCore.Http;

namespace Niobium.Platform.Identity
{
    internal sealed class BearerTokenMiddleware(PrincipalParser principalParser) : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            System.Security.Claims.ClaimsPrincipal? principal = await principalParser.TryParseAsync(context.Request, cancellationToken: context.RequestAborted);

            if (principal != null)
            {
                context.User = principal;
            }

            await next(context);
        }
    }
}
