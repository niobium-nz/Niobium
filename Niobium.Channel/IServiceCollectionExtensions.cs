using Microsoft.Extensions.DependencyInjection;

namespace Niobium.Channel
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection ScanAssemblyForViewModel(this IServiceCollection services, Type anyTypeFromAssembly)
        {
            IEnumerable<Type> types = anyTypeFromAssembly.Assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract);

            foreach (Type type in types)
            {
                if (typeof(IViewModel).IsAssignableFrom(type))
                {
                    services.AddTransient(type);
                    services.AddTransient(typeof(IViewModel), sp => (IViewModel)sp.GetRequiredService(type));
                }

                var isValueProvider = type.GetInterfaces().Any(i => 
                    i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEditModeValueProvider<>));
                if (isValueProvider)
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
            var serviceType = valueProviderType.GetInterfaces().SingleOrDefault(i =>
                    i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEditModeValueProvider<>))
                ?? throw new ArgumentException("Value provider type must implement IEditModeValueProvider<TViewModel>.");

            if (!valueProviderType.IsClass || valueProviderType.IsAbstract)
            {
                throw new ArgumentException("Value provider type must be a non-abstract class.");
            }

            if (!typeof(IViewModel).IsAssignableFrom(serviceType.GetGenericArguments()[0]))
            {
                throw new ArgumentException("Value provider type must implement IViewModel.");
            }

            services.AddTransient(valueProviderType);
            services.AddTransient(typeof(IEditModeValueProvider), sp => sp.GetRequiredService(valueProviderType));
            services.AddTransient(serviceType, sp => sp.GetRequiredService(valueProviderType));

            return services;
        }
    }
}
