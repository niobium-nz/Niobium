using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Cod.Channel
{
    public static class HttpClientExtensions
    {
        public async static Task<OperationResult<T>> RequestAsync<T>(
           this HttpClient httpClient,
           HttpMethod method,
           string uri,
           string bearerToken = null,
           object body = null)
        {
            var result = await httpClient.RequestAsync(method, uri, bearerToken, body);
            T obj = default;
            if (result.IsSuccess)
            {
                if (typeof(T) == typeof(string))
                {
                    obj = (T)Convert.ChangeType(result.Message, typeof(T));
                }
                else
                {
                    obj = JsonConvert.DeserializeObject<T>(result.Message);
                }
            }
            return new OperationResult<T>(result.Code, obj)
            {
                Reference = result.Reference,
                Message = result.Message,
            };
        }

        public async static Task<OperationResult> RequestAsync(
            this HttpClient httpClient,
            HttpMethod method,
            string uri,
            string bearerToken = null,
            object body = null)
        {
            using (var request = new HttpRequestMessage(method, uri))
            {
                if (!string.IsNullOrWhiteSpace(bearerToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
                }

                try
                {
                    if (body != null)
                    {
                        string content;
                        string contentType = null;
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
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var code = status;
                    if (code >= 200 && code < 400)
                    {
                        code = OperationResult.SuccessCode;
                    }
                    return OperationResult.Create(code, responseBody);
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
