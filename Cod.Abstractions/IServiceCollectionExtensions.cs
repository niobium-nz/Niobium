using Microsoft.Extensions.DependencyInjection;

namespace Cod
{
    public static class IServiceCollectionExtensions
    {
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
