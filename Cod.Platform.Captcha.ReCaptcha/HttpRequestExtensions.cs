using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Cod.Platform.Captcha.ReCaptcha
{
    public static class HttpRequestExtensions
    {
        public async static Task<IActionResult?> AssessRiskAsync(this HttpRequest request, IVisitorRiskAssessor assessor, Guid id, string captcha, ILogger? logger = null, CancellationToken cancellationToken = default)
        {
            var referer = request.Headers.Referer.SingleOrDefault();
            if (referer == null || !Uri.TryCreate(referer, UriKind.Absolute, out Uri? refererUri))
            {
                return new BadRequestResult();
            }

            var tenant = refererUri?.Host.ToLowerInvariant();

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
