using Azure.Storage.Queues;
using Cod.Platform;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Storage.Messaging
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddStorageMessaging(this IServiceCollection services)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddCodPlatform();

            services.AddTransient(sp =>
            {
                string conn = ConfigurationProvider.GetSetting(Constants.QueueEndpoint);
                conn ??= ConfigurationProvider.GetSetting(Constants.STORAGE_CONNECTION_NAME);
                return new QueueServiceClient(conn, new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
            });

            services.AddSingleton<IIoTCommander, AzureIoTHubCommander>();
            services.AddTransient<ISignatureIssuer, AzureQueueSignatureIssuer>();
            services.AddTransient<IMessagingHub, CloudPlatformQueue>();

            return services;
        }
    }
}