using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System.Net.Http.Headers;
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

        public static List<string> GetRemoteIPs(this HttpRequest request)
        {
            var result = new List<string>();

            if (request.Headers.TryGetValue("X-Forwarded-For", out var values))
            {
                result.AddRange(values.Where(v => !string.IsNullOrWhiteSpace(v))
                    .SelectMany(v => v.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries))
                    .Select(v => v.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries).First()));
            }

            if (request.Headers.TryGetValue("x-forwarded-for", out var values2))
            {
                result.AddRange(values2.Where(v => !string.IsNullOrWhiteSpace(v))
                    .SelectMany(v => v.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries))
                    .Select(v => v.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries).First()));
            }

            if (request.Headers.TryGetValue("CLIENT-IP", out var values3))
            {
                result.AddRange(values3.Where(v => !string.IsNullOrWhiteSpace(v))
                    .SelectMany(v => v.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries))
                    .Select(v => v.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries).First()));
            }

            if (request.Headers.TryGetValue("client-ip", out var values4))
            {
                result.AddRange(values4.Where(v => !string.IsNullOrWhiteSpace(v))
                    .SelectMany(v => v.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries))
                    .Select(v => v.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries).First()));
            }

            result = result.Where(i => IPv4ReservedIPPrefix.All(p => !i.StartsWith(p, StringComparison.InvariantCulture))).ToList();
            if (request.HttpContext.Connection.RemoteIpAddress != null)
            {
                result.Add(request.HttpContext.Connection.RemoteIpAddress.ToString());
            }

            if (result.Count > 0)
            {
                result = result.Distinct().ToList();
            }

            return result;
        }

        public static string GetRemoteIP(this HttpRequest request)
        {
            return GetRemoteIPs(request).FirstOrDefault();
        }

        public static string GetTenant(this HttpRequest request)
        {
            var referer = request.Headers.Referer.SingleOrDefault();
            if (referer != null && Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
            {
                return refererUri?.Host.ToLowerInvariant();
            }

            return null;
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

        public static void DeliverAuthenticationToken(this HttpRequest request, string token, string scheme)
        {
            if (request.HttpContext.Response.Headers.ContainsKey(HeaderNames.WWWAuthenticate))
            {
                request.HttpContext.Response.Headers.Remove(HeaderNames.WWWAuthenticate);
            }

            request.HttpContext.Response.Headers[HeaderNames.WWWAuthenticate] = new AuthenticationHeaderValue(scheme, token).ToString();
            request.HttpContext.Response.Headers[HeaderNames.AccessControlExposeHeaders] = HeaderNames.WWWAuthenticate;
        }
    }
}
