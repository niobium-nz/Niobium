using Cod.Platform.Analytics;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Platform
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddCodPlatform(this IServiceCollection services)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            Cod.InternalError.Register(new Cod.Platform.InternalErrorRetriever());
            services.AddTransient(typeof(Lazy<>), typeof(LazyWrapper<>));

            services.AddTransient<AppInsights>();

            services.AddTransient<IConfigurationProvider, ConfigurationProvider>();

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
    }
}