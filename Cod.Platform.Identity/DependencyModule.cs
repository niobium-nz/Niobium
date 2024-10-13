using Cod.Identity;
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

        public static IServiceCollection AddIdentity(this IServiceCollection services, IConfiguration configuration)
        {
            return services.AddIdentity(configuration.Bind);
        }

        public static IServiceCollection AddIdentity(this IServiceCollection services, Action<IdentityServiceOptions> identityOptions)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddCodPlatform();

            services.Configure<IdentityServiceOptions>(o => { identityOptions(o); o.Validate(); IdentityServiceOptions.Instance = o; });

            services.AddTransient<ISignatureService, SignatureService>();
            services.AddTransient<ITokenBuilder, BearerTokenBuilder>();
            services.AddTransient<AccessTokenMiddleware>();
            services.AddTransient<ResourceTokenMiddleware>();
            services.AddTransient<FunctionMiddlewareAdaptor<AccessTokenMiddleware>>();
            services.AddTransient<IEntitlementDescriptor, DatabaseEntitlementStore>();
            return services;
        }

        public static IFunctionsWorkerApplicationBuilder UsePlatformIdentity(this IFunctionsWorkerApplicationBuilder builder)
        {
            builder.UseWhen<FunctionMiddlewareAdaptor<AccessTokenMiddleware>>(FunctionMiddlewarePredicates.IsHttp);
            builder.UseWhen<FunctionMiddlewareAdaptor<ResourceTokenMiddleware>>(FunctionMiddlewarePredicates.IsHttp);
            return builder;
        }

        public static IApplicationBuilder UsePlatformIdentity(this IApplicationBuilder builder)
        {
            builder.UseMiddleware<AccessTokenMiddleware>();
            builder.UseMiddleware<ResourceTokenMiddleware>();
            return builder;
        }
    }
}