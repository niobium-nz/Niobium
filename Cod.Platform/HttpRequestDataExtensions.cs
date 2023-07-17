using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Cod.Platform
{
    public static class HttpRequestDataExtensions
    {
        private const string JsonMediaType = "application/json";
        private const string ContentTypeResponseHeaderKey = "Content-Type";
        private const string AuthorizationResponseHeaderKey = "WWW-Authenticate";
        private const string AuthorizationRequestHeaderKey = "Authorization";
        private const string ClientIDRequestHeaderKey = "ClientID";
        private const string HeaderCORSKey = "Access-Control-Expose-Headers";
        private static readonly CultureInfo DefaultUICulture = new("en-US");

        public static void Register(this HttpRequestData request, ILogger logger)
        {
            Logger.Register(logger);
            if (request != null && request.Headers.TryGetValues("Accept-Language", out var values))
            {
                if (values.Any())
                {
                    CultureInfo.CurrentUICulture = new CultureInfo(values.First());
                    return;
                }
            }

            CultureInfo.CurrentUICulture = DefaultUICulture;
        }

        public static async Task<HttpResponseData> MakeResponse(
            this HttpRequestData request,
            HttpStatusCode? statusCode = null,
            object payload = null,
            string contentType = null,
            ObjectSerializer serializer = null)
        {
            HttpStatusCode? code = null;
            if (statusCode.HasValue)
            {
                code = statusCode.Value;
            }

            var result = request.CreateResponse(code ?? HttpStatusCode.OK);
            if (payload == null)
            {
                return result;
            }

            if (payload is string str)
            {
                result.Headers.Add(ContentTypeResponseHeaderKey, contentType);
                await result.WriteStringAsync(str, Encoding.UTF8);
            }
            else
            {
                if (String.IsNullOrWhiteSpace(contentType))
                {
                    contentType = JsonMediaType;
                }

                if (serializer == null)
                {
                    await result.WriteAsJsonAsync(payload, contentType);
                }
                else
                {
                    await result.WriteAsJsonAsync(payload, serializer, contentType);
                }
            }

            return result;
        }

        public static IEnumerable<string> GetRemoteIP(this HttpRequestData request) => request.Headers.TryGetValues("X-Forwarded-For", out var values)
                ? values.Where(v => !String.IsNullOrWhiteSpace(v))
                       .SelectMany(v => v.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries))
                       .Select(v => v.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries).First())
                : Enumerable.Empty<string>();

        public static HttpResponseData CreateResponseWithAuthenticationToken(this HttpRequestData request, string token, string scheme)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            var response = request.CreateResponse();
            response.Headers.Add(AuthorizationResponseHeaderKey, new AuthenticationHeaderValue(scheme, token).ToString());
            response.Headers.Add(HeaderCORSKey, AuthorizationResponseHeaderKey);
            return response;
        }

        public static bool TryGetClientID(this HttpRequestData request, out string clientID)
        {
            clientID = String.Empty;

            if (request.Headers.TryGetValues(ClientIDRequestHeaderKey, out var val))
            {
                clientID = val.SingleOrDefault();
                return !String.IsNullOrWhiteSpace(clientID);
            }

            return false;
        }

        public static async Task<T> ParseAsync<T>(this HttpRequestData request)
            => Parse<T>(await new StreamReader(request.Body).ReadToEndAsync());

        public static bool TryGetAuthorizationCredentials(this HttpRequestData request, out string scheme, out string identity, out string credential)
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

        public static bool TryParseAuthorizationHeader(this HttpRequestData request, out string scheme, out string parameter)
        {
            parameter = null;
            scheme = null;

            if (!request.Headers.TryGetValues(AuthorizationRequestHeaderKey, out var header))
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

        public static async Task<ClaimsPrincipal> HasClaimAsync<T>(this HttpRequestData request, string claim, T value)
        {
            var principal = await TryParsePrincipalAsync(request);
            if (principal == null)
            {
                return null;
            }
            return !principal.TryGetClaim<T>(claim, out var result) ? null : result.Equals(value) ? principal : null;
        }

        public static async Task<ClaimsPrincipal> TryParsePrincipalAsync(this HttpRequestData request, string scheme = "Bearer")
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

        public static async Task<OperationResult<T>> ValidateSignatureAndParseAsync<T>(this HttpRequestData req, string secret, byte[] tenant = null)
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            if (String.IsNullOrWhiteSpace(requestBody))
            {
                return new OperationResult<T>(InternalError.BadRequest);
            }

            var model = JsonSerializer.DeserializeObject<T>(requestBody);
            _ = ValidationHelper.TryValidate(model, out var validation);

            string requestSignature = null;
            if (req.Headers.TryGetValues("ETag", out var etag))
            {
                requestSignature = etag.SingleOrDefault();
            }

            if (String.IsNullOrWhiteSpace(requestSignature))
            {
                validation.AddError("ETag", Localization.SignatureMissing);
            }

            if (!validation.IsValid)
            {
                return new OperationResult<T>(validation.ToOperationResult());
            }

            if (model is ITenantOwned tenantOwned)
            {
                tenant = tenantOwned.GetTenantAuthenticationIdentifier();
            }

            if (tenant == null)
            {
                throw new NotSupportedException();
            }

            var stringToSign = $"{req.Url.AbsolutePath}?{requestBody}";
            var tenantSecret = Cod.Platform.SignatureHelper.GetTenantSecret(tenant, secret);
            var signature = Cod.SignatureHelper.GetSignature(stringToSign, tenantSecret);
            return signature.ToUpperInvariant() != requestSignature.ToUpperInvariant()
                ? new OperationResult<T>(InternalError.AuthenticationRequired)
                : new OperationResult<T>(model);
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
                throw new InvalidDataException($"Token failed validation: {stvex.Message}");
            }
            catch (ArgumentException argex)
            {
                throw new ArgumentException($"Token was invalid: {argex.Message}");
            }
        }

        private static T Parse<T>(string body) => JsonSerializer.DeserializeObject<T>(body);
    }
}
