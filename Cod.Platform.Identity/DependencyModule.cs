using Cod.Storage.Table;
using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

            services.AddTransient<ISignatureService, SignatureService>();
            services.AddTransient<ITokenBuilder, BearerTokenBuilder>();
            services.AddTransient<AccessTokenMiddleware>();
            services.AddTransient<FunctionMiddlewareAdaptor<AccessTokenMiddleware>>();

            services.AddTransient<IRepository<Role>, CloudTableRepository<Role>>();
            services.AddTransient<IRepository<Entitlement>, CloudTableRepository<Entitlement>>();
            services.AddTransient<IEntitlementDescriptor, DatabaseEntitlementStore>();
            return services;
        }

        public static IFunctionsWorkerApplicationBuilder UsePlatformIdentity(this IFunctionsWorkerApplicationBuilder builder, Action<IdentityServiceOptions>? configureOptions = null)
        {
            var options = new IdentityServiceOptions();
            if (configureOptions != null)
            {
                configureOptions(options);
                options.Validate();
            }
            else
            {
                options.EnableAuthenticationEndpoint = false;
            }

            builder.Services.AddSingleton(options);

            if (options.EnableAuthenticationEndpoint)
            {
                builder.UseMiddleware<FunctionMiddlewareAdaptor<AccessTokenMiddleware>>();
            }

            return builder;
        }

        public static IApplicationBuilder UsePlatformIdentity(this IApplicationBuilder builder)
        {
            builder.UseMiddleware<AccessTokenMiddleware>();
            return builder;
        }
    }
}