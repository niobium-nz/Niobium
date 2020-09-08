using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Cod.Channel
{
    public static class HttpClientExtensions
    {
        public static async Task<OperationResult<T>> RequestAsync<T>(
           this HttpClient httpClient,
           HttpMethod method,
           string uri,
           string bearerToken = null,
           object body = null,
           IEnumerable<KeyValuePair<string, string>> headers = null,
           string contentType = null)
        {
            var result = await httpClient.RequestAsync(method, uri, bearerToken, body, headers, contentType);
            if (!result.IsSuccess)
            {
                return new OperationResult<T>(result);
            }

            var response = await result.Result.Content.ReadAsStringAsync();
            var obj = TypeConverter.Convert<T>(response);
            return new OperationResult<T>(obj);
        }

        public static async Task<OperationResult<HttpResponseMessage>> RequestAsync(
            this HttpClient httpClient,
            HttpMethod method,
            string uri,
            string bearerToken = null,
            object body = null,
            IEnumerable<KeyValuePair<string, string>> headers = null,
            string contentType = null,
            int retry = 0)
        {
            if (retry > 0)
            {
                // TODO (5he11) 界面友好提示用户网络有点慢
            }

            if (retry >= 3)
            {
                // TODO (5he11) 界面友好提示用户网络异常
                return new OperationResult<HttpResponseMessage>(InternalError.GatewayTimeout);
            }

            using (var request = new HttpRequestMessage(method, uri))
            {
                if (headers != null && headers.Any())
                {
                    foreach (var header in headers)
                    {
                        request.Headers.Add(header.Key, header.Value);
                    }
                }

                if (!String.IsNullOrWhiteSpace(bearerToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
                }

                try
                {
                    if (body != null)
                    {
                        string content;
                        if (body is string stringBody)
                        {
                            content = stringBody;
                        }
                        else
                        {
                            content = JsonConvert.SerializeObject(body);
                            contentType = "application/json";
                        }
                        request.Content = new StringContent(content);
                        if (contentType != null)
                        {
                            request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                        }
                    }

                    var response = await httpClient.SendAsync(request);
                    var status = (int)response.StatusCode;
                    var code = status;
                    if (code >= 200 && code < 400)
                    {
                        code = OperationResult.SuccessCode;
                    }

                    var result = new OperationResult<HttpResponseMessage>(response)
                    {
                        Code = code
                    };
                    return result;
                }
                catch (Exception)
                {
                    return await RequestAsync(httpClient,
                        method,
                        uri,
                        bearerToken: bearerToken,
                        body: body,
                        headers: headers,
                        contentType: contentType,
                        retry: ++retry);
                }
                finally
                {
                    if (request.Content != null && request.Content is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }
        }
    }
}
