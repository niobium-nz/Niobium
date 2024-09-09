using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Platform
{
    public class FunctionMiddlewareAdaptor<T> : IFunctionsWorkerMiddleware
        where T : IMiddleware
    {
        private readonly IMiddleware middleware;

        public FunctionMiddlewareAdaptor(IServiceProvider serviceProvider)
        {
            this.middleware = serviceProvider.GetRequiredService<T>();
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            var httpContext = context.GetHttpContext();
            await middleware.InvokeAsync(httpContext, async (_) => await next(context));
        }
    }
}
