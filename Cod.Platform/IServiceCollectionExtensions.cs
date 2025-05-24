using Microsoft.Extensions.DependencyInjection;

namespace Cod.Platform
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection RegisterResourceControl<T>(this IServiceCollection services)
            where T : class, IResourceControl
        {
            services.AddTransient<IResourceControl, T>();
            return services;
        }
    }
}
