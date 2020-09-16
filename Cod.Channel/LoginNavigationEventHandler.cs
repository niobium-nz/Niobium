using System;
using System.Threading.Tasks;

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
                var queries = this.navigator.GetQueryStrings();
                var returnUrl = queries.Get("returnUrl");
                if (String.IsNullOrWhiteSpace(returnUrl))
                {
                    returnUrl = this.navigator.CurrentUri;
                }
                this.navigator.NavigateTo($"/login?returnUrl={returnUrl}");
            }
            return Task.CompletedTask;
        }
    }
}
