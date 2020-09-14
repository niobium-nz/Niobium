using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Channel.Blazor
{
    public class DependencyModule : IDependencyModule
    {
        public void Load(IServiceCollection services)
        {
            services.AddTransient(sp =>
            {
                var client = new HttpClient();
                if (HttpClientSettings.BaseAddress != null)
                {
                    client.BaseAddress = HttpClientSettings.BaseAddress;
                }

                if (HttpClientSettings.Timeout != null)
                {
                    client.Timeout = HttpClientSettings.Timeout;
                }

                if (HttpClientSettings.MaxResponseContentBufferSize.HasValue)
                {
                    client.MaxResponseContentBufferSize = HttpClientSettings.MaxResponseContentBufferSize.Value;
                }

                return client;
            });
            services.AddSingleton<IBrowser, BlazorBrowser>();
            services.AddSingleton<IAuthenticator, LocalStorageAuthenticator>();
            services.AddSingleton<INavigator, NavigatorAdaptor>();
        }
    }
}
