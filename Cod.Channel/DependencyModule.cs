using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Channel
{
    public class DependencyModule : IDependencyModule
    {
        public void Load(IServiceCollection services)
        {
            InternalError.Register(new InternalErrorRetriever());

            services.AddTransient(sp =>
            {
                var client = new HttpClient();
                if (HttpClientSettings.BaseAddress != null)
                {
                    client.BaseAddress = HttpClientSettings.BaseAddress;
                }

                if (HttpClientSettings.Timeout.HasValue)
                {
                    client.Timeout = HttpClientSettings.Timeout.Value;
                }

                if (HttpClientSettings.MaxResponseContentBufferSize.HasValue)
                {
                    client.MaxResponseContentBufferSize = HttpClientSettings.MaxResponseContentBufferSize.Value;
                }

                return client;
            });
            services.AddTransient<IBootstrapper, AuthenticatorInitializer>();
            services.AddTransient<IQueue, ChannelQueue>();
            services.AddTransient<IBlob, ChannelBlob>();
            services.AddTransient<LoginCommand>();
            services.AddTransient<ICommand>(sp => sp.GetService<LoginCommand>());
            services.AddTransient<ICommand<LoginCommandParameter>>(sp => sp.GetService<LoginCommand>());
            services.AddTransient<LoginNavigationEventHandler>();
            services.AddTransient<IEventHandler<IAuthenticator>>(sp => sp.GetService<LoginNavigationEventHandler>());
            services.AddSingleton<IAuthenticator, DefaultAuthenticator>();
            services.AddSingleton<ICommander, DefaultCommander>();
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<IHttpClient, HttpClientAdapter>();
            services.AddSingleton(sp => new Lazy<IHttpClient>(() => sp.GetService<IHttpClient>(), true));
        }
    }
}
