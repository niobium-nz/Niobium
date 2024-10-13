using Cod.Platform;
using Cod.Storage.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Device.IoTHub
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddDevice(this IServiceCollection services)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddCodPlatform();
            services.AddSingleton<IIoTCommander, AzureIoTHubCommander>();

            return services;
        }
    }
}