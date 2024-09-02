using System.Collections.Concurrent;
using System.Net;

namespace Cod.Platform
{
    public static class HttpHandler
    {
        private static readonly ConcurrentDictionary<string, Task<HttpClientHandler>> proxyHandlers = new();
        private static readonly Lazy<HttpClientHandler> handler = new(() => new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        }, true);

        public static HttpClientHandler GetHandler()
        {
            return handler.Value;
        }

        public static async Task<HttpClientHandler> GetProxyHandler(string location, IHttpProxyFactory factory = null)
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                throw new ArgumentNullException(nameof(location));
            }
            location = location.Trim().ToUpperInvariant();

            if (proxyHandlers.ContainsKey(location))
            {
                HttpClientHandler handler = await proxyHandlers[location];
                IUnstableWebProxy proxy = (IUnstableWebProxy)handler.Proxy;
                bool works = await proxy.TestAsync();
                if (works)
                {
                    return handler;
                }
                else
                {
                    proxyHandlers.TryRemove(location, out Task<HttpClientHandler> _);
                }
            }

            return await proxyHandlers.AddOrUpdate(location, async (l) =>
            {
                IWebProxy proxy = null;
                if (factory != null)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        IUnstableWebProxy p = await factory.CreateAsync(location);
                        bool works = await p.TestAsync();
                        if (works)
                        {
                            proxy = p;
                            break;
                        }
                    }

                    if (proxy == null)
                    {
                        throw new InvalidOperationException("No suitable proxy HTTP handler can be found.");
                    }
                }
                else
                {
                    ConfigurationProvider cfg = new();
                    string proxyHost = await cfg.GetSettingAsStringAsync(Constants.HTTP_PROXY_HOST);
                    string proxyUsername = await cfg.GetSettingAsStringAsync(Constants.HTTP_PROXY_USERNAME);
                    string proxyPassword = await cfg.GetSettingAsStringAsync(Constants.HTTP_PROXY_PASSWORD);
                    proxy = new WebProxy
                    {
                        Address = new Uri(proxyHost),
                        BypassProxyOnLocal = false,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(
                            userName: proxyUsername,
                            password: proxyPassword)
                    };
                }

                return new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    Proxy = proxy,
                };
            },
            async (l, h) => await h);
        }
    }
}
