using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Cod.Channel
{
    public class WechatLoginHandler : ComponentBase
    {
        [Parameter]
        public string AppID { get; set; }

        [Inject]
        protected INavigator Navigator { get; set; }

        [Inject]
        protected ICommander Commander { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2208:Instantiate argument exceptions correctly", Justification = "BlazorComponentParameter")]
        protected async override Task OnInitializedAsync()
        {
            var queries = this.Navigator.GetQueryStrings();
            var code = queries.Get("code");
            if (!String.IsNullOrWhiteSpace(code))
            {
                var loginCommand = this.Commander.Get<LoginCommandParameter>(Commands.Login);
                var parameter = new LoginCommandParameter(Authentication.WechatLoginScheme, this.AppID, code);
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
                        this.Navigator.NavigateTo(returnUrl);
                    }
                }

                this.Navigator.NavigateTo(this.Navigator.BaseUri);
            }
        }
    }
}
