using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using Polly.Retry;

namespace Cod.Platform.OpenAI
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddOpenAI(this IServiceCollection services, IConfiguration configuration)
        {
            return services.AddOpenAI(configuration.Bind);
        }

        public static IServiceCollection AddOpenAI(
            this IServiceCollection services,
            Action<OpenAIServiceOptions> options)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddPlatform();

            services.Configure<OpenAIServiceOptions>(o => { options(o); o.Validate(); });

            services.AddHttpClient<IOpenAIService, OpenAIService>((serviceProvider, httpClient) =>
            {
                var config = serviceProvider.GetRequiredService<IOptions<OpenAIServiceOptions>>();
                httpClient.BaseAddress = new Uri(config.Value.Endpoint);
                httpClient.DefaultRequestHeaders.Add("api-key", config.Value.Secret);
            })
            .AddPolicyHandler(RetryPolicy())
            .AddPolicyHandler(CircuitBreakerPolicy());
            return services;
        }

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