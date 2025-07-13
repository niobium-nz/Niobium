using Cod.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

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

            var httpClientBuilder2 = services.AddHttpClient(Constants.PlatformAPIHttpClientName, (sp, httpClient) =>
            {
                var identityOptions = sp.GetRequiredService<IOptions<IdentityServiceOptions>>().Value;
                if (identityOptions.PlatformAPIEndpoint != null && !string.IsNullOrWhiteSpace(identityOptions.PlatformAPIEndpoint))
                {
                    httpClient.BaseAddress = new Uri(identityOptions.PlatformAPIEndpoint);
                }

                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var authenticator = sp.GetRequiredService<IAuthenticator>();
                if (authenticator.AccessToken != null)
                {
                    if (authenticator.IDToken != null)
                    {
                        var idTokenHeader = new AuthenticationHeaderValue(AuthenticationScheme.BearerLoginScheme, authenticator.IDToken.EncodedToken);
                        httpClient.DefaultRequestHeaders.TryAddWithoutValidation(Cod.Identity.Constants.IDTokenHeaderKey, idTokenHeader.ToString());
                    }

                    if (authenticator.AccessToken != null)
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthenticationScheme.BearerLoginScheme, authenticator.AccessToken.EncodedToken);
                    }
                }
            });
            if (!testMode)
            {
                httpClientBuilder2.AddStandardResilienceHandler();
            }

            services.AddTransient<EmailLoginViewModel>();
            services.AddTransient<ICommand<LoginCommandParameter, LoginResult>, LoginCommand>();
            services.AddTransient<ICommand<TOTPLoginCommandParameter, LoginResult>, TOTPLoginCommand>();
            services.AddSingleton<IAuthenticator, DefaultAuthenticator>();
            return services;
        }
    }
}