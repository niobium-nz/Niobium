using Microsoft.Extensions.Logging;
using System.Net.Sockets;

namespace Niobium.Platform
{
    public static class UriExtensions
    {
        private const int DefaultRetryTimes = 3;

        public static async Task<Stream?> FetchStreamAsync(this Uri uri)
        {
            return await uri.FetchStreamAsync(null);
        }

        public static async Task<Stream?> FetchStreamAsync(this Uri uri, Func<HttpResponseMessage, Task>? onError)
        {
            return await uri.FetchStreamAsync(onError, DefaultRetryTimes);
        }

        public static async Task<Stream?> FetchStreamAsync(this Uri uri, Func<HttpResponseMessage, Task>? onError, int retry)
        {
            if (retry <= 0)
            {
                return null;
            }


            try
            {
                using HttpClient httpclient = new(HttpHandler.GetHandler(), false)
                {
#if !DEBUG
                    Timeout = TimeSpan.FromSeconds(10),
#endif
                };
                HttpResponseMessage resp = await httpclient.GetAsync(uri);
                if (resp.IsSuccessStatusCode)
                {
                    using Stream s = await resp.Content.ReadAsStreamAsync();
                    MemoryStream responseStream = new((int)s.Length);
                    await s.CopyToAsync(responseStream);
                    responseStream.Seek(0, SeekOrigin.Begin);
                    return responseStream;
                }
                else if (onError != null)
                {
                    await onError(resp);
                }
            }
            catch (OperationCanceledException e)
            {
                Logger.Instance?.LogError(e?.Message);
            }
            catch (SocketException e)
            {
                Logger.Instance?.LogError(e?.Message);
            }
            catch (IOException e)
            {
                Logger.Instance?.LogError(e?.Message);
            }

            return await uri.FetchStreamAsync(onError, --retry);
        }
    }
}
