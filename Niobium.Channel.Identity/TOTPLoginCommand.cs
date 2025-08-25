using Microsoft.Extensions.Options;
using Niobium.Identity;

namespace Niobium.Channel.Identity
{
    internal sealed class TOTPLoginCommand(
        IAuthenticator authenticator,
        INavigator navigator,
        ILoadingStateService loadingStateService,
        IOptions<IdentityServiceOptions> options)
        : LoginCommand(authenticator, navigator, loadingStateService),
            ICommand<TOTPLoginCommandParameter, LoginResult>
    {
        public async Task<LoginResult> ExecuteAsync(TOTPLoginCommandParameter parameter, CancellationToken cancellationToken = default)
        {
            string identity = IdentityHelper.BuildIdentity(options.Value.App, parameter.Username);
            LoginCommandParameter p = new(AuthenticationScheme.BasicLoginScheme, identity)
            {
                Credential = parameter.TOTP == null ? null : IdentityHelper.BuildTOTPCredential(parameter.TOTP),
                Remember = parameter.Remember,
                ReturnUrl = parameter.ReturnUrl,
            };
            return await ExecuteAsync(p, cancellationToken);
        }
    }
}
