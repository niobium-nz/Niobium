using Cod.Identity;
using Microsoft.Extensions.Options;

namespace Cod.Channel.Identity
{
    internal class TOTPLoginCommand(
        IAuthenticator authenticator,
        INavigator navigator,
        ILoadingStateService loadingStateService,
        IOptions<IdentityServiceOptions> options)
        : LoginCommand(authenticator, navigator, loadingStateService),
            ICommand<TOTPLoginCommandParameter, LoginResult>
    {
        public async Task<LoginResult> ExecuteAsync(TOTPLoginCommandParameter parameter, CancellationToken? cancellationToken = default)
        {
            var identity = IdentityHelper.BuildIdentity(options.Value.App, parameter.Username);
            var p = new LoginCommandParameter(AuthenticationScheme.BasicLoginScheme, identity)
            {
                Credential = parameter.TOTP == null ? null : IdentityHelper.BuildTOTPCredential(parameter.TOTP),
                Remember = parameter.Remember,
                ReturnUrl = parameter.ReturnUrl,
            };
            return await this.ExecuteAsync(p, cancellationToken);
        }
    }
}
