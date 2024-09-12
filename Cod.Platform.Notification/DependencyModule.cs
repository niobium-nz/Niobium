using Cod.Platform.Tenant;
using Cod.Storage.Table;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Platform.Notification
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddPlatformNotification(this IServiceCollection services, StorageTableOptions options)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddCodPlatform();
            services.AddPlatformTenant(options);

            services.AddTransient<INofiticationChannelRepository, NofiticationChannelRepository>();
            services.AddTransient<INotificationService, NotificationService>();
            return services;
        }
    }
}