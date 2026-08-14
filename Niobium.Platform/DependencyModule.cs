using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Niobium.Platform.Analytics;

namespace Niobium.Platform
{
    public static class DependencyModule
    {
        private static volatile bool added;
        private static volatile bool used;
        private static volatile bool loaded;
        private static volatile bool middlewareRegistered;

        public static IHostApplicationBuilder AddPlatform(this IHostApplicationBuilder builder)
        {
            if (added)
            {
                return builder;
            }

            added = true;
            builder.AddServiceDefaults();

            builder.Services.AddProblemDetails();
            builder.Services.AddOpenApi();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddControllers();
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
            });

            builder.Services.AddPlatform();
            return builder;
        }

        public static IApplicationBuilder UsePlatform(this IApplicationBuilder builder)
        {
            if (used)
            {
                return builder;
            }

            used = true;

            builder.UseForwardedHeaders();
            builder.UseRouting();

            if (builder is WebApplication app)
            {
                app.MapControllers();

                if (app.Environment.IsDevelopment())
                {
                    app.MapOpenApi();
                }
            }

            builder.ToMiddlewareHost().UsePlatform();
            return builder;
        }

        public static IServiceCollection AddPlatform(this IServiceCollection services)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            Niobium.InternalError.Register(new Platform.InternalErrorRetriever());
            services.AddOptions();
            services.AddTransient(typeof(Lazy<>), typeof(LazyWrapper<>));
            services.AddTransient(typeof(ObjectFactory<>));

            services.AddTransient<AppInsights>();

            services.AddTransient<ICacheStore, DatabaseCacheStore>();
            services.AddTransient<IConfigurationProvider, ConfigurationProvider>();

            services.AddTransient<ErrorHandlingMiddleware>();

            services.ConfigureHttpClientDefaults(http =>
            {
                http.AddStandardResilienceHandler();
                http.AddServiceDiscovery();
            });

            return services;
        }

        public static IMiddlewareHost UsePlatform(this IMiddlewareHost builder)
        {
            if (middlewareRegistered)
            {
                return builder;
            }

            middlewareRegistered = true;

            builder.UseMiddleware<ErrorHandlingMiddleware>();
            return builder;
        }
    }
}