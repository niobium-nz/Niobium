using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace Cod.Platform
{
    public static class HttpRequestMessageExtensions
    {
        public static IEnumerable<string> GetRemoteIP(this HttpRequestMessage request)
        {
            if (request.Headers.TryGetValues("X-Forwarded-For", out var values))
            {
                return values.Where(v => !String.IsNullOrWhiteSpace(v))
                       .SelectMany(v => v.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries))
                       .Select(v => v.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries).First());
            }
            return Enumerable.Empty<string>();

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
               => JsonConvert.DeserializeObject<T>(body);

        public static async Task<T> ParseAsync<T>(this HttpRequestMessage request)
            => Parse<T>(await request.Content.ReadAsStringAsync());

        public static bool TryGetAuthorizationCredentials(this HttpRequestMessage request, out string scheme, out string identity, out string credential)
        {
            identity = null;
            credential = null;
            scheme = null;

            var auth = request.Headers.Authorization;
            if (auth == null || string.IsNullOrWhiteSpace(auth.Scheme) || String.IsNullOrWhiteSpace(auth.Parameter))
            {
                return false;
            }

            scheme = auth.Scheme.ToLower();
            var base64EncodedBytes = Convert.FromBase64String(auth.Parameter);
            var credentials = Encoding.UTF8.GetString(base64EncodedBytes).Split(':');
            identity = credentials[0];
            if (credentials.Length >= 2)
            {
                credential = credentials[1];
            }
            return true;
        }

        public static async Task<ClaimsPrincipal> HasClaimAsync<T>(this HttpRequestMessage request, string claim, T value)
        {
            var principal = await TryParsePrincipalAsync(request);
            if (principal == null)
            {
                return null;
            }
            if (!principal.TryGetClaim<T>(claim, out var result))
            {
                return null;
            }
            if (result.Equals(value))
            {
                return principal;
            }
            else
            {
                return null;
            }
        }

        public static async Task<ClaimsPrincipal> TryParsePrincipalAsync(this HttpRequestMessage request, string scheme = "Bearer")
        {
            var auth = request.Headers.Authorization;
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

        private static async Task<ClaimsPrincipal> ValidateAndDecodeAsync(string token)
        {
            var secret = await new ConfigurationProvider().GetSettingAsync(Constant.AUTH_SECRET_NAME);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var validationParameters = new TokenValidationParameters
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
                var claimsPrincipal = new JwtSecurityTokenHandler()
                    .ValidateToken(token, validationParameters, out var rawValidatedToken);

                return claimsPrincipal;
            }
            catch (SecurityTokenValidationException stvex)
            {
                throw new Exception($"Token failed validation: {stvex.Message}");
            }
            catch (ArgumentException argex)
            {
                throw new Exception($"Token was invalid: {argex.Message}");
            }
        }
    }
}
