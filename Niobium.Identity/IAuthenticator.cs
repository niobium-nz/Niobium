using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace Niobium.Identity
{
    public interface IAuthenticator
    {
        JsonWebToken? IDToken { get; }

        JsonWebToken? AccessToken { get; }

        Task<bool> GetAuthenticateStatus(CancellationToken cancellationToken = default);

        Task<IEnumerable<Claim>?> GetClaimsAsync(CancellationToken cancellationToken = default);

        Task<LoginResult> LoginAsync(string scheme, string identity, string? credential, bool remember, CancellationToken cancellationToken = default);

        Task LogoutAsync(CancellationToken cancellationToken = default);

        Task<string> RetrieveResourceTokenAsync(ResourceType type, string resource, string? partition = null, string? id = null, CancellationToken cancellationToken = default);
    }
}
