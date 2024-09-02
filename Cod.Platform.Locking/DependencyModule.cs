using Cod.Storage.Table;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Platform.Locking
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddPlatformLocking(this IServiceCollection services, IConfiguration configuration)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddStorageTable(configuration);

            services.AddTransient<IStorageControl, ImpedimentControl>();
            services.AddTransient<IImpedimentPolicy, ImpedementPolicyScanProvider>();

            return services;
        }
    }
}