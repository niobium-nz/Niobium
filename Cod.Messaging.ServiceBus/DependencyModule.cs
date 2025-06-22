using Cod.Identity;
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

        public static IServiceCollection AddMessagingBroker<T>(this IServiceCollection services, Action<ServiceBusOptions>? options = null)
            where T : class, IDomainEvent
        {
            services.AddTransient<IMessagingBroker<T>>(sp =>
            {
                var factory = sp.GetRequiredService<AuthenticationBasedQueueFactory>();
                ServiceBusOptions config = new();
                options?.Invoke(config);
                factory.Configuration = config;
                var authenticator = new Lazy<IAuthenticator>(() => sp.GetRequiredService<IAuthenticator>());
                var broker = new ServiceBusQueueBroker<T>(factory, authenticator);
                return broker;
            });
            return services;
        }
    }
}