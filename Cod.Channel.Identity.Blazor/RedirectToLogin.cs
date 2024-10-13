using Cod.Identity;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

namespace Cod.Channel.Identity.Blazor
{
    public class RedirectToLogin : ComponentBase
    {
        [Inject]
        public required IOptions<IdentityServiceOptions> Options { get; set; }

        [Inject]
        public required INavigator Navigator { get; set; }

        [Inject]
        public required IAuthenticator Authenticator { get; set; }

        protected override async Task OnInitializedAsync()
        {
            var isAuthenticated = await Authenticator.GetAuthenticateStatus();
            if (!isAuthenticated)
            {
                var queries = Navigator.GetQueryStrings();
                var returnUrl = queries.Get(Constants.LoginReturnUrlQueryStringName);
                if (string.IsNullOrWhiteSpace(returnUrl))
                {
                    returnUrl = Navigator.CurrentUri;
                }

                await Navigator.NavigateToAsync($"{Options.Value.LoginUri}?{Constants.LoginReturnUrlQueryStringName}={returnUrl}");
            }
        }
    }
}
