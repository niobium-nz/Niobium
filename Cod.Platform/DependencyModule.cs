using Cod.Platform.Analytics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cod.Platform
{
    public static class DependencyModule
    {
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

            services.AddTransient<AppInsights>();

            services.AddTransient<ICacheStore, DatabaseCacheStore>();
            services.AddTransient<IConfigurationProvider, ConfigurationProvider>();

            services.AddTransient<ErrorHandlingMiddleware>();
            services.AddTransient<FunctionMiddlewareAdaptor<ErrorHandlingMiddleware>>();


            //services.AddTransient<IQueryableRepository<Impediment>, CloudTableRepository<Impediment>>();
            //services.AddTransient<IRepository<Impediment>, CloudTableRepository<Impediment>>();

            //services.AddTransient<IQueryableRepository<Entitlement>, CloudTableRepository<Entitlement>>();
            //services.AddTransient<IRepository<Entitlement>, CloudTableRepository<Entitlement>>();

            //services.AddTransient<IQueryableRepository<MobileLocation>, CloudTableRepository<MobileLocation>>();
            //services.AddTransient<IRepository<MobileLocation>, CloudTableRepository<MobileLocation>>();

            //services.AddTransient<IQueryableRepository<Job>, CloudTableRepository<Job>>();
            //services.AddTransient<IRepository<Job>, CloudTableRepository<Job>>();

            //services.AddTransient<IQueryableRepository<Report>, CloudTableRepository<Report>>();
            //services.AddTransient<IRepository<Report>, CloudTableRepository<Report>>();

            return services;
        }

        public static IFunctionsWorkerApplicationBuilder UsePlatform(this IFunctionsWorkerApplicationBuilder builder)
        {
            builder.UseWhen<FunctionMiddlewareAdaptor<ErrorHandlingMiddleware>>(FunctionMiddlewarePredicates.IsHttp);
            return builder;
        }

        public static IApplicationBuilder UsePlatform(this IApplicationBuilder builder)
        {
            builder.UseMiddleware<ErrorHandlingMiddleware>();
            return builder;
        }
    }
}