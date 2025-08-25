namespace Niobium.Platform.Captcha.ReCaptcha
{
    public class CaptchaOptions
    {
        public bool IsDisabled { get; set; }

        public required Dictionary<string, string> Secrets { get; set; }
    }
}
