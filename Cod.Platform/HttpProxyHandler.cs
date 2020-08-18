using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public static class HttpHandler
    {
        private static readonly ConcurrentDictionary<string, Task<HttpClientHandler>> proxyHandlers = new ConcurrentDictionary<string, Task<HttpClientHandler>>();
        private static readonly Lazy<HttpClientHandler> handler = new Lazy<HttpClientHandler>(() => new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        }, true);

        public static HttpClientHandler GetHandler() => handler.Value;

        public async static Task<HttpClientHandler> GetProxyHandler(string location, IHttpProxyFactory factory = null)
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                throw new ArgumentNullException(nameof(location));
            }
            location = location.Trim().ToUpperInvariant();

            if (proxyHandlers.ContainsKey(location))
            {
                var handler = await proxyHandlers[location];
                var proxy = (IUnstableWebProxy)handler.Proxy;
                var works = await proxy.TestAsync();
                if (works)
                {
                    return handler;
                }
                else
                {
                    proxyHandlers.TryRemove(location, out var _);
                }
            }

            return await proxyHandlers.AddOrUpdate(location, async (l) =>
            {
                IWebProxy proxy = null;
                if (factory != null)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        var p = await factory.CreateAsync(location);
                        var works = await p.TestAsync();
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
                    var cfg = new ConfigurationProvider();
                    var proxyHost = await cfg.GetSettingAsStringAsync(Constant.HTTP_PROXY_HOST);
                    var proxyUsername = await cfg.GetSettingAsStringAsync(Constant.HTTP_PROXY_USERNAME);
                    var proxyPassword = await cfg.GetSettingAsStringAsync(Constant.HTTP_PROXY_PASSWORD);
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
