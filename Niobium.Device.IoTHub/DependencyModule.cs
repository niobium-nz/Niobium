using Microsoft.Extensions.DependencyInjection;
using Niobium.Platform;

namespace Niobium.Device.IoTHub
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

            services.AddPlatform();
            services.AddSingleton<IIoTCommander, AzureIoTHubCommander>();

            return services;
        }
    }
}