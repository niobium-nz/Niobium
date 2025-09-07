using Microsoft.Extensions.DependencyInjection;

namespace Niobium.Messaging
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddMessaging(this IServiceCollection services, bool testMode = false)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddTransient(typeof(IExternalEventAdaptor<,>), typeof(ExternalEventAdaptor<,>));

            if (testMode)
            {
                services.AddTransient(typeof(IMessagingBroker<>), typeof(DevelopmentMessagingBroker<>));
            }

            return services;
        }

        public static IServiceCollection EnableExternalEvent<TEvent, TEntity>(this IServiceCollection services)
            where TEntity : class
            where TEvent : class, IDomainEvent
        {
            services.AddDomainEventHandler<EventMessagingBroker<IDomain<TEntity>, TEntity, TEvent>, TEntity>();
            return services;
        }
    }
}