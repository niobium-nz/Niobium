using Cod.Storage.Table;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Platform.Locking
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddPlatformLocking(this IServiceCollection services, StorageTableOptions options)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddStorageTable(options);

            services.AddTransient<IResourceControl, ImpedimentControl>();
            services.AddTransient<IImpedimentPolicy, ImpedementPolicyScanProvider>();

            return services;
        }
    }
}