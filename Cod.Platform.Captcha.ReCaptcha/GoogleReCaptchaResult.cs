namespace Cod.Platform.Captcha.Recaptcha
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
