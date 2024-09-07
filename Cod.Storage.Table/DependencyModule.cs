using Azure.Identity;
using Cod.Platform;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Storage.Table
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddStorageTable(this IServiceCollection services, IConfiguration configuration)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddCodPlatform();

            services.AddAzureClients(clientBuilder =>
            {
                clientBuilder.AddTableServiceClient(
                    configuration.GetSection(Constants.AppSettingStorageTable)
                        .GetValue<string>(Constants.AppSettingStorageTableServiceUri))
                .WithCredential(new DefaultAzureCredential());
                clientBuilder.ConfigureDefaults(configuration.GetSection("AzureDefaults"));
            });

            services.AddTransient<ISignatureIssuer, AzureTableSignatureIssuer>();
            services.AddTransient<IRepository<Cache>, CloudTableRepository<Cache>>();

            return services;
        }
    }
}