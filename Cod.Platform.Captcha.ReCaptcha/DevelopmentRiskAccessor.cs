using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cod.Platform.Captcha.ReCaptcha
{
    internal class DevelopmentRiskAccessor(HttpClient httpClient, IOptions<CaptchaOptions> options, ILogger<GoogleReCaptchaRiskAssessor> logger)
        : GoogleReCaptchaRiskAssessor(httpClient, options, logger)
    {
        public override Task<bool> AssessAsync(string requestID, string tenant, string token, string? remoteIP, CancellationToken cancellationToken) => Task.FromResult(true);
    }
}
