using Microsoft.Azure.Functions.Worker;

namespace Niobium.Platform.Functions
{
    public static class IFunctionsWorkerApplicationBuilderExtensions
    {
        public static IMiddlewareHost ToMiddlewareHost(this IFunctionsWorkerApplicationBuilder builder) => new FunctionWorkerBuilderMiddlewareHost(builder);
    }
}
