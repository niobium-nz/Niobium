using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

        public virtual async Task<bool> AssessAsync(
            string token,
            string? requestID = null,
            string? hostname = null,
            string? clientIP = null,
            bool throwsExceptionWhenFail = true,
            CancellationToken cancellationToken = default)
        {
            requestID ??= Guid.NewGuid().ToString();
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ApplicationException(Niobium.InternalError.BadRequest, "Missing captcha token in request.");
            }

            if (string.IsNullOrWhiteSpace(hostname))
            {
                hostname = httpContextAccessor.Value.HttpContext?.Request.GetSourceHostname()
                    ?? throw new ApplicationException(Niobium.InternalError.BadRequest, "Cannot retrieve hostname from request.");
            }

            if (string.IsNullOrWhiteSpace(clientIP))
            {
                clientIP = httpContextAccessor.Value.HttpContext?.Request.GetRemoteIP()
                    ?? throw new ApplicationException(Niobium.InternalError.BadRequest, "unable to get client IP from request.");
            }

            if (!options.Value.Secrets.TryGetValue(hostname, out string? secret))
            {
                throw new ApplicationException(Niobium.InternalError.InternalServerError, $"Missing tenant secret: {hostname}");
            }

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
            GoogleReCaptchaResult result = JsonMarshaller.Unmarshall<GoogleReCaptchaResult>(respbody, JsonMarshallingFormat.SnakeCase);
            if (result == null)
            {
                logger.LogError($"Error deserializing Google ReCaptcha response: {respbody} on request {requestID}.");
                return false;
            }

            bool lowrisk = result.Success && result.Hostname.Equals(hostname, StringComparison.OrdinalIgnoreCase);
            if (throwsExceptionWhenFail && !lowrisk)
            {
                logger?.LogWarning($"{clientIP} is considered high risk for request {requestID}");
                throw new UnauthorizedAccessException();
            }

            return lowrisk;
        }
    }
}
