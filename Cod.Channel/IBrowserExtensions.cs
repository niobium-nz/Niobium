namespace Cod.Channel
{
    public static class IBrowserExtensions
    {
        private const string WechatUserAgent = "MICROMESSENGER";
        private const string AndroidUserAgent = "ANDROID";
        private static readonly string[] AppleUserAgents = ["IPHONE", "IPAD", "IPOD"];

        public static async Task<BrowserType> GetBrowserTypeAsync(this IBrowser browser)
        {
            string ua = await browser.GetUserAgentAsync();
            ua = ua.ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(ua))
            {
                return BrowserType.Unknown;
            }

            return ua.Contains(WechatUserAgent)
                ? BrowserType.Wechat
                : ua.Contains(AndroidUserAgent)
                ? BrowserType.Android
                : AppleUserAgents.Any(a => ua.Contains(a)) ? BrowserType.iOS : BrowserType.Unknown;
        }
    }
}
