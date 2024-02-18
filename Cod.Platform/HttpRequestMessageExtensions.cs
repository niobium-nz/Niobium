using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace Cod.Platform
{
    public static class HttpRequestMessageExtensions
    {
        public static IEnumerable<string> GetRemoteIP(this HttpRequestMessage request)
        {
            return request.Headers.TryGetValues("X-Forwarded-For", out IEnumerable<string> values)
                ? values.Where(v => !string.IsNullOrWhiteSpace(v))
                       .SelectMany(v => v.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries))
                       .Select(v => v.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries).First())
                : Enumerable.Empty<string>();
        }

        public static void DeliverAuthenticationToken(this HttpResponseMessage response, string token, string scheme)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }
            response.Headers.Add("WWW-Authenticate", new AuthenticationHeaderValue(scheme, token).ToString());
            response.Headers.Add("Access-Control-Expose-Headers", "WWW-Authenticate");
        }

        public static T Parse<T>(string body)
        {
            return JsonSerializer.DeserializeObject<T>(body);
        }

        public static async Task<T> ParseAsync<T>(this HttpRequestMessage request)
        {
            return Parse<T>(await request.Content.ReadAsStringAsync());
        }

        public static bool TryGetAuthorizationCredentials(this HttpRequestMessage request, out string scheme, out string identity, out string credential)
        {
            identity = null;
            credential = null;
            scheme = null;

            AuthenticationHeaderValue auth = request.Headers.Authorization;
            if (auth == null || string.IsNullOrWhiteSpace(auth.Scheme) || string.IsNullOrWhiteSpace(auth.Parameter))
            {
                return false;
            }

            scheme = auth.Scheme.ToLowerInvariant();
            byte[] base64EncodedBytes = Convert.FromBase64String(auth.Parameter);
            string[] credentials = Encoding.UTF8.GetString(base64EncodedBytes).Split(':');
            identity = credentials[0];
            if (credentials.Length >= 2)
            {
                credential = credentials[1];
            }
            return true;
        }

        public static async Task<ClaimsPrincipal> HasClaimAsync<T>(this HttpRequestMessage request, string claim, T value)
        {
            ClaimsPrincipal principal = await TryParsePrincipalAsync(request);
            if (principal == null)
            {
                return null;
            }
            return !principal.TryGetClaim<T>(claim, out T result) ? null : result.Equals(value) ? principal : null;
        }

        public static async Task<ClaimsPrincipal> TryParsePrincipalAsync(this HttpRequestMessage request, string scheme = "Bearer")
        {
            AuthenticationHeaderValue auth = request.Headers.Authorization;
            if (auth == null || auth.Scheme.ToLowerInvariant() != scheme.ToLowerInvariant())
            {
                return null;
            }
            try
            {
                return await ValidateAndDecodeAsync(auth.Parameter);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static Task<ClaimsPrincipal> ValidateAndDecodeAsync(string token)
        {
            string secret = ConfigurationProvider.GetSetting(Constant.AUTH_SECRET_NAME);
            SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(secret));
            TokenValidationParameters validationParameters = new()
            {
                ClockSkew = TimeSpan.FromMinutes(5),
                IssuerSigningKeys = new[] { key },
                RequireSignedTokens = true,
                RequireExpirationTime = true,
                ValidateLifetime = true,
                ValidateAudience = true,
                ValidAudience = "cod.client",
                ValidateIssuer = true,
                ValidIssuer = "cod.platform"
            };

            try
            {
                ClaimsPrincipal claimsPrincipal = new JwtSecurityTokenHandler()
                    .ValidateToken(token, validationParameters, out SecurityToken rawValidatedToken);

                return Task.FromResult(claimsPrincipal);
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
