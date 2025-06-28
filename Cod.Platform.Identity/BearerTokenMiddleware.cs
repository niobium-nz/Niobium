using Microsoft.AspNetCore.Http;

namespace Cod.Platform.Identity
{
    internal class BearerTokenMiddleware(PrincipalParser principalParser) : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                context.User = await principalParser.ParseAsync(context.Request, cancellationToken: context.RequestAborted);
                await next(context);
            }
            catch (ApplicationException)
            {
            }
        }
    }
}
