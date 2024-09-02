using Cod.Platform.Tenant;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Platform.Notification
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddPlatformNotification(this IServiceCollection services, IConfiguration configuration)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddCodPlatform();
            services.AddPlatformTenant(configuration);

            services.AddTransient<INofiticationChannelRepository, NofiticationChannelRepository>();
            services.AddTransient<INotificationService, NotificationService>();
            return services;
        }
    }
}