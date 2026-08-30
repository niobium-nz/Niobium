using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Niobium.Profile
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static TBuilder AddProfile<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
        {
            builder.Services.AddProfile(
                builder.Configuration.GetSection(nameof(ProfileOptions)).Bind,
                builder.Environment.IsDevelopment());
            return builder;
        }

        public static IServiceCollection AddProfile(this IServiceCollection services, Action<ProfileOptions>? options, bool testMode = false)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.Configure<ProfileOptions>(o => { options?.Invoke(o); o.Validate(); });

            IHttpClientBuilder httpClientBuilder = services.AddHttpClient(Constants.DefaultHttpClientName, (sp, httpClient) =>
            {
                IOptions<ProfileOptions> options = sp.GetRequiredService<IOptions<ProfileOptions>>();
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
