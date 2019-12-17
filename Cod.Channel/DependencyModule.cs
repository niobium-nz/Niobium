using Microsoft.Extensions.DependencyInjection;

namespace Cod.Channel
{
    public class DependencyModule : IDependencyModule
    {
        public void Load(IServiceCollection services)
        {
            services.AddTransient<ICommand, LoginCommand>();
            services.AddTransient<IEventHandler<IAuthenticator>, LoginNavigationEventHandler>();
            services.AddSingleton<IAuthenticator, DefaultAuthenticator>();
            services.AddSingleton<ICommandService, DefaultCommandService>();
        }
    }
}
