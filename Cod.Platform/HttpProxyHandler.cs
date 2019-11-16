using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public static class HttpHandler
    {
        private static HttpClientHandler proxyHandler;
        private static readonly Lazy<HttpClientHandler> handler = new Lazy<HttpClientHandler>(() => new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        }, true);

        public static HttpClientHandler GetHandler() => handler.Value;

        public static async Task<HttpClientHandler> GetProxyHandler()
        {
            if (proxyHandler == null)
            {
                var cfg = new ConfigurationProvider();
                var proxyHost = await cfg.GetSettingAsync(Constant.HTTP_PROXY_HOST);
                var proxyUsername = await cfg.GetSettingAsync(Constant.HTTP_PROXY_USERNAME);
                var proxyPassword = await cfg.GetSettingAsync(Constant.HTTP_PROXY_PASSWORD);

                proxyHandler = new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    Proxy = new WebProxy
                    {
                        Address = new Uri(proxyHost),
                        BypassProxyOnLocal = false,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(
                        userName: proxyUsername,
                        password: proxyPassword)
                    }
                };
            }
            return proxyHandler;
        }
    }
}
