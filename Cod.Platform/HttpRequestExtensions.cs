using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Net;

namespace Cod.Platform
{
    public static class HttpRequestExtensions
    {
        private const string JsonMediaType = "application/json";
        private static readonly string[] IPv4ReservedIPPrefix =
        {
            "0.",   //0.0.0.0每0.255.255.255
            "10.",  //10.0.0.0每10.255.255.255
            "127.",  //127.0.0.0每127.255.255.255
            "169.254.",  //169.254.0.0每169.254.255.255
            "192.168.",  //192.168.0.0每192.168.255.255
            "198.18.",  //198.18.0.0每198.18.255.255	
            "198.19.",  //198.19.0.0每198.19.255.255	
            "172.16.",  //172.16.0.0每172.31.255.255
            "172.17.",  //172.16.0.0每172.31.255.255
            "172.18.",  //172.16.0.0每172.31.255.255
            "172.19.",  //172.16.0.0每172.31.255.255
            "172.20.",  //172.16.0.0每172.31.255.255
            "172.21.",  //172.16.0.0每172.31.255.255
            "172.22.",  //172.16.0.0每172.31.255.255
            "172.23.",  //172.16.0.0每172.31.255.255
            "172.24.",  //172.16.0.0每172.31.255.255
            "172.25.",  //172.16.0.0每172.31.255.255
            "172.26.",  //172.16.0.0每172.31.255.255
            "172.27.",  //172.16.0.0每172.31.255.255
            "172.28.",  //172.16.0.0每172.31.255.255
            "172.29.",  //172.16.0.0每172.31.255.255
            "172.30.",  //172.16.0.0每172.31.255.255
            "172.31.",  //172.16.0.0每172.31.255.255
        };

        public static string GetRemoteIP(this HttpRequest request)
        {
            if (!request.Headers.TryGetValue("X-Forwarded-For", out StringValues values))
            {
                if (!request.Headers.TryGetValue("x-forwarded-for", out values))
                {
                    if (!request.Headers.TryGetValue("CLIENT-IP", out values))
                    {
                        return null;
                    }
                }
            }

            var ips = values.Where(v => !string.IsNullOrWhiteSpace(v))
                       .SelectMany(v => v.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries))
                       .Select(v => v.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries).First());
            return ips.Where(i => IPv4ReservedIPPrefix.All(p => !i.StartsWith(p, StringComparison.InvariantCulture))).FirstOrDefault();
        }

        public static IActionResult MakeResponse(
            this HttpRequest request,
            HttpStatusCode? statusCode = null,
            object payload = null,
            string contentType = null,
            JsonSerializationFormat? serializationFormat = null)
        {
            int? code = null;
            if (statusCode.HasValue)
            {
                code = (int)statusCode.Value;
            }

            if (payload == null)
            {
                return new StatusCodeResult(code ?? 200);
            }

            ContentResult result = new()
            {
                StatusCode = code,
            };
            if (payload is string str)
            {
                result.Content = str;
                result.ContentType = contentType;
            }
            else
            {
                result.Content = JsonSerializer.SerializeObject(payload, serializationFormat);
                result.ContentType = JsonMediaType;
            }

            return result;
        }
    }
}
