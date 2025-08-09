using Azure.Core.Extensions;
using Azure.Identity;
using Azure.Storage.Queues;
using Cod.Platform;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Messaging.StorageAccount
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddMessaging(
            this IServiceCollection services,
            IConfiguration tableConfiguration,
            IConfiguration? azureClientDefaults = null,
            bool enableInteractiveIdentity = false)
        {
            return services.AddMessaging(s =>
            {
                s.AddAzureClients(clientBuilder =>
                {
                    clientBuilder.AddQueueServiceClient(tableConfiguration)
                        .WithCredential(new DefaultAzureCredential(includeInteractiveCredentials: enableInteractiveIdentity));

                    if (azureClientDefaults != null)
                    {
                        clientBuilder.ConfigureDefaults(azureClientDefaults);
                    }
                });
            });
        }

        public static IServiceCollection AddMessaging(
            this IServiceCollection services,
            StorageQueueOptions options,
            IConfiguration? azureClientDefaults = null)
        {
            return services.AddMessaging(s =>
            {
                s.AddAzureClients(clientBuilder =>
                {
                    IAzureClientBuilder<QueueServiceClient, QueueClientOptions> queueClientBuilder;
                    if (Uri.TryCreate(options.ServiceEndpoint, UriKind.Absolute, out var endpointUri))
                    {
                        queueClientBuilder = clientBuilder.AddQueueServiceClient(endpointUri);
                    }
                    else
                    {
                        queueClientBuilder = clientBuilder.AddQueueServiceClient(options.ServiceEndpoint);
                        if (options.Base64MessageEncoding)
                        {
                            queueClientBuilder.ConfigureOptions(opt => opt.MessageEncoding = QueueMessageEncoding.Base64);
                        }
                    }

                    queueClientBuilder.WithCredential(new DefaultAzureCredential(includeInteractiveCredentials: options.EnableInteractiveIdentity));
                    if (azureClientDefaults != null)
                    {
                        clientBuilder.ConfigureDefaults(azureClientDefaults);
                    }
                });
            });
        }

        private static IServiceCollection AddMessaging(this IServiceCollection services, Action<IServiceCollection> configure)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddPlatform();

            services.AddTransient<ISignatureIssuer, StorageQueueSignatureIssuer>();
            services.AddTransient(typeof(IMessagingBroker<>), typeof(StorageAccountQueueBroker<>));

            configure.Invoke(services);
            return services;
        }
    }
}