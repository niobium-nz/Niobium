using Azure.Core.Extensions;
using Azure.Data.Tables;
using Azure.Identity;
using Cod.Platform;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Storage.Table
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddStorageTable(this IServiceCollection services, StorageTableOptions options)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            options.Validate();
            services.AddSingleton(options);

            services.AddCodPlatform();
            services.AddAzureClients(clientBuilder =>
            {
                IAzureClientBuilder<TableServiceClient, TableClientOptions> tableClientBuilder;
                if (Uri.TryCreate(options.ServiceEndpoint, UriKind.Absolute, out var endpointUri))
                {
                    tableClientBuilder = clientBuilder.AddTableServiceClient(endpointUri);
                }
                else
                {
                    tableClientBuilder = clientBuilder.AddTableServiceClient(options.ServiceEndpoint);
                }

                tableClientBuilder.WithCredential(new DefaultAzureCredential(includeInteractiveCredentials: options.EnableInteractiveIdentity));
                if (options.AzureStorageTableDefaults != null)
                {
                    clientBuilder.ConfigureDefaults(options.AzureStorageTableDefaults);
                }
            });

            services.AddTransient<ISignatureIssuer, AzureTableSignatureIssuer>();
            services.AddTransient<IRepository<Cache>, CloudTableRepository<Cache>>();

            return services;
        }
    }
}