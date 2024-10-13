using Cod.Identity;

namespace Cod.Channel.Identity
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

        public virtual async Task OnLogin()
        {
            if (!UserInput.TryValidate(out var r))
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
                    var result = await loginCommand.ExecuteAsync(new TOTPLoginCommandParameter(UserInput.Username!));
                    this.IsChallenged = !result.IsSuccess && result.Challenge != null;
                    this.IsFailed = !this.IsChallenged;
                }
            }
            else
            {
                var result = await loginCommand.ExecuteAsync(new TOTPLoginCommandParameter(UserInput.Username!, UserInput.Password, true));
                this.IsChallenged = !result.IsSuccess && result.Challenge != null;
                this.IsFailed = !(!this.IsChallenged && result.IsSuccess);
            }
        }

        public virtual Task OnCancel()
        {
            this.IsChallenged = false;
            this.IsFailed = false;
            this.UserInput.Password = null;
            return Task.CompletedTask;
        }
    }
}
