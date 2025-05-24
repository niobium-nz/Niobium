using Cod.Profile;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cod.Channel.Profile
{
    public static class DependencyModule
    {
        public const string DefaultHttpClientName = "IdentityAPIClient";

        private static volatile bool loaded;

        public static IServiceCollection AddProfile(this IServiceCollection services, Action<ProfileOptions> options, bool testMode = false)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.Configure<ProfileOptions>(o => { options?.Invoke(o); o.Validate(); });

            var httpClientBuilder = services.AddHttpClient(DefaultHttpClientName, (sp, httpClient) =>
            {
                var options = sp.GetRequiredService<IOptions<ProfileOptions>>();
                httpClient.BaseAddress = new Uri(options.Value.ProfileServiceHost);
            });

            if (!testMode)
            {
                httpClientBuilder.AddStandardResilienceHandler();
            }

            services.AddTransient(typeof(IProfileService<>), typeof(GenericProfileService<>));
            return services;
        }
    }
}
