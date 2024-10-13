using Azure.Storage.Blobs;
using Cod.Platform;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Storage.Blob
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddStorageBlob(this IServiceCollection services)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddCodPlatform();

            services.AddTransient(sp =>
            {
                string conn = ConfigurationProvider.GetSetting(Constants.BlobEndpoint);
                conn ??= ConfigurationProvider.GetSetting(Constants.STORAGE_CONNECTION_NAME);
                return new BlobServiceClient(conn);
            });

            services.AddTransient<ISignatureIssuer, AzureBlobSignatureIssuer>();
            services.AddTransient<IBlobRepository, CloudBlobRepository>();

            return services;
        }
    }
}