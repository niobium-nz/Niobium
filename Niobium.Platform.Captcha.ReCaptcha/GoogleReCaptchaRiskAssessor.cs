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
        private const string wwwPrefix = "www.";

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

            if (!options.Value.Secrets.TryGetValue(hostname, out string? secret))
            {
                if (!hostname.StartsWith(wwwPrefix, StringComparison.OrdinalIgnoreCase) || !options.Value.Secrets.TryGetValue(hostname[4..], out secret))
                {
                    string escapeHostname = hostname.Replace(".", "_").ToUpperInvariant();
                    if (!options.Value.Secrets.TryGetValue(escapeHostname, out secret))
                    {
                        if (!hostname.StartsWith(wwwPrefix, StringComparison.OrdinalIgnoreCase) || !options.Value.Secrets.TryGetValue(escapeHostname[4..], out secret))
                        {
                            // Last chance: try to match a configured wildcard secret (e.g. "*.abc.com")
                            string baseHostname = hostname.StartsWith(wwwPrefix, StringComparison.OrdinalIgnoreCase) ? hostname[4..] : hostname;
                            string escapeBaseHostname = baseHostname.Replace('.', '_').ToUpperInvariant();
                            foreach (KeyValuePair<string, string> kvp in options.Value.Secrets)
                            {
                                string key = kvp.Key ?? String.Empty;
                                if (key.StartsWith("*.", StringComparison.Ordinal))
                                {
                                    // wildcard suffix includes the leading dot, e.g. ".abc.com"
                                    string suffix = key[1..]; // ".abc.com"
                                    if (baseHostname.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                                    {
                                        secret = kvp.Value;
                                        break;
                                    }
                                    // also try matching against escaped form (underscore + upper)
                                    string escapedSuffix = suffix.Replace('.', '_').ToUpperInvariant();
                                    if (escapeBaseHostname.EndsWith(escapedSuffix, StringComparison.OrdinalIgnoreCase))
                                    {
                                        secret = kvp.Value;
                                        break;
                                    }
                                }
                            }
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
                    string baseDomain1 = hostname.StartsWith(wwwPrefix, StringComparison.OrdinalIgnoreCase) ? hostname[4..] : hostname;
                    string baseDomain2 = result.Hostname.StartsWith(wwwPrefix, StringComparison.OrdinalIgnoreCase) ? result.Hostname[4..] : result.Hostname;
                    lowrisk = baseDomain1.Equals(baseDomain2, StringComparison.OrdinalIgnoreCase);
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
