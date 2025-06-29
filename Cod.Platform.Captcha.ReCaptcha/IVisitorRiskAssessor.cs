namespace Cod.Platform.Captcha.ReCaptcha
{
    public interface IVisitorRiskAssessor
    {
        Task<bool> AssessAsync(string token, string? requestID = null, string? tenant = null, string? clientIP = null, bool throwsExceptionWhenFail = true, CancellationToken cancellationToken = default);
    }
}
