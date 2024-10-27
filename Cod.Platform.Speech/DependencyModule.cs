using Cod.Identity;
using Cod.Platform.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using Polly.Retry;

namespace Cod.Platform.Speech
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddSpeech(this IServiceCollection services, IConfiguration configuration)
        {
            return services.AddSpeech(configuration.Bind);
        }

        public static IServiceCollection AddSpeech(this IServiceCollection services, Action<SpeechServiceOptions> options)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddPlatform();

            services.Configure<SpeechServiceOptions>(o => { options(o); o.Validate(); });

            services.AddTransient<IResourceControl, SpeechServiceControl>();
            services.AddHttpClient<ISignatureIssuer, SpeechServiceSignatureIssuer>((serviceProvider, httpClient) =>
                {
                    var options = serviceProvider.GetRequiredService<IOptions<SpeechServiceOptions>>();
                    httpClient.BaseAddress = new Uri($"https://{options.Value.ServiceRegion}.api.cognitive.microsoft.com/");
                    httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", options.Value.AccessKey);
                })
                .AddPolicyHandler(RetryPolicy())
                .AddPolicyHandler(CircuitBreakerPolicy());

            return services;
        }

        public static IServiceCollection GrantSpeechTranscribeEntitlementToDefaultRole(this IServiceCollection services)
            => services.GrantSpeechTranscribeEntitlementTo(sp => sp.GetRequiredService<IOptions<IdentityServiceOptions>>().Value.DefaultRole);

        public static IServiceCollection GrantSpeechTranscribeEntitlementTo(this IServiceCollection services, Func<IServiceProvider, string> resolveRole)
            => services.AddTransient<IEntitlementDescriptor>(sp =>
            {
                var role = resolveRole(sp);
                return new RoleBasedTranscribeEntitlementDescriptor(role, sp.GetRequiredService<IOptions<SpeechServiceOptions>>());
            });

        public static IServiceCollection GrantSpeechTranscribeEntitlementTo(this IServiceCollection services, string role)
            => services.AddTransient<IEntitlementDescriptor>(sp => new RoleBasedTranscribeEntitlementDescriptor(role, sp.GetRequiredService<IOptions<SpeechServiceOptions>>()));

        private static AsyncCircuitBreakerPolicy<HttpResponseMessage> CircuitBreakerPolicy()
          => HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

        private static AsyncRetryPolicy<HttpResponseMessage> RetryPolicy()
          => Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .WaitAndRetryAsync(Backoff.DecorrelatedJitterBackoffV2(
                medianFirstRetryDelay: TimeSpan.FromMilliseconds(500),
                retryCount: 5));
    }
}
