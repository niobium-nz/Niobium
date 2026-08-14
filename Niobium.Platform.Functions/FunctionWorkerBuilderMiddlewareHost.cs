using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;

namespace Niobium.Platform.Functions
{
    internal class FunctionWorkerBuilderMiddlewareHost(IFunctionsWorkerApplicationBuilder builder) : IMiddlewareHost
    {
        public void UseMiddleware<TMiddleware>() where TMiddleware : IMiddleware
        {
            builder.UsePlatform();
            builder.UseMiddleware<FunctionMiddlewareAdaptor<TMiddleware>>();
        }
    }
}
