using Cod.Platform.Tenant;

namespace Cod.Platform.Identity
{
    public interface IOpenIDManager
    {
        Task RegisterAsync(IEnumerable<OpenIDRegistration> registrations, CancellationToken cancellationToken = default);

        IAsyncEnumerable<OpenID> GetChannelsAsync(Guid user, CancellationToken cancellationToken = default);

        IAsyncEnumerable<OpenID> GetChannelsAsync(Guid user, int kind, CancellationToken cancellationToken = default);
    }
}
