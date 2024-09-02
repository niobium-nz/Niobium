using System;
using System.Threading.Tasks;

namespace Cod.Channel
{
    public class LoginNavigationEventHandler : IEventHandler<IAuthenticator>
    {
        private readonly INavigator navigator;

        public LoginNavigationEventHandler(INavigator navigator) => this.navigator = navigator;

        public async Task HandleAsync(object sender, object e)
        {
            if (e is AuthenticationUpdatedEvent evt && !evt.IsAuthenticated)
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
