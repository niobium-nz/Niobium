using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace Cod.Platform.Identity
{
    public static class HttpRequestExtensions
    {
        private const string AuthorizationResponseHeaderKey = "WWW-Authenticate";
        private const string AuthorizationRequestHeaderKey = "Authorization";
        private const string HeaderCORSKey = "Access-Control-Expose-Headers";

        public static void DeliverAuthenticationToken(this HttpRequest request, string? token, string scheme)
        {
            if (request.HttpContext.Response.Headers.ContainsKey(AuthorizationResponseHeaderKey))
            {
                request.HttpContext.Response.Headers.Remove(AuthorizationResponseHeaderKey);
            }

            request.HttpContext.Response.Headers.Add(AuthorizationResponseHeaderKey, new AuthenticationHeaderValue(scheme, token).ToString());
            request.HttpContext.Response.Headers.Add(HeaderCORSKey, AuthorizationResponseHeaderKey);
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

            if (!request.Headers.TryGetValue(AuthorizationRequestHeaderKey, out StringValues header))
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

        public static async Task<ClaimsPrincipal> TryParsePrincipalAsync(this HttpRequest request, SecurityKey? key = null)
        {
            if (!request.TryParseAuthorizationHeader(out string inputScheme, out string parameter) || inputScheme != AuthenticationScheme.BasicLoginScheme)
            {
                throw new ApplicationException(InternalError.AuthenticationRequired);
            }

            if (IdentityServiceOptions.Instance == null)
            {
                throw new InvalidOperationException($"'{nameof(DependencyModule.AddPlatformIdentity)}' must be called at startup.");
            }

            if (IdentityServiceOptions.Instance.AccessTokenSecret == null)
            {
                throw new InvalidOperationException($"'{nameof(IdentityServiceOptions.AccessTokenSecret)}' must be configured at startup.");
            }

            try
            {
                key ??= new SymmetricSecurityKey(Encoding.UTF8.GetBytes(IdentityServiceOptions.Instance.AccessTokenSecret));
                return await ValidateAndDecodeJWTAsync(parameter, key, IdentityServiceOptions.Instance.AccessTokenIssuer, IdentityServiceOptions.Instance.AccessTokenAudience);
            }
            catch (Exception)
            {
                throw new ApplicationException(InternalError.AuthenticationRequired);
            }
        }

        internal static async Task<ClaimsPrincipal> ValidateAndDecodeJWTAsync(string jwt, SecurityKey key, string issuer, string audience)
        {
            TokenValidationParameters validationParameters = new()
            {
                ClockSkew = TimeSpan.FromMinutes(5),
                IssuerSigningKeys = new[] { key },
                RequireSignedTokens = true,
                RequireExpirationTime = true,
                ValidateLifetime = true,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateIssuer = true,
                ValidIssuer = issuer
            };

            try
            {
                TokenValidationResult validationResult = await new JsonWebTokenHandler().ValidateTokenAsync(jwt, validationParameters);
                if (!validationResult.IsValid)
                {
                    throw new ApplicationException(InternalError.AuthenticationRequired);
                }

                return new ClaimsPrincipal(validationResult.ClaimsIdentity);
            }
            catch (SecurityTokenValidationException stvex)
            {
                throw new InvalidDataException($"Token failed validation: {stvex.Message}");
            }
            catch (ArgumentException argex)
            {
                throw new ArgumentException($"Token was invalid: {argex.Message}");
            }
        }
    }
}
