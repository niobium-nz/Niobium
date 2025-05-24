using Microsoft.Extensions.DependencyInjection;

namespace Cod.Channel
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddChannelDomain<TDomain, TEntity>(this IServiceCollection services) where TDomain : class, IDomain<TEntity>
        {
            services.AddDomain<TDomain, TEntity>(singletonRepository: true)
                    .AddTransient<ICommand<LoadCommandParameter, LoadCommandResult<TDomain>>, LoadCommand<TDomain, TEntity>>();
            return services;
        }

        public static IServiceCollection AddViewModel<T>(this IServiceCollection services) where T : class
        {
            services.AddTransient<T>();
            services.AddTransient<Func<T>>(sp => () => sp.GetRequiredService<T>());
            return services;
        }
    }
}
