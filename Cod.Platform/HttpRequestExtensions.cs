using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace Cod.Platform
{
    public static class HttpRequestExtensions
    {
        private const string JsonMediaType = "application/json";
        private const string AuthorizationResponseHeaderKey = "WWW-Authenticate";
        private const string AuthorizationRequestHeaderKey = "Authorization";
        private const string ClientIDRequestHeaderKey = "ClientID";
        private const string HeaderCORSKey = "Access-Control-Expose-Headers";

        public static IActionResult MakeResponse(
            this HttpRequest request,
            HttpStatusCode? statusCode = null,
            object payload = null,
            string contentType = null,
            JsonSerializerSettings serializerSettings = null)
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

            var result = new ContentResult
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
                result.Content = JsonConvert.SerializeObject(payload, serializerSettings);
                result.ContentType = JsonMediaType;
            }

            return result;
        }

        public static IEnumerable<string> GetRemoteIP(this HttpRequest request)
        {
            if (request.Headers.TryGetValue("X-Forwarded-For", out var values))
            {
                return values.Where(v => !String.IsNullOrWhiteSpace(v))
                       .SelectMany(v => v.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries))
                       .Select(v => v.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries).First());
            }
            return Enumerable.Empty<string>();

        }

        public static void DeliverAuthenticationToken(this HttpRequest request, string token, string scheme)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            if (request.HttpContext.Response.Headers.ContainsKey(AuthorizationResponseHeaderKey))
            {
                request.HttpContext.Response.Headers.Remove(AuthorizationResponseHeaderKey);
            }

            request.HttpContext.Response.Headers.Add(AuthorizationResponseHeaderKey, new AuthenticationHeaderValue(scheme, token).ToString());
            request.HttpContext.Response.Headers.Add(HeaderCORSKey, AuthorizationResponseHeaderKey);
        }

        public static bool TryGetClientID(this HttpRequestMessage request, out string clientID)
        {
            clientID = String.Empty;

            if (request.Headers.Contains(ClientIDRequestHeaderKey))
            {
                clientID = request.Headers.GetValues(ClientIDRequestHeaderKey).SingleOrDefault();
                return true;
            }

            return false;
        }

        public static async Task<T> ParseAsync<T>(this HttpRequest request)
            => Parse<T>(await new StreamReader(request.Body).ReadToEndAsync());

        public static bool TryGetAuthorizationCredentials(this HttpRequest request, out string scheme, out string identity, out string credential)
        {
            identity = null;
            credential = null;

            if (!request.TryParseAuthorizationHeader(out scheme, out var parameter))
            {
                return false;
            }

            scheme = scheme.ToLowerInvariant();
            var base64EncodedBytes = Convert.FromBase64String(parameter);
            var credentials = Encoding.UTF8.GetString(base64EncodedBytes).Split(':');
            identity = credentials[0];
            if (credentials.Length >= 2)
            {
                credential = credentials[1];
            }
            return true;
        }

        public static bool TryParseAuthorizationHeader(this HttpRequest request, out string scheme, out string parameter)
        {
            parameter = null;
            scheme = null;

            if (!request.Headers.TryGetValue(AuthorizationRequestHeaderKey, out var header))
            {
                return false;
            }

            var auth = header.SingleOrDefault();
            if (String.IsNullOrWhiteSpace(auth))
            {
                return false;
            }

            var parts = auth.Split(' ');
            if (parts.Length < 2)
            {
                return false;
            }

            scheme = parts[0];
            parameter = parts[1];
            return true;
        }

        public static async Task<ClaimsPrincipal> HasClaimAsync<T>(this HttpRequest request, string claim, T value)
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

        public static async Task<ClaimsPrincipal> TryParsePrincipalAsync(this HttpRequest request, string scheme = "Bearer")
        {
            if (!request.TryParseAuthorizationHeader(out var inputScheme, out var parameter)
                || inputScheme.ToUpperInvariant() != scheme.ToUpperInvariant())
            {
                return null;
            }

            try
            {
                return await ValidateAndDecodeAsync(parameter);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static Task<ClaimsPrincipal> ValidateAndDecodeAsync(string token)
        {
            var secret = ConfigurationProvider.GetSetting(Constant.AUTH_SECRET_NAME);
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

                return Task.FromResult(claimsPrincipal);
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
        private static T Parse<T>(string body) => JsonConvert.DeserializeObject<T>(body);
    }
}
