using Microsoft.Extensions.DependencyInjection;

namespace Cod.Channel.Mobile
{
    public static class DependencyModule
    {
        public static IServiceCollection AddCodMobile(this IServiceCollection services)
        {
            services.AddSingleton<INavigator, NavigatorAdaptor>();
            return services;
        }
    }
}
