using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;

namespace Niobium.Platform
{
    public static class IHttpClientBuilderExtensions
    {
        /// <summary>
        /// Configures automatic redirects on the client's existing HTTP transport.
        /// </summary>
        /// <remarks>
        /// Applies after client-specific handler selection. A client's explicit setting
        /// overrides shared defaults regardless of registration order. Supports both
        /// SocketsHttpHandler and HttpClientHandler without replacing them or changing
        /// their other settings. This does not change resilience policies.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// The primary handler does not expose a supported redirect setting.
        /// </exception>
        public static IHttpClientBuilder ConfigureAutoRedirect(this IHttpClientBuilder builder, bool allowAutoRedirect)
        {
            builder.Services.AddSingleton(new RedirectPolicy(builder.Name, allowAutoRedirect));
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHttpMessageHandlerBuilderFilter, RedirectPolicyFilter>());
            return builder;
        }

        // A null name denotes ConfigureHttpClientDefaults; an empty name is the
        // ordinary unnamed client. Keep these distinct so explicit client settings win.
        private sealed record RedirectPolicy(string? Name, bool AllowAutoRedirect);

        private sealed class RedirectPolicyFilter(IEnumerable<RedirectPolicy> policies) : IHttpMessageHandlerBuilderFilter
        {
            public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
            {
                return builder =>
                {
                    next(builder);
                    RedirectPolicy? policy = policies.LastOrDefault(p => p.Name == builder.Name)
                        ?? policies.LastOrDefault(p => p.Name is null);
                    if (policy is null)
                    {
                        return;
                    }

                    switch (builder.PrimaryHandler)
                    {
                        case SocketsHttpHandler sockets:
                            sockets.AllowAutoRedirect = policy.AllowAutoRedirect;
                            break;
                        case HttpClientHandler client:
                            client.AllowAutoRedirect = policy.AllowAutoRedirect;
                            break;
                        default:
                            throw new InvalidOperationException("Automatic redirects require a SocketsHttpHandler or HttpClientHandler primary handler.");
                    }
                };
            }
        }
    }
}