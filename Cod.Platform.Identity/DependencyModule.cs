using Cod.Storage.Table;
using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cod.Platform.Identity
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddPlatformIdentity(
            this IServiceCollection services,
            IdentityServiceOptions identityOptions,
            StorageTableOptions tableOptions)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            identityOptions.Validate();
            services.AddSingleton(identityOptions);
            IdentityServiceOptions.Instance = identityOptions;

            services.AddStorageTable(tableOptions);
            services.AddCodPlatform();

            services.AddTransient<ISignatureService, SignatureService>();
            services.AddTransient<ITokenBuilder, BearerTokenBuilder>();
            services.AddTransient<AccessTokenMiddleware>();
            services.AddTransient<SignatureMiddleware>();
            services.AddTransient<FunctionMiddlewareAdaptor<AccessTokenMiddleware>>();

            services.AddTransient<IRepository<Role>, CloudTableRepository<Role>>();
            services.AddTransient<IRepository<Entitlement>, CloudTableRepository<Entitlement>>();
            services.AddTransient<IEntitlementDescriptor, DatabaseEntitlementStore>();
            return services;
        }

        public static IFunctionsWorkerApplicationBuilder UsePlatformIdentity(this IFunctionsWorkerApplicationBuilder builder)
        {
            builder.UseWhen<FunctionMiddlewareAdaptor<AccessTokenMiddleware>>(FunctionMiddlewarePredicates.IsHttp);
            builder.UseWhen<FunctionMiddlewareAdaptor<SignatureMiddleware>>(FunctionMiddlewarePredicates.IsHttp);
            return builder;
        }

        public static IApplicationBuilder UsePlatformIdentity(this IApplicationBuilder builder)
        {
            builder.UseMiddleware<AccessTokenMiddleware>();
            builder.UseMiddleware<SignatureMiddleware>();
            return builder;
        }
    }
}