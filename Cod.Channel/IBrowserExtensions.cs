using System;
using System.Linq;
using System.Threading.Tasks;

namespace Cod.Channel
{
    public static class IBrowserExtensions
    {
        private const string WechatUserAgent = "MICROMESSENGER";
        private const string AndroidUserAgent = "ANDROID";
        private static readonly string[] AppleUserAgents = new[] { "IPHONE", "IPAD", "IPOD" };

        public async static Task<BrowserType> GetBrowserTypeAsync(this IBrowser browser)
        {
            var ua = await browser.GetUserAgentAsync();
            ua = ua.ToUpperInvariant();
            if (String.IsNullOrWhiteSpace(ua))
            {
                return BrowserType.Unknown;
            }

            if (ua.Contains(WechatUserAgent))
            {
                return BrowserType.Wechat;
            }

            if (ua.Contains(AndroidUserAgent))
            {
                return BrowserType.Android;
            }

            if (AppleUserAgents.Any(a => ua.Contains(a)))
            {
                return BrowserType.iOS;
            }

            return BrowserType.Unknown;
        }
    }
}
