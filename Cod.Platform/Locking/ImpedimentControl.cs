using Cod.Platform.Authorization;
using Cod.Platform.Database;
using System.Security.Claims;

namespace Cod.Platform.Locking
{
    internal class ImpedimentControl : IStorageControl
    {
        public bool Grantable(StorageType type, string resource)
        {
            return type == StorageType.Table && resource.ToLowerInvariant() == typeof(Impediment).Name.ToLowerInvariant();
        }

        public Task<StorageControl> GrantAsync(ClaimsPrincipal principal, StorageType type, string resource, string partition, string row, CancellationToken cancellationToken = default)
        {
            Guid sid = principal.GetClaim<Guid>(ClaimTypes.Sid);
            return partition.StartsWith(sid.ToKey(), StringComparison.InvariantCultureIgnoreCase)
                ? Task.FromResult(new StorageControl((int)TablePermissions.Query, typeof(Impediment).Name)
                {
                    StartPartitionKey = partition,
                    EndPartitionKey = partition
                })
                : Task.FromResult<StorageControl>(null);
        }
    }
}
