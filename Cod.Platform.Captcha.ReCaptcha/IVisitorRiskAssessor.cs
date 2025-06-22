namespace Cod.Platform.Captcha.ReCaptcha
{
    public interface IVisitorRiskAssessor
    {
        Task<bool> AssessAsync(string requestID, string tenant, string token, string? remoteIP, CancellationToken cancellationToken);
    }
}
