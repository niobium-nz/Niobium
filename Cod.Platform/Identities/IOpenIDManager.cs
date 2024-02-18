using Cod.Platform.Authentication;

namespace Cod.Platform.Identities
{
    public interface IOpenIDManager
    {
        Task RegisterAsync(IEnumerable<OpenIDRegistration> registrations, CancellationToken cancellationToken = default);

        IAsyncEnumerable<OpenID> GetChannelsAsync(Guid user, CancellationToken cancellationToken = default);

        IAsyncEnumerable<OpenID> GetChannelsAsync(Guid user, int kind, CancellationToken cancellationToken = default);
    }
}
