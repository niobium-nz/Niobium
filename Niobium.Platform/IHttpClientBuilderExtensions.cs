using Microsoft.Extensions.DependencyInjection;

namespace Niobium.Platform
{
    public static class IHttpClientBuilderExtensions
    {
        /// <summary>
        /// Configures automatic redirects on the client's existing HTTP transport.
        /// </summary>
        /// <remarks>
        /// Call after selecting a custom primary handler. Supports both the factory's
        /// default SocketsHttpHandler and HttpClientHandler without replacing them or
        /// changing their other settings. This does not change resilience policies.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// The primary handler does not expose a supported redirect setting.
        /// </exception>
        public static IHttpClientBuilder ConfigureAutoRedirect(this IHttpClientBuilder builder, bool allowAutoRedirect)
        {
            return builder.ConfigurePrimaryHttpMessageHandler((handler, _) =>
            {
                switch (handler)
                {
                    case SocketsHttpHandler sockets:
                        sockets.AllowAutoRedirect = allowAutoRedirect;
                        break;
                    case HttpClientHandler client:
                        client.AllowAutoRedirect = allowAutoRedirect;
                        break;
                    default:
                        throw new InvalidOperationException("Automatic redirects require a SocketsHttpHandler or HttpClientHandler primary handler.");
                }
            });
        }
    }
}