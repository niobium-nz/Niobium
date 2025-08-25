using Niobium.Identity;

namespace Niobium.Channel.Identity
{
    public class TOTPLoginViewModel(
        ILoadingStateService loadingStateService,
        ICommand<TOTPLoginCommandParameter, LoginResult> loginCommand)
    {
        public bool IsBusy => loadingStateService.IsBusy(BusyGroups.Login);

        public bool IsChallenged { get; protected set; }

        public bool IsFailed { get; protected set; }

        public TOTPLoginInput UserInput { get; } = new();

        public ValidationState UserInputValidation { get; protected set; } = new();

        public void Reset()
        {
            IsChallenged = false;
            IsFailed = false;
            UserInputValidation.Clear();
        }

        public virtual async Task OnLogin()
        {
            if (!UserInput.TryValidate(out ValidationState? r))
            {
                UserInputValidation = r;
                return;
            }

            if (string.IsNullOrWhiteSpace(UserInput.Password))
            {
                if (IsChallenged)
                {
                    UserInputValidation.AddError(nameof(UserInput.Password), "Password cannot be empty");
                }
                else
                {
                    UserInputValidation.Clear();
                    LoginResult result = await loginCommand.ExecuteAsync(new TOTPLoginCommandParameter(UserInput.Username!));
                    IsChallenged = !result.IsSuccess && result.Challenge != null;
                    IsFailed = !IsChallenged;
                }
            }
            else
            {
                LoginResult result = await loginCommand.ExecuteAsync(new TOTPLoginCommandParameter(UserInput.Username!, UserInput.Password, true));
                IsChallenged = !result.IsSuccess;
                IsFailed = !(!IsChallenged && result.IsSuccess);
            }
        }

        public virtual Task OnCancel()
        {
            IsChallenged = false;
            IsFailed = false;
            UserInput.Password = null;
            return Task.CompletedTask;
        }
    }
}
