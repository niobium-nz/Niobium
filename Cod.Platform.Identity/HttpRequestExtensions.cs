using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using System.Net.Http.Headers;
using System.Security.Claims;

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

        public static async Task<ClaimsPrincipal?> HasClaimAsync<T>(this HttpRequest request, string claim, T value)
        {
            var principal = await TryParsePrincipalAsync(request);
            return !principal.TryGetClaim<T>(claim, out T result) ? null : result!.Equals(value) ? principal : null;
        }

        public static bool TryParseAuthorizationHeader(this HttpRequest request, out string scheme, out string parameter)
        {
            parameter = string.Empty;
            scheme = string.Empty;

            if (!request.Headers.TryGetValue(HeaderNames.Authorization, out StringValues header))
            {
                return false;
            }

            var auth = header.SingleOrDefault();
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

        public static async Task<ClaimsPrincipal> TryParsePrincipalAsync(this HttpRequest request, SecurityKey? key = null, string? issuer = null, string? audience = null, CancellationToken cancellationToken = default)
        {
            if (!request.TryParseAuthorizationHeader(out string inputScheme, out string parameter) || inputScheme != AuthenticationScheme.BearerLoginScheme)
            {
                throw new ApplicationException(InternalError.AuthenticationRequired);
            }

            return await AccessTokenHelper.TryParsePrincipalAsync(parameter, key, issuer, audience, cancellationToken);
        }
    }
}
