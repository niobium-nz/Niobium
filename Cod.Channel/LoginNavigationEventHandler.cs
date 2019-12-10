using System.Threading.Tasks;
using Cod.Contract;

namespace Cod.Channel
{
    internal class LoginNavigationEventHandler : DomainEventHandler<IAuthenticator>
    {
        private readonly INavigator navigator;

        public LoginNavigationEventHandler(INavigator navigator) => this.navigator = navigator;

        protected override Task CoreHandleAsync(IAuthenticator sender, object e)
        {
            if (!sender.IsAuthenticated())
            {
                this.navigator.NavigateTo($"/login?returnUrl={this.navigator.CurrentUri}");
            }
            return Task.CompletedTask;
        }
    }
}
