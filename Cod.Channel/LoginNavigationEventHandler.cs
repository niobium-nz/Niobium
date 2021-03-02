using System;
using System.Threading.Tasks;

namespace Cod.Channel
{
    public class LoginNavigationEventHandler : DomainEventHandler<IAuthenticator>
    {
        private readonly INavigator navigator;

        public LoginNavigationEventHandler(INavigator navigator) => this.navigator = navigator;

        protected async override Task CoreHandleAsync(IAuthenticator sender, object e)
        {
            if (!sender.IsAuthenticated())
            {
                var queries = this.navigator.GetQueryStrings();
                var returnUrl = queries.Get("returnUrl");
                if (String.IsNullOrWhiteSpace(returnUrl))
                {
                    returnUrl = this.navigator.CurrentUri;
                }

                await this.navigator.NavigateToAsync($"/login?returnUrl={returnUrl}");
            }
        }
    }
}
