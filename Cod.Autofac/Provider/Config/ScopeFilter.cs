using AzureFunctions.Autofac.Configuration;
using Microsoft.Azure.WebJobs.Host;

namespace AzureFunctions.Autofac.Provider.Config
{
#pragma warning disable CS0618 // Type or member is obsolete
    public class ScopeFilter : IFunctionInvocationFilter, IFunctionExceptionFilter, IFunctionFilter
    {
        public Task OnExceptionAsync(FunctionExceptionContext exceptionContext, CancellationToken cancellationToken)
        {
            if (exceptionContext is null)
            {
                throw new ArgumentNullException(nameof(exceptionContext));
            }

            RemoveScope(exceptionContext.FunctionInstanceId);
            return Task.CompletedTask;
        }

        public Task OnExecutedAsync(FunctionExecutedContext executedContext, CancellationToken cancellationToken)
        {
            if (executedContext is null)
            {
                throw new ArgumentNullException(nameof(executedContext));
            }

            RemoveScope(executedContext.FunctionInstanceId);
            return Task.CompletedTask;
        }

        public Task OnExecutingAsync(FunctionExecutingContext executingContext, CancellationToken cancellationToken) => Task.CompletedTask;

        private static void RemoveScope(Guid functionInstanceId) => DependencyInjection.RemoveScope(functionInstanceId);
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
