using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public static class UriExtensions
    {
        public static async Task<Stream> FetchStreamAsync(this Uri uri, int retry = 3)
        {
            if (retry <= 0)
            {
                return null;
            }

            try
            {
                using (var httpclient = new HttpClient(HttpHandler.GetHandler(), false)
                {
#if !DEBUG
                    Timeout = TimeSpan.FromSeconds(2),
#endif
                })
                {
                    var resp = await httpclient.GetAsync(uri);
                    var status = (int)resp.StatusCode;
                    if (status >= 200 && status < 400)
                    {
                        using (var s = await resp.Content.ReadAsStreamAsync())
                        {
                            var ms = new MemoryStream((int)s.Length);
                            await s.CopyToAsync(ms);
                            return ms;
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
            }
            catch (SocketException)
            {
            }
            catch (IOException)
            {
            }

            return await FetchStreamAsync(uri, --retry);
        }
    }
}
