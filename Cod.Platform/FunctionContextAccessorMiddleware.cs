using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace Cod.Platform
{
    internal class FunctionContextAccessorMiddleware : IFunctionsWorkerMiddleware
    {
        private IHttpContextAccessor ContextAccessor { get; }

        public FunctionContextAccessorMiddleware(IHttpContextAccessor accessor)
        {
            ContextAccessor = accessor;
        }

        public Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            if (ContextAccessor.HttpContext != null)
            {
                // This should never happen because the context should be localized to the current Task chain.
                // But if it does happen (perhaps the implementation is bugged), then we need to know immediately so it can be fixed.
                throw new InvalidOperationException($"Unable to initalize {nameof(IHttpContextAccessor)}: context has already been initialized.");
            }

            ContextAccessor.HttpContext = context.GetHttpContext();

            return next(context);
        }
    }
}
