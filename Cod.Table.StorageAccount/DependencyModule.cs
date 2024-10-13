using Azure.Core.Extensions;
using Azure.Data.Tables;
using Azure.Identity;
using Cod.Platform;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Table.StorageAccount
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddTable(
            this IServiceCollection services,
            IConfiguration tableConfiguration,
            IConfiguration azureClientDefaults = null,
            bool enableInteractiveIdentity = false)
        {
            return services.AddTable(s =>
            {
                s.AddAzureClients(clientBuilder =>
                {
                    clientBuilder.AddTableServiceClient(tableConfiguration)
                        .WithCredential(new DefaultAzureCredential(includeInteractiveCredentials: enableInteractiveIdentity));

                    if (azureClientDefaults != null)
                    {
                        clientBuilder.ConfigureDefaults(azureClientDefaults);
                    }
                });
            });
        }

        public static IServiceCollection AddTable(this IServiceCollection services, StorageTableOptions options, IConfiguration azureClientDefaults = null)
        {
            return services.AddTable(s =>
            {
                s.AddAzureClients(clientBuilder =>
                {
                    IAzureClientBuilder<TableServiceClient, TableClientOptions> tableClientBuilder;
                    if (Uri.TryCreate(options.ConnectionString, UriKind.Absolute, out var endpointUri))
                    {
                        tableClientBuilder = clientBuilder.AddTableServiceClient(endpointUri);
                    }
                    else
                    {
                        tableClientBuilder = clientBuilder.AddTableServiceClient(options.ConnectionString);
                    }

                    tableClientBuilder.WithCredential(new DefaultAzureCredential(includeInteractiveCredentials: options.EnableInteractiveIdentity));
                    if (azureClientDefaults != null)
                    {
                        clientBuilder.ConfigureDefaults(azureClientDefaults);
                    }
                });
            });
        }

        private static IServiceCollection AddTable(this IServiceCollection services, Action<IServiceCollection> configure)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddCodPlatform();

            services.AddTransient(typeof(IRepository<>), typeof(CloudTableRepository<>));
            services.AddTransient(typeof(IQueryableRepository<>), typeof(CloudTableRepository<>));
            services.AddTransient<ISignatureIssuer, AzureTableSignatureIssuer>();
            services.AddTransient<IRepository<Cache>, CloudTableRepository<Cache>>();

            configure.Invoke(services);
            return services;
        }
    }
}