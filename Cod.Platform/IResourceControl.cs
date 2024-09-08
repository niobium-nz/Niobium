using System.Security.Claims;

namespace Cod.Platform
{
    public interface IResourceControl
    {
        bool Grantable(ResourceType type, string resource);

        Task<StorageControl> GrantAsync(ClaimsPrincipal principal, ResourceType type, string resource, string partition, string row, CancellationToken cancellationToken = default);
    }
}
