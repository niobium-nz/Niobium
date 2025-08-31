using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace Niobium.Platform
{
    public class FunctionMiddlewareAdaptor<T>(IServiceProvider serviceProvider) : IFunctionsWorkerMiddleware
        where T : IMiddleware
    {
        private readonly IMiddleware middleware = serviceProvider.GetRequiredService<T>();

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            HttpContext? httpContext = context.GetHttpContext();
            if (httpContext != null)
            {
                await middleware.InvokeAsync(httpContext, async (_) => await next(context));
            }

            await next(context);
        }
    }
}
