using Cod.Identity;

namespace Cod.Channel.Identity
{
    public class EmailLoginViewModel(
        ILoadingStateService loadingStateService,
        ICommand<TOTPLoginCommandParameter, LoginResult> loginCommand)
        : TOTPLoginViewModel(loadingStateService, loginCommand)
    {
        public async override Task OnLogin()
        {
            if (!RegexUtilities.IsValidEmail(UserInput.Username))
            {
                UserInputValidation.AddError(nameof(UserInput.Username), "Please enter a valid email address.");
                return;
            }

            await base.OnLogin();
        }
    }
}
