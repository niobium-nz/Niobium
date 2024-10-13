using Microsoft.Extensions.DependencyInjection;

namespace Cod.Channel
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
            services.AddSingleton<ILoadingStateService, DefaultLoadingStateService>();
            services.AddSingleton<INotificationService, NotificationService>();
            return services;
        }

        public async static Task InitializeAsync(this IServiceProvider services)
        {
            var bootstrappers = services.GetService<IEnumerable<IBootstrapper>>();
            if (bootstrappers != null)
            {
                foreach (var bootstrapper in bootstrappers)
                {
                    await bootstrapper.InitializeAsync();
                }
            }
        }
    }
}
