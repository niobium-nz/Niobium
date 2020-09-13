using System;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Channel
{
    public class DependencyModule : IDependencyModule
    {
        public void Load(IServiceCollection services)
        {
            InternalError.Register(new InternalErrorRetriever());
            services.AddTransient<AuthenticatorInitializer>();
            services.AddTransient<IQueue, ChannelQueue>();
            services.AddTransient<LoginCommand>();
            services.AddTransient<ICommand>(sp => sp.GetService<LoginCommand>());
            services.AddTransient<ICommand<LoginCommandParameter>>(sp => sp.GetService<LoginCommand>());
            services.AddTransient<IEventHandler<IAuthenticator>, LoginNavigationEventHandler>();
            services.AddSingleton<IAuthenticator, DefaultAuthenticator>();
            services.AddSingleton<ICommander, DefaultCommander>();
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<IHttpClient, HttpClientAdapter>();
            services.AddSingleton(sp => new Lazy<IHttpClient>(() => sp.GetService<IHttpClient>(), true));
        }
    }
}
