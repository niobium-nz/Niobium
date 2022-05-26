using System.Net;
using Microsoft.AspNetCore.Components;

namespace Cod.Channel.Blazor
{
    public class WechatLoginHandler : ComponentBase
    {
        [Parameter]
        public string AppID { get; set; }

        [Inject]
        protected INavigator Navigator { get; set; }

        [Inject]
        protected ICommander Commander { get; set; }

        [Inject]
        protected IConfigurationProvider Configuration { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2208:Instantiate argument exceptions correctly", Justification = "BlazorComponentParameter")]
        protected override async Task OnInitializedAsync()
        {
            var queries = this.Navigator.GetQueryStrings();
            var code = queries.Get("code");
            if (String.IsNullOrWhiteSpace(code))
            {
                return;
            }

            var loginID = queries.Get("id");
            if (!String.IsNullOrWhiteSpace(loginID))
            {
                var apiUrl = await this.Configuration.GetSettingAsStringAsync(Constants.KEY_API_URL);
                var apiLoginUrl = $"{apiUrl}/v1/wechat/login?id={loginID}&code={code}";
                await this.Navigator.NavigateToAsync(apiLoginUrl);
                return;
            }

            var loginCommand = this.Commander.Get<LoginCommandParameter>(Commands.Login);
            var parameter = new LoginCommandParameter(Authentication.WechatLoginScheme, this.AppID, code, true);
            var result = await loginCommand.ExecuteAsync(parameter);
            if (result.Result.IsSuccess)
            {
                var returnUrl = queries.Get("state");
                if (String.IsNullOrWhiteSpace(returnUrl))
                {
                    returnUrl = this.Navigator.BaseUri;
                }

                returnUrl = WebUtility.UrlDecode(returnUrl);
                var currentHost = new Uri(this.Navigator.BaseUri).Host;
                var targetHost = new Uri(returnUrl).Host;
                if (currentHost.ToUpperInvariant() == targetHost.ToUpperInvariant())
                {
                    await this.Navigator.NavigateToAsync(returnUrl);
                }
            }

            await this.Navigator.NavigateToAsync(this.Navigator.BaseUri);
        }
    }
}
