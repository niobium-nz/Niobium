using AzureFunctions.Autofac.Provider.Config;
using AzureFunctions.Autofac.Startup;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;

[assembly: WebJobsStartup(typeof(DependencyInjectionStartup))]

namespace AzureFunctions.Autofac.Startup
{
    public class DependencyInjectionStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddExtension<InjectExtensionConfigProvider>();

#pragma warning disable CS0618 // Type or member is obsolete
            builder.Services.AddSingleton<IFunctionFilter, ScopeFilter>();
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
