using Microsoft.Extensions.DependencyInjection;

namespace Niobium.Channel
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddChannel(this IServiceCollection services)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            InternalError.Register(new InternalErrorRetriever());
            services.AddOptions();
            services.AddTransient(typeof(Lazy<>), typeof(LazyWrapper<>));
            services.AddTransient(typeof(ObjectFactory<>));
            services.AddSingleton<ILoadingStateService, DefaultLoadingStateService>();
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddTransient<IEditModeValueProviderFactory>(sp => new DefaultEditModeValueProviderFactory(sp));
            services.AddSingleton<IBootstrapper, DynamicUICache>();
            return services;
        }

        public static async Task InitializeAsync(this IServiceProvider services)
        {
            IEnumerable<IBootstrapper>? bootstrappers = services.GetService<IEnumerable<IBootstrapper>>();
            if (bootstrappers != null)
            {
                foreach (IBootstrapper bootstrapper in bootstrappers)
                {
                    await bootstrapper.InitializeAsync();
                }
            }
        }
    }
}
