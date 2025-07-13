using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cod.Profile
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddProfile(IServiceCollection services, Action<ProfileOptions>? options, bool testMode = false)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.Configure<ProfileOptions>(o => { options?.Invoke(o); o.Validate(); });

            var httpClientBuilder = services.AddHttpClient(Constants.DefaultHttpClientName, (sp, httpClient) =>
            {
                var options = sp.GetRequiredService<IOptions<ProfileOptions>>();
                httpClient.BaseAddress = new Uri(options.Value.ProfileServiceHost);
            });

            if (!testMode)
            {
                httpClientBuilder.AddStandardResilienceHandler();
            }

            return services;
        }
    }
}
