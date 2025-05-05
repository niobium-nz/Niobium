using Microsoft.Extensions.DependencyInjection;

namespace Cod
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection RegisterDomain<TDomain, TEntity>(this IServiceCollection services) where TDomain : class, IDomain<TEntity>
        {
            services.AddTransient<TDomain>();
            services.AddTransient<Func<TDomain>>(sp => () => sp.GetRequiredService<TDomain>());
            services.AddTransient<IDomainRepository<TDomain, TEntity>, GenericDomainRepository<TDomain, TEntity>>();
            return services;
        }

        public static IServiceCollection RegisterDomainEventHandler<TEventHandler, TEntity>(this IServiceCollection services)
            where TEventHandler : class, IDomainEventHandler<IDomain<TEntity>>
        {
            services.AddTransient<IDomainEventHandler<IDomain<TEntity>>, TEventHandler>();
            return services;
        }
    }
}
