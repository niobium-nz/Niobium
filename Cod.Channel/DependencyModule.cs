using Microsoft.Extensions.DependencyInjection;

namespace Cod.Channel
{
    public class DependencyModule : IDependencyModule
    {
        public void Load(IServiceCollection services)
        {
            services.AddSingleton<IAuthenticator, DefaultAuthenticator>();
            services.AddSingleton<ICommandService, DefaultCommandService>();
        }
    }
}
