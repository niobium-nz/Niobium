namespace Niobium.Platform.Captcha.ReCaptcha
{
    public interface IVisitorRiskAssessor
    {
        Task<bool> AssessAsync(string token, string? requestID = null, string? hostname = null, string? clientIP = null, bool throwsExceptionWhenFail = true, CancellationToken cancellationToken = default);
    }
}
