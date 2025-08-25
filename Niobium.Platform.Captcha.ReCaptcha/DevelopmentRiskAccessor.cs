using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Niobium.Platform.Captcha.ReCaptcha
{
    internal sealed class DevelopmentRiskAccessor(HttpClient httpClient, IOptions<CaptchaOptions> options, Lazy<IHttpContextAccessor> httpContextAccessor, ILogger<GoogleReCaptchaRiskAssessor> logger)
                : GoogleReCaptchaRiskAssessor(httpClient, options, httpContextAccessor, logger)
    {
        public override Task<bool> AssessAsync(string token, string? requestID = null, string? tenant = null, string? clientIP = null, bool throwsExceptionWhenFail = true, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }
}
