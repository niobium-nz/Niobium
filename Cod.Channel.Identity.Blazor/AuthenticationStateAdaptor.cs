using Cod.Identity;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Cod.Channel.Identity.Blazor
{
    class AuthenticationStateAdaptor(IAuthenticator authenticator)
        : AuthenticationStateProvider, IDomainEventHandler<IAuthenticator, AuthenticationUpdatedEvent>
    {
        private readonly static ClaimsIdentity EmptyClaim = new();
        private readonly static ClaimsPrincipal Unauthenticated = new(EmptyClaim);

        public async override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var claims = await authenticator.GetClaimsAsync();
            if (claims == null)
            {
                return new AuthenticationState(Unauthenticated);
            }

            var identity = new ClaimsIdentity(claims, nameof(IAuthenticator));
            var principal = new ClaimsPrincipal(identity);
            return new AuthenticationState(principal);
        }

        public async Task HandleAsync(AuthenticationUpdatedEvent e, CancellationToken cancellationToken)
        {
            var state = await GetAuthenticationStateAsync();
            NotifyAuthenticationStateChanged(Task.FromResult(state));
        }

        public async Task HandleAsync(object e, CancellationToken cancellationToken)
        {
            if (e is AuthenticationUpdatedEvent args)
            {
                await HandleAsync(args, cancellationToken);
            }
        }
    }
}
