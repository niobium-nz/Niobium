using Microsoft.Extensions.DependencyInjection;

namespace Niobium.Platform
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddResourceControl<T>(this IServiceCollection services)
            where T : class, IResourceControl
        {
            services.AddTransient<IResourceControl, T>();
            return services;
        }
    }
}
