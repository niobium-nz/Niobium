using System.Diagnostics.CodeAnalysis;

namespace Niobium.Platform.Captcha.ReCaptcha
{
    internal sealed class GoogleReCaptchaResult
    {
        [MemberNotNullWhen(true, nameof(Hostname), nameof(Score), nameof(Action))]
        public required bool Success { get; set; }

        public DateTimeOffset ChallengeTs { get; set; }

        public string? Hostname { get; set; }

        public double? Score { get; set; }

        public string? Action { get; set; }
    }
}
