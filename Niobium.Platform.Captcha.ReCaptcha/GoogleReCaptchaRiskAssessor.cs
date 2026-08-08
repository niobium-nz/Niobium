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
            if (String.IsNullOrWhiteSpace(token))
            {
                throw new ApplicationException(Niobium.InternalError.BadRequest, "Missing captcha token in request.");
            }

            if (String.IsNullOrWhiteSpace(hostname))
            {
                hostname = httpContextAccessor.Value.HttpContext?.Request.GetSourceHostname()
                    ?? throw new ApplicationException(Niobium.InternalError.BadRequest, "Cannot retrieve hostname from request.");
            }

            if (String.IsNullOrWhiteSpace(clientIP))
            {
                clientIP = httpContextAccessor.Value.HttpContext?.Request.GetRemoteIP()
                    ?? throw new ApplicationException(Niobium.InternalError.BadRequest, "unable to get client IP from request.");
            }

            hostname = hostname.ToUpperInvariant();
            if (!options.Value.Secrets.TryGetValue(hostname, out string? secret))
            {
                string escapeHostname = hostname.Replace(".", "_");
                if (!options.Value.Secrets.TryGetValue(escapeHostname, out secret))
                {
                    foreach (string key in options.Value.Secrets.Keys)
                    {
                        if (escapeHostname.EndsWith(key, StringComparison.OrdinalIgnoreCase))
                        {
                            secret = options.Value.Secrets[key];
                            break;
                        }
                    }
                }
            }

            if (secret == null)
            {
                throw new ApplicationException(Niobium.InternalError.InternalServerError, $"Missing tenant secret: {hostname}");
            }

            List<KeyValuePair<string, string>> parameters = new([
                new KeyValuePair<string, string>("secret", secret),
                new KeyValuePair<string, string>("response", token),
            ]);
            if (!String.IsNullOrWhiteSpace(clientIP))
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

            bool lowrisk = result.Success;

            if (lowrisk)
            {
                lowrisk = result.Hostname == null || result.Hostname.Equals(hostname, StringComparison.OrdinalIgnoreCase);
                if (!lowrisk && result.Hostname != null && hostname != null)
                {
                    string baseDomain = result.Hostname.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? result.Hostname[4..] : result.Hostname;
                    lowrisk = hostname.EndsWith(baseDomain, StringComparison.OrdinalIgnoreCase);
                }
            }

            if (throwsExceptionWhenFail && !lowrisk)
            {
                logger?.LogWarning($"{clientIP} is considered high risk for request {requestID}: {respbody}");
                throw new UnauthorizedAccessException();
            }

            return lowrisk;
        }
    }
}
