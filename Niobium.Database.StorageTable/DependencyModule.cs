using Microsoft.Extensions.DependencyInjection;

namespace Niobium.Database.StorageTable
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddDatabase(this IServiceCollection services, Action<StorageTableOptions>? options = null)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.Configure<StorageTableOptions>(o => options?.Invoke(o));
            services.AddTransient<IAzureTableClientFactory, AzureTableClientFactory>();
            services.AddTransient(typeof(CloudTableRepository<>));
            services.AddTransient(typeof(IRepository<>), typeof(CloudTableRepository<>));

            return services;
        }

        public static IServiceCollection AddMemoryCachedRepository<T>(this IServiceCollection services)
             where T : class, new()
        {
            services.AddSingleton<IRepository<T>>(sp =>
            {
                CloudTableRepository<T> innerRepository = sp.GetRequiredService<CloudTableRepository<T>>();
                return new MemoryCachedRepository<T>(innerRepository);
            });
            return services;
        }

        public static IServiceCollection AddRepository<T>(this IServiceCollection services, string tableName)
             where T : class, new()
        {
            services.AddTransient<IRepository<T>>(sp =>
            {
                CloudTableRepository<T> repo = sp.GetRequiredService<CloudTableRepository<T>>();
                repo.TableName = tableName.Trim();
                return repo;
            });
            return services;
        }
    }
}