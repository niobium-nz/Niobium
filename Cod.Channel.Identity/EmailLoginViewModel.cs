using Cod.Identity;

namespace Cod.Channel.Identity
{
    public class EmailLoginViewModel(
        ILoadingStateService loadingStateService,
        ICommand<TOTPLoginCommandParameter, LoginResult> loginCommand)
        : TOTPLoginViewModel(loadingStateService, loginCommand)
    {
        public override Task OnLogin()
        {
            if (!RegexUtilities.IsValidEmail(UserInput.Username))
            {
                UserInputValidation.AddError(nameof(UserInput.Username), "Please enter a valid email address.");
                return Task.CompletedTask;
            }

            return base.OnLogin();
        }
    }
}
