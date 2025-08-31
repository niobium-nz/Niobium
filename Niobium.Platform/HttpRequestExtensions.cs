using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System.Net;
using System.Net.Http.Headers;

namespace Niobium.Platform
{
    public static class HttpRequestExtensions
    {
        private const string JsonMediaType = "application/json";
        private static readonly string[] IPv4ReservedIPPrefix =
        [
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
        ];
        private static readonly string[] colonSeparator = [":"];
        private static readonly string[] commaSeparator = [","];

        public static bool TryParseAuthorizationHeader(this HttpRequest request, string headerName, out string scheme, out string parameter)
        {
            parameter = string.Empty;
            scheme = string.Empty;

            if (!request.Headers.TryGetValue(headerName, out StringValues header))
            {
                return false;
            }

            string? auth = header.SingleOrDefault();
            if (string.IsNullOrWhiteSpace(auth))
            {
                return false;
            }

            string[] parts = auth.Split(' ');
            if (parts.Length < 2)
            {
                return false;
            }

            scheme = parts[0];
            parameter = parts[1];
            return true;
        }

        public static bool TryParseAuthorizationHeader(this HttpRequest request, out string scheme, out string parameter)
        {
            return request.TryParseAuthorizationHeader(HeaderNames.Authorization, out scheme, out parameter);
        }

        public static List<string> GetRemoteIPs(this HttpRequest request)
        {
            List<string> result = [];

            if (request.Headers.TryGetValue("X-Forwarded-For", out StringValues values))
            {
                result.AddRange(values.Where(v => !string.IsNullOrWhiteSpace(v))
                    .SelectMany(v => v!.Split(commaSeparator, StringSplitOptions.RemoveEmptyEntries))
                    .Select(v => v.Split(colonSeparator, StringSplitOptions.RemoveEmptyEntries).First()));
            }

            if (request.Headers.TryGetValue("x-forwarded-for", out StringValues values2))
            {
                result.AddRange(values2.Where(v => !string.IsNullOrWhiteSpace(v))
                    .SelectMany(v => v!.Split(commaSeparator, StringSplitOptions.RemoveEmptyEntries))
                    .Select(v => v.Split(colonSeparator, StringSplitOptions.RemoveEmptyEntries).First()));
            }

            if (request.Headers.TryGetValue("CLIENT-IP", out StringValues values3))
            {
                result.AddRange(values3.Where(v => !string.IsNullOrWhiteSpace(v))
                    .SelectMany(v => v!.Split(commaSeparator, StringSplitOptions.RemoveEmptyEntries))
                    .Select(v => v.Split(colonSeparator, StringSplitOptions.RemoveEmptyEntries).First()));
            }

            if (request.Headers.TryGetValue("client-ip", out StringValues values4))
            {
                result.AddRange(values4.Where(v => !string.IsNullOrWhiteSpace(v))
                    .SelectMany(v => v!.Split(commaSeparator, StringSplitOptions.RemoveEmptyEntries))
                    .Select(v => v.Split(colonSeparator, StringSplitOptions.RemoveEmptyEntries).First()));
            }

            result = [.. result.Where(i => IPv4ReservedIPPrefix.All(p => !i.StartsWith(p, StringComparison.InvariantCulture)))];
            if (request.HttpContext.Connection.RemoteIpAddress != null)
            {
                result.Add(request.HttpContext.Connection.RemoteIpAddress.ToString());
            }

            if (result.Count > 0)
            {
                result = [.. result.Distinct()];
            }

            return result;
        }

        public static string? GetRemoteIP(this HttpRequest request)
        {
            return request.GetRemoteIPs().FirstOrDefault();
        }

        public static string? GetTenant(this HttpRequest request)
        {
            string? referer = request.Headers.Referer.SingleOrDefault();
            return referer != null && Uri.TryCreate(referer, UriKind.Absolute, out Uri? refererUri) ? (refererUri?.Host.ToLowerInvariant()) : null;
        }

        public static IActionResult MakeResponse(
            this HttpRequest? request,
            HttpStatusCode? statusCode = null,
            object? payload = null,
            string? contentType = null,
            JsonMarshallingFormat serializationFormat = JsonMarshallingFormat.CamelCase)
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
                result.Content = JsonMarshaller.Marshall(payload, serializationFormat);
                result.ContentType = JsonMediaType;
            }

            return result;
        }

        public static void DeliverAuthenticationToken(this HttpRequest request, string token, string scheme)
        {
            request.HttpContext.Response.Headers.Remove(HeaderNames.WWWAuthenticate);
            request.HttpContext.Response.Headers[HeaderNames.WWWAuthenticate] = new AuthenticationHeaderValue(scheme, token).ToString();
            request.HttpContext.Response.Headers[HeaderNames.AccessControlExposeHeaders] = HeaderNames.WWWAuthenticate;
        }
    }
}
