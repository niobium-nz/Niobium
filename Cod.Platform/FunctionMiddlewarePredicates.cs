using Microsoft.Azure.Functions.Worker;

namespace Cod.Platform
{
    public abstract class FunctionMiddlewarePredicates
    {
        public static bool IsHttp(FunctionContext context)
        {
            return context.FunctionDefinition.InputBindings.Values.First(a => a.Type.EndsWith("Trigger", StringComparison.Ordinal)).Type == "httpTrigger";
        }
    }
}
