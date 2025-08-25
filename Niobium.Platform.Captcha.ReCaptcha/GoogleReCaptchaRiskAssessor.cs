using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Niobium.Platform.Captcha.ReCaptcha
{
    internal partial class GoogleReCaptchaRiskAssessor(
        HttpClient httpClient,
        IOptions<CaptchaOptions> options,
        Lazy<IHttpContextAccessor> httpContextAccessor,
        ILogger<GoogleReCaptchaRiskAssessor> logger)
        : IVisitorRiskAssessor
    {
        private const string recaptchaAPI = "https://www.google.com/recaptcha/api/siteverify";
        private static readonly JsonSerializerOptions GoogleRechptchaSerializationOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        public virtual async Task<bool> AssessAsync(string token, string? requestID = null, string? tenant = null, string? clientIP = null, bool throwsExceptionWhenFail = true, CancellationToken cancellationToken = default)
        {
            requestID ??= Guid.NewGuid().ToString();
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ApplicationException(Niobium.InternalError.BadRequest, "Missing captcha token in request.");
            }

            if (string.IsNullOrWhiteSpace(tenant))
            {
                tenant = httpContextAccessor.Value.HttpContext?.Request.GetTenant()
                    ?? throw new ApplicationException(Niobium.InternalError.BadRequest, "Missing tenant information in request.");
            }

            if (string.IsNullOrWhiteSpace(clientIP))
            {
                clientIP = httpContextAccessor.Value.HttpContext?.Request.GetRemoteIP()
                    ?? throw new ApplicationException(Niobium.InternalError.BadRequest, "unable to get client IP from request.");
            }

            string secret = options.Value.Secrets[tenant]
                ?? throw new ApplicationException(Niobium.InternalError.InternalServerError, $"Missing tenant secret: {tenant}");

            List<KeyValuePair<string, string>> parameters = new([
                new KeyValuePair<string, string>("secret", secret),
                new KeyValuePair<string, string>("response", token),
            ]);
            if (!string.IsNullOrWhiteSpace(clientIP))
            {
                parameters.Add(new KeyValuePair<string, string>("remoteip", clientIP));
            }
            FormUrlEncodedContent payload = new(parameters);

            using HttpResponseMessage response = await httpClient.PostAsync(recaptchaAPI, payload, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError($"Error response {response.StatusCode} from Google ReCaptcha on request {requestID}.");
                return false;
            }

            string respbody = await response.Content.ReadAsStringAsync(cancellationToken);
            GoogleReCaptchaResult result = Deserialize<GoogleReCaptchaResult>(respbody);
            if (result == null)
            {
                logger.LogError($"Error deserializing Google ReCaptcha response: {respbody} on request {requestID}.");
                return false;
            }

            bool lowrisk = result.Success && result.Hostname.Equals(tenant, StringComparison.OrdinalIgnoreCase);
            if (throwsExceptionWhenFail && !lowrisk)
            {
                logger?.LogWarning($"{clientIP} is considered high risk for request {requestID}");
                throw new UnauthorizedAccessException();
            }

            return lowrisk;
        }

        private static T Deserialize<T>(string json)
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(json, GoogleRechptchaSerializationOptions)!;
        }
    }
}
