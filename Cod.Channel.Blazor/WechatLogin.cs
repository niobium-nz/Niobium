using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Cod.Channel
{
    public class WechatLogin : ComponentBase
    {
        private const string WechatAuthorizeUrl = "https://open.weixin.qq.com/connect/oauth2/authorize?appid={APPID}&redirect_uri={REDIRECT}&response_type=code&scope=snsapi_userinfo&state={STATE}";
        private const string WechatQRConnectUrl = "https://open.weixin.qq.com/connect/qrconnect?appid={APPID}&redirect_uri={REDIRECT}&response_type=code&scope=snsapi_userinfo&state={STATE}";

        [Parameter]
        public string AppID { get; set; }

        [Parameter]
        public string Handler { get; set; }

        [Inject]
        protected IBrowser Browser { get; set; }

        [Inject]
        protected INavigator Navigator { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2208:Instantiate argument exceptions correctly", Justification = "BlazorComponentParameter")]
        protected async override Task OnInitializedAsync()
        {
            if (String.IsNullOrWhiteSpace(this.AppID))
            {
                throw new ArgumentNullException(nameof(this.AppID));
            }

            if (String.IsNullOrWhiteSpace(this.Handler))
            {
                throw new ArgumentNullException(nameof(this.Handler));
            }

            if (this.Handler[0] == '/' || this.Handler[0] == '\\')
            {
                throw new NotSupportedException($"The value of {nameof(this.Handler)} cannot start with slash.");
            }

            this.AppID = this.AppID.Trim();
            var queries = this.Navigator.GetQueryStrings();
            var returnUrl = queries.Get("returnUrl");
            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                returnUrl = String.Empty;
            }

            returnUrl = WebUtility.UrlEncode(returnUrl);
            var callbackUrl = WebUtility.UrlEncode($"{this.Navigator.BaseUri}?go={this.Handler}");
            var type = await this.Browser.GetBrowserTypeAsync();
            if (type == BrowserType.Wechat)
            {
                this.Navigator.NavigateTo(WechatAuthorizeUrl
                    .Replace("{APPID}", this.AppID)
                    .Replace("{REDIRECT}", callbackUrl)
                    .Replace("{STATE}", returnUrl));
            }
            else
            {
                this.Navigator.NavigateTo(WechatQRConnectUrl
                    .Replace("{APPID}", this.AppID)
                    .Replace("{REDIRECT}", callbackUrl)
                    .Replace("{STATE}", returnUrl));
            }
        }
    }
}
