using Cod.Channel.Blazor;
using Cod.Identity;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Channel.Identity.Blazor
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddIdentityBlazor(this IServiceCollection services, Action<IdentityServiceOptions> options)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddIdentity(options);
            services.AddChannelBlazor();

            services.AddAuthorizationCore();
            services.AddSingleton<IAuthenticator, LocalStorageAuthenticator>();
            services.AddTransient<AuthenticationStateAdaptor>();
            services.AddTransient<IDomainEventHandler<IAuthenticator>>(sp => sp.GetRequiredService<AuthenticationStateAdaptor>());
            services.AddTransient<AuthenticationStateProvider>(sp => sp.GetRequiredService<AuthenticationStateAdaptor>());
            return services;
        }
    }
}
