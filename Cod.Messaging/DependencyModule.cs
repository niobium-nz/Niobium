using Microsoft.Extensions.DependencyInjection;

namespace Cod.Messaging
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddMessaging(this IServiceCollection services)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddTransient(typeof(IExternalEventAdaptor<,>), typeof(ExternalEventAdaptor<,>));

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