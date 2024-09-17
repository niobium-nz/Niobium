using Cod.Platform;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.CircuitBreaker;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using Polly.Retry;

namespace Cod.Cloud.Azure.OpenAI
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddOpenAI(this IServiceCollection services, OpenAIServiceOptions options)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            options.Validate();
            services.AddSingleton(options);

            services.AddCodPlatform();

            services.AddHttpClient<IOpenAIService, OpenAIService>((serviceProvider, httpClient) =>
            {
                httpClient.BaseAddress = new Uri(options.Endpoint);
                httpClient.DefaultRequestHeaders.Add("api-key", options.Secret);
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