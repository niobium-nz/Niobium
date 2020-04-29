using Microsoft.Extensions.DependencyInjection;

namespace Cod.Channel.Blazor
{
    public class DependencyModule : IDependencyModule
    {
        public void Load(IServiceCollection services)
        {
            services.AddSingleton<IBrowser, BlazorBrowser>();
        }
    }
}
