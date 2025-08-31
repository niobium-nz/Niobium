using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Niobium
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection RegisterDomainComponents(this IServiceCollection services, Type anyTypeFromAssembly)
        {
            var implementations = anyTypeFromAssembly.Assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract);

            foreach (var implementation in implementations)
            {
                if (typeof(IFlow).IsAssignableFrom(implementation))
                {
                    services.AddTransient(implementation);
                    services.AddTransient(typeof(IFlow), implementation);
                }
            }

            foreach (var implementation in implementations)
            {
                var domainType = implementation.GetInterfaces()
                    .SingleOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomain<>));
                if (domainType != null)
                {
                    var entityType = domainType.GetGenericArguments()[0];
                    var addDomainMethod = typeof(IServiceCollectionExtensions)
                        .GetMethod(nameof(AddDomain), BindingFlags.Public | BindingFlags.Static)!
                        .MakeGenericMethod(domainType, entityType);
                    addDomainMethod.Invoke(null, [services, false]);
                }
            }

            foreach (var implementation in implementations)
            {
                var domainEventHandlerType = implementation.GetInterfaces()
                    .SingleOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>));
                if (domainEventHandlerType != null)
                {
                    var domainArguments = domainEventHandlerType.GetGenericArguments()[0].GetGenericArguments();
                    if (domainArguments.Length != 1)
                    {
                        throw new InvalidCastException($"Unsupported domain event handler type: {domainEventHandlerType.FullName}. IDomainEventHandler implementation must explicitly declare its corresponding IDomain<T>");
                    }

                    var entityType = domainArguments[0];
                    var addDomainEventHandlerMethod = typeof(IServiceCollectionExtensions)
                        .GetMethod(nameof(AddDomainEventHandler), BindingFlags.Public | BindingFlags.Static)
                        ?.MakeGenericMethod(implementation, entityType);
                    addDomainEventHandlerMethod?.Invoke(null, [services]);
                }
            }

            return services;
        }

        public static IServiceCollection AddDomain<TDomain, TEntity>(this IServiceCollection services, bool singletonRepository = false) where TDomain : class, IDomain<TEntity>
        {
            services.AddTransient<TDomain>();
            services.AddTransient<Func<TDomain>>(sp => () => sp.GetRequiredService<TDomain>());

            if (singletonRepository)
            {
                services.AddSingleton<IDomainRepository<TDomain, TEntity>, GenericDomainRepository<TDomain, TEntity>>();
            }
            else
            {
                services.AddTransient<IDomainRepository<TDomain, TEntity>, GenericDomainRepository<TDomain, TEntity>>();
            }
            return services;
        }

        public static IServiceCollection AddDomainEventHandler<TEventHandler, TEntity>(this IServiceCollection services)
            where TEventHandler : class, IDomainEventHandler<IDomain<TEntity>>
        {
            services.AddTransient<IDomainEventHandler<IDomain<TEntity>>, TEventHandler>();
            return services;
        }
    }
}
