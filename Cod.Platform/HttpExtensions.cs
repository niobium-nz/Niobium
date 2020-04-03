using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Cod.Platform
{
    public static class HttpExtensions
    {
        private const string JsonMediaType = "application/json";
        private const string TextMediaType = "text/plain";

        public static Task<HttpResponseMessage> MakeHttpResponseMessageAsync(this OperationResult operationResult, object successPayload = null, IDictionary<string, string> responseHeaders = null)
        {
            if (operationResult is null)
            {
                throw new ArgumentNullException(nameof(operationResult));
            }

            var result = new HttpResponseMessage();
            object payload = operationResult;
            if (operationResult.IsSuccess)
            {
                result.StatusCode = HttpStatusCode.OK;
                payload = successPayload;
            }
            else if (operationResult.Code < 600)
            {
                result.StatusCode = (HttpStatusCode)operationResult.Code;
            }
            else
            {
                result.StatusCode = HttpStatusCode.InternalServerError;
            }

            if (responseHeaders != null)
            {
                foreach (var key in responseHeaders.Keys)
                {
                    result.Headers.Add(key, responseHeaders[key]);
                }
            }

            if (payload != null)
            {
                if (payload is string str)
                {
                    result.Content = new StringContent(str, Encoding.UTF8, TextMediaType);
                }
                else
                {
                    var json = JsonConvert.SerializeObject(payload);
                    result.Content = new StringContent(json, Encoding.UTF8, JsonMediaType);
                }
            }

            return Task.FromResult(result);
        }

        public static Task<HttpResponseMessage> MakeHttpResponseMessageAsync(this ValidationState validationState)
        {
            if (validationState is null)
            {
                throw new ArgumentNullException(nameof(validationState));
            }

            var result = new HttpResponseMessage(HttpStatusCode.BadRequest);
            var json = JsonConvert.SerializeObject(validationState.ToDictionary());
            result.Content = new StringContent(json, Encoding.UTF8, JsonMediaType);
            return Task.FromResult(result);
        }

        public static Task<HttpResponseMessage> MakeHttpResponseMessageAsync(this HttpRequestMessage requestMessage, HttpStatusCode statusCode, object payload = null)
        {
            var result = new HttpResponseMessage(statusCode);
            if (payload != null)
            {
                if (payload is string str)
                {
                    result.Content = new StringContent(str, Encoding.UTF8, TextMediaType);
                }
                else
                {
                    var json = JsonConvert.SerializeObject(payload);
                    result.Content = new StringContent(json, Encoding.UTF8, JsonMediaType);
                }
            }

            return Task.FromResult(result);
        }

        public static IDictionary<string, string> MakeAuthorizationHeaders(string scheme, string token)
            => new Dictionary<string, string>
            {
                { "WWW-Authenticate", new AuthenticationHeaderValue(scheme, token).ToString() },
                { "Access-Control-Expose-Headers", "WWW-Authenticate" },
            };
    }
}
