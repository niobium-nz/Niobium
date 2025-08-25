using Microsoft.Extensions.DependencyInjection;

namespace Niobium.Channel
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection ScanAssemblyForViewModel(this IServiceCollection services, Type anyTypeFromAssembly)
        {
            IEnumerable<Type> types = anyTypeFromAssembly.Assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IViewModel).IsAssignableFrom(t));

            foreach (Type type in types)
            {
                services.AddTransient(type);
                services.AddTransient(typeof(IViewModel), sp => (IViewModel)sp.GetRequiredService(type));

                if (typeof(IEditModeValueProvider<>).IsAssignableFrom(type))
                {
                    services.AddEditModeValueProvider(type);
                }
            }

            DynamicUICache.RegisterUIComponents(anyTypeFromAssembly);
            return services;
        }

        public static IServiceCollection AddChannelDomain<TDomain, TEntity>(this IServiceCollection services) where TDomain : class, IDomain<TEntity>
        {
            services.AddDomain<TDomain, TEntity>(singletonRepository: true)
                    .AddTransient<ICommand<LoadCommandParameter, LoadCommandResult<TDomain>>, LoadCommand<TDomain, TEntity>>();
            return services;
        }

        public static IServiceCollection AddEditModeValueProvider<TImplementation, TViewModel>(this IServiceCollection services)
            where TImplementation : class, IEditModeValueProvider<TViewModel>
            where TViewModel : class, IViewModel
        {
            services.AddTransient<IEditModeValueProvider, TImplementation>();
            services.AddTransient<IEditModeValueProvider<TViewModel>, TImplementation>();
            return services;
        }

        public static IServiceCollection AddEditModeValueProvider(this IServiceCollection services, Type valueProviderType)
        {
            if (typeof(IEditModeValueProvider<>).IsAssignableFrom(valueProviderType))
            {
                throw new ArgumentException("Value provider type must implement IEditModeValueProvider<TViewModel>.");
            }
            if (!valueProviderType.IsClass || valueProviderType.IsAbstract)
            {
                throw new ArgumentException("Value provider type must be a non-abstract class.");
            }
            if (!typeof(IViewModel).IsAssignableFrom(valueProviderType.GetGenericArguments()[0]))
            {
                throw new ArgumentException("Value provider type must implement IViewModel.");
            }

            Type viewModelType = valueProviderType.GetGenericArguments()[0];
            Type implementationType = typeof(IEditModeValueProvider<>).MakeGenericType(viewModelType);
            services.AddTransient(implementationType, valueProviderType);
            services.AddTransient(typeof(IEditModeValueProvider), sp => sp.GetRequiredService(implementationType));
            services.AddTransient(typeof(IEditModeValueProvider<>).MakeGenericType(viewModelType), sp => sp.GetRequiredService(implementationType));

            return services;
        }
    }
}
