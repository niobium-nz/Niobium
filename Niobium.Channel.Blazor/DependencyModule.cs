using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Niobium.Channel.Blazor
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddChannelBlazor(this IServiceCollection services)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddChannel();


            services.AddTransient(services => (IJSInProcessRuntime)services.GetRequiredService<IJSRuntime>());
            services.AddSingleton<IBrowser, BlazorBrowser>();
            services.AddSingleton<INavigator, NavigatorAdaptor>();
            return services;
        }
    }
}
