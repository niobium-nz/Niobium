using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace Cod.Platform
{
    public static class HttpRequestExtensions
    {
        private const string JsonMediaType = "application/json";
        private const string AuthorizationResponseHeaderKey = "WWW-Authenticate";
        private const string AuthorizationRequestHeaderKey = "Authorization";
        private const string ClientIDRequestHeaderKey = "ClientID";
        private const string HeaderCORSKey = "Access-Control-Expose-Headers";
        private static readonly CultureInfo DefaultUICulture = new("en-US");

        public static void Register(this HttpRequest request, ILogger logger)
        {
            Logger.Register(logger);

            if (request != null && request.Headers.TryGetValue("Accept-Language", out StringValues value))
            {
                string[] parts = value.ToString().Split(',');
                if (parts.Length > 0)
                {
                    try
                    {
                        CultureInfo.CurrentUICulture = new CultureInfo(parts[0]);
                    }
                    catch (CultureNotFoundException)
                    {
                        throw new NotSupportedException($"The specified culture is not supported: {value}");
                    }
                    return;
                }
            }

            CultureInfo.CurrentUICulture = DefaultUICulture;
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

            return values.Where(v => !string.IsNullOrWhiteSpace(v))
                       .SelectMany(v => v.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries))
                       .Select(v => v.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries).First())
                       .FirstOrDefault();
        }

        public static void DeliverAuthenticationToken(this HttpRequest request, string token, string scheme)
        {
            if (request.HttpContext.Response.Headers.ContainsKey(AuthorizationResponseHeaderKey))
            {
                request.HttpContext.Response.Headers.Remove(AuthorizationResponseHeaderKey);
            }

            request.HttpContext.Response.Headers.Add(AuthorizationResponseHeaderKey, new AuthenticationHeaderValue(scheme, token).ToString());
            request.HttpContext.Response.Headers.Add(HeaderCORSKey, AuthorizationResponseHeaderKey);
        }

        public static bool TryGetClientID(this HttpRequest request, out string clientID)
        {
            clientID = string.Empty;

            if (request.Headers.TryGetValue(ClientIDRequestHeaderKey, out StringValues val))
            {
                clientID = val.ToString();
                return true;
            }

            return false;
        }

        public static async Task<T> ParseAsync<T>(this HttpRequest request)
        {
            return Parse<T>(await new StreamReader(request.Body).ReadToEndAsync());
        }

        public static async Task<ClaimsPrincipal> HasClaimAsync<T>(this HttpRequest request, string claim, T value)
        {
            ClaimsPrincipal principal = await TryParsePrincipalAsync(request);
            return principal == null ? null : !principal.TryGetClaim<T>(claim, out T result) ? null : result.Equals(value) ? principal : null;
        }

        public static bool TryParseAuthorizationHeader(this HttpRequest request, out string scheme, out string parameter)
        {
            parameter = string.Empty;
            scheme = string.Empty;

            if (!request.Headers.TryGetValue(AuthorizationRequestHeaderKey, out StringValues header))
            {
                return false;
            }

            string auth = header.SingleOrDefault();
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

        public static async Task<ClaimsPrincipal> TryParsePrincipalAsync(this HttpRequest request, string scheme = "Bearer")
        {
            if (!request.TryParseAuthorizationHeader(out string inputScheme, out string parameter)
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

        private static async Task<ClaimsPrincipal> ValidateAndDecodeAsync(string token)
        {
            string secret = ConfigurationProvider.GetSetting(Constants.AUTH_SECRET_NAME);
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
                TokenValidationResult validationResult = await new JsonWebTokenHandler().ValidateTokenAsync(token, validationParameters);
                return validationResult.IsValid ? null : new ClaimsPrincipal(validationResult.ClaimsIdentity);
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

        private static T Parse<T>(string body)
        {
            return JsonSerializer.DeserializeObject<T>(body);
        }
    }
}
