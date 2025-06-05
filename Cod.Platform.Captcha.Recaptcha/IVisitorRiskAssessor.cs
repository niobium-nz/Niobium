namespace Cod.Platform.Captcha.Recaptcha
{
    public interface IVisitorRiskAssessor
    {
        Task<bool> AssessAsync(Guid requestID, string token, string? remoteIP, CancellationToken cancellationToken);
    }
}
