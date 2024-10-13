using Cod.Identity;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.CircuitBreaker;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using Polly.Retry;

namespace Cod.Channel.Identity
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddIdentity(this IServiceCollection services, Action<IdentityServiceOptions> options)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddChannel();

            services.Configure<IdentityServiceOptions>(o => { options(o); o.Validate(); });
            services.AddHttpClient("default").AddPolicyHandler(RetryPolicy()).AddPolicyHandler(CircuitBreakerPolicy());
            services.AddTransient<EmailLoginViewModel>();
            services.AddTransient<ICommand<LoginCommandParameter, LoginResult>, LoginCommand>();
            services.AddTransient<ICommand<TOTPLoginCommandParameter, LoginResult>, TOTPLoginCommand>();
            services.AddSingleton<IAuthenticator, DefaultAuthenticator>();
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