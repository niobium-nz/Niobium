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

        public static void AddIdentity(this IHostApplicationBuilder builder)
        {
            builder.Services.AddIdentity(builder.Configuration.GetSection(nameof(IdentityServiceOptions)).Bind);
        }

        public static IServiceCollection AddIdentity(this IServiceCollection services, Action<IdentityServiceOptions>? identityOptions)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddPlatform();

            services.Configure<IdentityServiceOptions>(o => { identityOptions?.Invoke(o); o.Validate(); IdentityServiceOptions.Instance = o; });

            services.AddTransient<PrincipalParser>();
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
            builder.UsePlatform();
            builder.UseWhen<FunctionMiddlewareAdaptor<AccessTokenMiddleware>>(FunctionMiddlewarePredicates.IsHttp);
            builder.UseWhen<FunctionMiddlewareAdaptor<ResourceTokenMiddleware>>(FunctionMiddlewarePredicates.IsHttp);
            return builder;
        }

        public static IApplicationBuilder UsePlatformIdentity(this IApplicationBuilder builder)
        {
            builder.UsePlatform();
            builder.UseMiddleware<AccessTokenMiddleware>();
            builder.UseMiddleware<ResourceTokenMiddleware>();
            return builder;
        }
    }
}