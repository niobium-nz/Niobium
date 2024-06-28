using System.Net.Sockets;

namespace Cod.Platform
{
    public static class UriExtensions
    {
        private const int DefaultRetryTimes = 3;

        public static async Task<Stream> FetchStreamAsync(this Uri uri)
        {
            return await FetchStreamAsync(uri, null);
        }

        public static async Task<Stream> FetchStreamAsync(this Uri uri, Func<HttpResponseMessage, Exception, Task> onError)
        {
            return await FetchStreamAsync(uri, onError, DefaultRetryTimes);
        }

        public static async Task<Stream> FetchStreamAsync(this Uri uri, Func<HttpResponseMessage, Exception, Task> onError, int retry)
        {
            if (retry <= 0)
            {
                return null;
            }

            using HttpClient httpclient = new(HttpHandler.GetHandler(), false)
            {
#if !DEBUG
                    Timeout = TimeSpan.FromSeconds(10),
#endif
            };
            HttpResponseMessage resp = await httpclient.GetAsync(uri);
            try
            {
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
                    await onError(resp, null);
                }
            }
            catch (OperationCanceledException e)
            {
                await onError(resp, e);
            }
            catch (SocketException e)
            {
                await onError(resp, e);
            }
            catch (IOException e)
            {
                await onError(resp, e);
            }

            return await FetchStreamAsync(uri, onError, --retry);
        }
    }
}
