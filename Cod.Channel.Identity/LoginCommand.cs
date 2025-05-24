using Cod.Identity;
using System.Net;

namespace Cod.Channel.Identity
{
    internal class LoginCommand(
        IAuthenticator authenticator,
        INavigator navigator,
        ILoadingStateService loadingStateService)
        : ICommand<LoginCommandParameter, LoginResult>
    {
        public async Task<LoginResult> ExecuteAsync(LoginCommandParameter parameter, CancellationToken? cancellationToken)
        {
            cancellationToken ??= CancellationToken.None;
            using (loadingStateService.SetBusy(BusyGroups.Login))
            {
                var result = await authenticator.LoginAsync(
                            parameter.Scheme,
                            parameter.Identity,
                            parameter.Credential,
                            parameter.Remember,
                            cancellationToken);

                if (result.IsSuccess)
                {
                    var returnUrl = parameter.ReturnUrl;
                    if (String.IsNullOrEmpty(returnUrl) && navigator.TryGetQueryString(Constants.LoginReturnUrlQueryStringName, out var r))
                    {
                        returnUrl = WebUtility.UrlDecode(r);
                    }

                    if (!String.IsNullOrEmpty(returnUrl))
                    {
                        await navigator.NavigateToAsync(returnUrl, forceLoad: true);
                    }
                }

                return result;
            }
        }
    }
}
