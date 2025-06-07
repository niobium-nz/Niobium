namespace Cod.Platform.Captcha.ReCaptcha
{
    internal class DevelopmentRiskAccessor : IVisitorRiskAssessor
    {
        public Task<bool> AssessAsync(Guid requestID, string tenant, string token, string? remoteIP, CancellationToken cancellationToken) => Task.FromResult(true);
    }
}
