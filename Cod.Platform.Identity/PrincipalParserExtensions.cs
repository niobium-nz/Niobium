using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Cod.Platform.Identity
{
    public static class PrincipalParserExtensions
    {
        public static async Task<T?> GetClaimAsync<T>(this PrincipalParser helper, HttpRequest request, string claim)
        {
            var principal = await helper.ParseAsync(request);
            return principal.TryGetClaim(claim, out T result) ? result : default;
        }

        public static async Task<IEnumerable<T>> GetClaimsAsync<T>(this PrincipalParser helper, HttpRequest request, string claim)
        {
            var principal = await helper.ParseAsync(request);
            return principal.TryGetClaims(claim, out IEnumerable<T> result) ? result : [];
        }

        public static async Task<ClaimsPrincipal?> HasClaimAsync<T>(this PrincipalParser helper, HttpRequest request, string claim, T value)
        {
            var principal = await helper.ParseAsync(request);
            return !principal.TryGetClaim(claim, out T result) ? null : result!.Equals(value) ? principal : null;
        }

        public static async Task<ClaimsPrincipal?> TryParseAsync(this PrincipalParser helper, HttpRequest request, CancellationToken cancellationToken = default)
        {
            if (!request.TryParseAuthorizationHeader(out string inputScheme, out string parameter) || inputScheme != AuthenticationScheme.BearerLoginScheme)
            {
                return null;
            }

            try
            {
                return await helper.ParseAsync(parameter, cancellationToken);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static async Task<ClaimsPrincipal> ParseAsync(this PrincipalParser helper, HttpRequest request, CancellationToken cancellationToken = default)
        {
            if (!request.TryParseAuthorizationHeader(out string inputScheme, out string parameter) || inputScheme != AuthenticationScheme.BearerLoginScheme)
            {
                throw new ApplicationException(InternalError.AuthenticationRequired);
            }

            return await helper.ParseAsync(parameter, cancellationToken);
        }

        public static async Task<ClaimsPrincipal> ParseIDPrincipalAsync(this PrincipalParser helper, HttpRequest request, string? audience = null, CancellationToken cancellationToken = default)
        {
            if (!request.TryParseAuthorizationHeader(out string inputScheme, out string parameter) || inputScheme != AuthenticationScheme.BearerLoginScheme)
            {
                throw new ApplicationException(InternalError.AuthenticationRequired);
            }

            return await helper.ParseIDPrincipalAsync(parameter, audience: audience, cancellationToken);
        }
    }
}
