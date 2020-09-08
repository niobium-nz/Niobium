using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Cod.Channel.Blazor
{
    public class WechatLogin : ComponentBase
    {
        public const string AppIDPlaceholder = "{APPID}";
        private const string WechatAuthorizeUrl = "https://open.weixin.qq.com/connect/oauth2/authorize?appid=" + AppIDPlaceholder + "&redirect_uri={REDIRECT}&response_type=code&scope=snsapi_userinfo&state={STATE}";
        private const int QRCodeCheckMaxRetry = 100;
        private static readonly TimeSpan QRCodeCheckInterval = TimeSpan.FromSeconds(3);
        private string loginID;

        [Parameter]
        public string AppID { get; set; }

        [Parameter]
        public string Handler { get; set; }

        [Parameter]
        public string PostScanHandler { get; set; }

        [Parameter]
        public bool QRCodeTimeout { get; set; }

        [Parameter]
        public string QRCodeElementID { get; set; }

        [Parameter]
        public int QRCodeWidth { get; set; }

        [Parameter]
        public int QRCodeHeight { get; set; }

        [Inject]
        protected IBrowser Browser { get; set; }

        [Inject]
        protected INavigator Navigator { get; set; }

        [Inject]
        protected IJSRuntime JSRuntime { get; set; }

        [Inject]
        protected HttpClient HttpClient { get; set; }

        [Inject]
        protected IConfigurationProvider Configuration { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2208:Instantiate argument exceptions correctly", Justification = "BlazorComponentParameter")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Exception")]
        protected override async Task OnInitializedAsync()
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
            if (String.IsNullOrWhiteSpace(returnUrl))
            {
                returnUrl = String.Empty;
            }

            var baseUrl = WechatAuthorizeUrl.Replace(AppIDPlaceholder, this.AppID);
            var type = await this.Browser.GetBrowserTypeAsync();
            if (type == BrowserType.Wechat)
            {
                var callbackUrl = $"{this.Navigator.BaseUri}?go={this.Handler}";
                this.Navigator.NavigateTo(baseUrl
                    .Replace("{REDIRECT}", WebUtility.UrlEncode(callbackUrl))
                    .Replace("{STATE}", WebUtility.UrlEncode(returnUrl)));
            }
            else
            {
                if (String.IsNullOrWhiteSpace(this.QRCodeElementID))
                {
                    throw new ArgumentNullException(nameof(this.QRCodeElementID));
                }

                await this.PerformQRCodeLogin(returnUrl, baseUrl);
            }
        }

        private async Task PerformQRCodeLogin(string returnUrl, string baseUrl)
        {
            this.QRCodeTimeout = false;
            this.loginID = Guid.NewGuid().ToString("N");

            string callbackUrl;
            if (String.IsNullOrWhiteSpace(this.PostScanHandler))
            {
                callbackUrl = $"{this.Navigator.BaseUri}?go={this.Handler}&id={this.loginID}";
            }
            else
            {
                callbackUrl = $"{this.Navigator.BaseUri}{this.PostScanHandler}?id={this.loginID}";
            }

            var href = baseUrl.Replace("{REDIRECT}", WebUtility.UrlEncode(callbackUrl))
                        .Replace("{STATE}", String.Empty);
            var param = QrCodeHelper.GetQrCodeParameters(
                 this.QRCodeElementID,
                 href,
                 this.QRCodeWidth == 0 ? 250 : this.QRCodeWidth,
                 this.QRCodeHeight == 0 ? 250 : this.QRCodeHeight);

            await this.JSRuntime.InvokeVoidAsync("generateQRCode", param);
            var code = await this.CheckLoginAsync();

            if (code == null)
            {
                this.QRCodeTimeout = true;
            }
            else
            {
                this.Navigator.NavigateTo($"{this.Navigator.BaseUri}{this.Handler}?code={code}&state={WebUtility.UrlEncode(returnUrl)}");
            }
        }

        private async Task<string> CheckLoginAsync()
        {
            var count = 0;
            while (count < QRCodeCheckMaxRetry)
            {
                var resp = await this.HttpClient.RequestAsync<string>(
                    HttpMethod.Get,
                    $"{await this.Configuration.GetSettingAsStringAsync(Constants.KEY_API_URL)}/v1/wechat/login/{this.loginID}");

                if (resp.IsSuccess && !String.IsNullOrWhiteSpace(resp.Result))
                {
                    return resp.Result;
                }

                await Task.Delay(QRCodeCheckInterval);
                count++;
            }

            return null;
        }
    }
}
