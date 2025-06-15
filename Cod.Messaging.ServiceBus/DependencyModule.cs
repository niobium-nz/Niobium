using Microsoft.Extensions.DependencyInjection;

namespace Cod.Messaging.ServiceBus
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddMessaging(this IServiceCollection services, Action<ServiceBusOptions>? options)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            Cod.Messaging.DependencyModule.AddMessaging(services);
            services.Configure<ServiceBusOptions>(o => options?.Invoke(o));
            services.AddTransient<AuthenticationBasedQueueFactory>();
            services.AddTransient(typeof(IMessagingBroker<>), typeof(ServiceBusQueueBroker<>));

            return services;
        }
    }
}