using Cod.Platform.Identity.Authentication;
using Cod.Platform.Identity.Authorization;
using Cod.Storage.Table;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Platform.Identity
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddPlatformIdentity(this IServiceCollection services, IConfiguration configuration)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddStorageTable(configuration);
            services.AddCodPlatform();

            services.AddTransient<ISignatureService, AzureStorageSignatureService>();
            services.AddTransient<ITokenBuilder, BearerTokenBuilder>();

            return services;
        }
    }
}