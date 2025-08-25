using Niobium.Identity;
using System.Net;

namespace Niobium.Channel.Identity
{
    internal class LoginCommand(
        IAuthenticator authenticator,
        INavigator navigator,
        ILoadingStateService loadingStateService)
        : ICommand<LoginCommandParameter, LoginResult>
    {
        public async Task<LoginResult> ExecuteAsync(LoginCommandParameter parameter, CancellationToken cancellationToken)
        {
            using (loadingStateService.SetBusy(BusyGroups.Login))
            {
                LoginResult result = await authenticator.LoginAsync(
                            parameter.Scheme,
                            parameter.Identity,
                            parameter.Credential,
                            parameter.Remember,
                            cancellationToken);

                if (result.IsSuccess)
                {
                    string? returnUrl = parameter.ReturnUrl;
                    if (string.IsNullOrEmpty(returnUrl) && navigator.TryGetQueryString(Constants.LoginReturnUrlQueryStringName, out string? r))
                    {
                        returnUrl = WebUtility.UrlDecode(r);
                    }

                    if (!string.IsNullOrEmpty(returnUrl))
                    {
                        await navigator.NavigateToAsync(returnUrl, forceLoad: true);
                    }
                }

                return result;
            }
        }
    }
}
