using Microsoft.Extensions.DependencyInjection;

namespace Cod.File.Blob
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddFile(this IServiceCollection services, Action<StorageBlobOptions>? options = null)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.Configure<StorageBlobOptions>(o => options?.Invoke(o));
            services.AddTransient<AzureBlobClientFactory>();
            services.AddTransient<IFileService, CloudBlobRepository>();
            return services;
        }
    }
}