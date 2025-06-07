namespace Cod.Platform.Captcha.ReCaptcha
{
    internal class GoogleReCaptchaResult
    {
        public required bool Success { get; set; }

        public DateTimeOffset ChallengeTs { get; set; }

        public required string Hostname { get; set; }

        public required double Score { get; set; }

        public required string Action { get; set; }
    }
}
