using Niobium.Identity;

namespace Niobium.Channel.Identity
{
    public class EmailLoginViewModel(
        ILoadingStateService loadingStateService,
        ICommand<TOTPLoginCommandParameter, LoginResult> loginCommand)
        : TOTPLoginViewModel(loadingStateService, loginCommand)
    {
        public override async Task OnLogin()
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
