using Microsoft.Extensions.DependencyInjection;
using Niobium.Identity;

namespace Niobium.Messaging.ServiceBus
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

            services.AddMessaging();
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
                AuthenticationBasedQueueFactory factory = sp.GetRequiredService<AuthenticationBasedQueueFactory>();
                ServiceBusOptions config = new();
                options?.Invoke(config);
                factory.Configuration = config;
                Lazy<IAuthenticator> authenticator = new(() => sp.GetRequiredService<IAuthenticator>());
                ServiceBusQueueBroker<T> broker = new(factory, authenticator);
                return broker;
            });
            return services;
        }
    }
}