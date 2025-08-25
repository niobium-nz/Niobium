using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Niobium.Identity;
using Niobium.Platform.Identity;

namespace Niobium.Platform.Speech
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static void AddSpeech(this IHostApplicationBuilder builder)
        {
            builder.Services.AddSpeech(builder.Configuration.GetSection(nameof(SpeechServiceOptions)).Bind);
        }

        public static IServiceCollection AddSpeech(this IServiceCollection services, Action<SpeechServiceOptions>? options)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddPlatform();

            services.Configure<SpeechServiceOptions>(o => { options?.Invoke(o); o.Validate(); });

            services.AddTransient<IResourceControl, SpeechServiceControl>();
            services.AddHttpClient<ISignatureIssuer, SpeechServiceSignatureIssuer>((serviceProvider, httpClient) =>
                {
                    IOptions<SpeechServiceOptions> options = serviceProvider.GetRequiredService<IOptions<SpeechServiceOptions>>();
                    httpClient.BaseAddress = new Uri($"https://{options.Value.ServiceRegion}.api.cognitive.microsoft.com/");
                    httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", options.Value.AccessKey);
                })
                .AddStandardResilienceHandler();

            return services;
        }

        public static IServiceCollection GrantSpeechTranscribeEntitlementToDefaultRole(this IServiceCollection services)
        {
            return services.GrantSpeechTranscribeEntitlementTo(sp => sp.GetRequiredService<IOptions<IdentityServiceOptions>>().Value.DefaultRole);
        }

        public static IServiceCollection GrantSpeechTranscribeEntitlementTo(this IServiceCollection services, Func<IServiceProvider, string> resolveRole)
        {
            return services.AddTransient<IEntitlementDescriptor>(sp =>
                    {
                        string role = resolveRole(sp);
                        return new RoleBasedTranscribeEntitlementDescriptor(role, sp.GetRequiredService<IOptions<SpeechServiceOptions>>());
                    });
        }

        public static IServiceCollection GrantSpeechTranscribeEntitlementTo(this IServiceCollection services, string role)
        {
            return services.AddTransient<IEntitlementDescriptor>(sp => new RoleBasedTranscribeEntitlementDescriptor(role, sp.GetRequiredService<IOptions<SpeechServiceOptions>>()));
        }
    }
}
