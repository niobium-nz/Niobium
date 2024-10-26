using Cod.Table.StorageAccount;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Database.StorageTable
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddDatabase(this IServiceCollection services, Action<StorageTableOptions> options = null)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.Configure<StorageTableOptions>(o => options?.Invoke(o));
            services.AddTransient<IAzureTableClientFactory, AzureTableClientFactory>();
            services.AddTransient(typeof(IRepository<>), typeof(CloudTableRepository<>));
            return services;
        }
    }
}