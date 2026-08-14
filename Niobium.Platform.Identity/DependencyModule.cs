using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Niobium.Identity;

namespace Niobium.Platform.Identity
{
    public static class DependencyModule
    {
        private static volatile bool added;
        private static volatile bool used;
        private static volatile bool middlewareRegistered;

        public static void AddIdentity(this IHostApplicationBuilder builder)
            => builder.Services.AddIdentity(builder.Configuration.GetSection(nameof(IdentityServiceOptions)).Bind);

        public static IServiceCollection AddIdentity(this IServiceCollection services, Action<IdentityServiceOptions>? identityOptions)
        {
            if (added)
            {
                return services;
            }

            added = true;

            services.AddPlatform();

            services.Configure<IdentityServiceOptions>(o => { identityOptions?.Invoke(o); o.Validate(); IdentityServiceOptions.Instance = o; });

            services.AddTransient<PrincipalParser>();
            services.AddTransient<ISignatureService, SignatureService>();
            services.AddTransient<ITokenBuilder, BearerTokenBuilder>();
            services.AddTransient<BearerTokenMiddleware>();
            services.AddTransient<AccessTokenMiddleware>();
            services.AddTransient<ResourceTokenMiddleware>();
            services.AddTransient<IEntitlementDescriptor, DatabaseEntitlementStore>();
            return services;
        }

        public static IApplicationBuilder UsePlatformIdentity(this IApplicationBuilder builder)
        {
            if (used)
            {
                return builder;
            }

            used = true;

            builder.UsePlatform();
            builder.ToMiddlewareHost().UsePlatformIdentity();
            return builder;
        }

        public static IMiddlewareHost UsePlatformIdentity(this IMiddlewareHost builder)
        {
            if (middlewareRegistered)
            {
                return builder;
            }

            middlewareRegistered = true;

            builder.UsePlatform();
            builder.UseMiddleware<BearerTokenMiddleware>();
            builder.UseMiddleware<AccessTokenMiddleware>();
            builder.UseMiddleware<ResourceTokenMiddleware>();
            return builder;
        }
    }
}