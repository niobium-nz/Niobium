using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System.Net.Http.Headers;

namespace Cod.Platform.Identity
{
    public static class HttpRequestExtensions
    {
        public static void DeliverToken(this HttpRequest request, string? token, string scheme)
        {
            request.HttpContext.Response.Headers.Authorization = new AuthenticationHeaderValue(scheme, token).ToString();
            request.HttpContext.Response.Headers.AccessControlExposeHeaders = HeaderNames.Authorization;
        }

        public static void DeliverChallenge(this HttpRequest request, string? token, string scheme)
        {
            request.HttpContext.Response.Headers.WWWAuthenticate = new AuthenticationHeaderValue(scheme, token).ToString();
            request.HttpContext.Response.Headers.AccessControlExposeHeaders = HeaderNames.WWWAuthenticate;
        }
    }
}
