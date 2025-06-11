namespace Cod.Platform.Captcha.ReCaptcha
{
    public class CaptchaOptions
    {
        public bool IsEnabled { get; set; }

        public required Dictionary<string, string> Secrets { get; set; }
    }
}
