using Microsoft.Extensions.DependencyInjection;

namespace Cod.Platform.Notification
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddPlatformNotification(this IServiceCollection services)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddPlatform();

            services.AddTransient<INofiticationChannelRepository, NofiticationChannelRepository>();
            services.AddTransient<INotificationService, NotificationService>();
            return services;
        }
    }
}