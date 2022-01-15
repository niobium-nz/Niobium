using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.Options;

namespace AzureFunctions.Autofac.Provider.Config
{
    public class InjectExtensionConfigProvider : IExtensionConfigProvider
    {
        private readonly InjectBindingProvider bindingProvider;

        public InjectExtensionConfigProvider(IOptions<ExecutionContextOptions> options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var appDirectory = options.Value.AppDirectory;
            this.bindingProvider = new InjectBindingProvider(appDirectory);
        }

        public void Initialize(ExtensionConfigContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.AddBindingRule<InjectAttribute>().Bind(this.bindingProvider);
        }
    }
}

