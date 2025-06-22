using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Cod.Platform.Captcha.ReCaptcha
{
    public static class HttpRequestExtensions
    {
        public async static Task<IActionResult?> AssessRiskAsync(this HttpRequest request, IVisitorRiskAssessor assessor, string id, string captcha, ILogger? logger = null, CancellationToken cancellationToken = default)
        {
            var tenant = request.GetTenant();
            if (string.IsNullOrWhiteSpace(tenant))
            {
                return new BadRequestResult();
            }

            var clientIP = request.GetRemoteIP();
            var lowRisk = await assessor.AssessAsync(id, tenant!, captcha, clientIP, cancellationToken);
            if (!lowRisk)
            {
                logger?.LogWarning($"{clientIP} is considered high risk for request {id}");
                return new ForbidResult();
            }

            return null;
        }
    }
}
