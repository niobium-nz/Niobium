using Blazor.Extensions.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Channel
{
    public class DependencyModule : IDependencyModule
    {
        public void Load(IServiceCollection services)
        {
            services.AddStorage();
            services.AddTransient<ICommand, LoginCommand>();
            services.AddTransient<IEventHandler<IAuthenticator>, LoginNavigationEventHandler>();
            services.AddSingleton<IAuthenticator, DefaultAuthenticator>();
            services.AddSingleton<ICommander, DefaultCommander>();
        }
    }
}
