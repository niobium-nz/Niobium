using Cod.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Channel.Identity
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddIdentity(this IServiceCollection services, Action<IdentityServiceOptions> options, bool testMode = false)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddChannel();

            services.Configure<IdentityServiceOptions>(o => { options?.Invoke(o); o.Validate(); });

            var httpClientBuilder = services.AddHttpClient<IdentityService>();
            if (!testMode)
            {
                httpClientBuilder.AddStandardResilienceHandler();
            }

            services.AddTransient<EmailLoginViewModel>();
            services.AddTransient<ICommand<LoginCommandParameter, LoginResult>, LoginCommand>();
            services.AddTransient<ICommand<TOTPLoginCommandParameter, LoginResult>, TOTPLoginCommand>();
            services.AddSingleton<IAuthenticator, DefaultAuthenticator>();
            return services;
        }
    }
}