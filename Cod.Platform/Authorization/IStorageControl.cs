using System.Security.Claims;

namespace Cod.Platform.Authorization
{
    public interface IStorageControl
    {
        bool Grantable(StorageType type, string resource);

        Task<StorageControl> GrantAsync(ClaimsPrincipal principal, StorageType type, string resource, string partition, string row, CancellationToken cancellationToken = default);
    }
}
