using Cod.Identity;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Cod.Channel.Identity.Blazor
{
    internal sealed class AuthenticationStateAdaptor(IAuthenticator authenticator)
        : AuthenticationStateProvider, IDomainEventHandler<IAuthenticator, AuthenticationUpdatedEvent>
    {
        private static readonly ClaimsIdentity EmptyClaim = new();
        private static readonly ClaimsPrincipal Unauthenticated = new(EmptyClaim);

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            IEnumerable<Claim>? claims = await authenticator.GetClaimsAsync();
            if (claims == null)
            {
                return new AuthenticationState(Unauthenticated);
            }

            ClaimsIdentity identity = new(claims, nameof(IAuthenticator));
            ClaimsPrincipal principal = new(identity);
            return new AuthenticationState(principal);
        }

        public async Task HandleAsync(AuthenticationUpdatedEvent e, CancellationToken cancellationToken = default)
        {
            AuthenticationState state = await GetAuthenticationStateAsync();
            NotifyAuthenticationStateChanged(Task.FromResult(state));
        }

        public async Task HandleAsync(object e, CancellationToken cancellationToken = default)
        {
            if (e is AuthenticationUpdatedEvent args)
            {
                await HandleAsync(args, cancellationToken);
            }
        }
    }
}
