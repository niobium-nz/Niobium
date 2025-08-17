using Cod.Platform.Analytics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cod.Platform
{
    public static class DependencyModule
    {
        private static volatile bool used;
        private static volatile bool loaded;

        public static IServiceCollection AddPlatform(this IServiceCollection services)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            Cod.InternalError.Register(new Cod.Platform.InternalErrorRetriever());
            services.AddOptions();
            services.AddTransient(typeof(Lazy<>), typeof(LazyWrapper<>));
            services.AddTransient(typeof(ObjectFactory<>));

            services.AddTransient<AppInsights>();

            services.AddTransient<ICacheStore, DatabaseCacheStore>();
            services.AddTransient<IConfigurationProvider, ConfigurationProvider>();

            services.AddTransient<ErrorHandlingMiddleware>();
            services.AddTransient<FunctionMiddlewareAdaptor<ErrorHandlingMiddleware>>();

            services.AddHttpContextAccessor();
            return services;
        }

        public static T UsePlatform<T>(this T builder) where T : IFunctionsWorkerApplicationBuilder
        {
            if (used)
            {
                return builder;
            }

            used = true;

            builder.Services.AddSingleton<IHttpContextAccessor, FunctionContextAccessor>();
            builder.UseMiddleware<FunctionContextAccessorMiddleware>();
            builder.UseMiddleware<FunctionMiddlewareAdaptor<ErrorHandlingMiddleware>>();
            return builder;
        }

        public static IApplicationBuilder UsePlatform(this IApplicationBuilder builder)
        {
            if (used)
            {
                return builder;
            }

            used = true;
            builder.UseMiddleware<ErrorHandlingMiddleware>();
            return builder;
        }
    }
}