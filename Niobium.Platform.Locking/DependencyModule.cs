using Microsoft.Extensions.DependencyInjection;

namespace Niobium.Platform.Locking
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddPlatformLocking(this IServiceCollection services)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddTransient<IResourceControl, ImpedimentControl>();
            services.AddTransient<IImpedimentPolicy, ImpedementPolicyScanProvider>();

            return services;
        }
    }
}