using System.Security.Claims;

namespace Cod.Platform.Locking
{
    internal class ImpedimentControl : IResourceControl
    {
        public bool Grantable(ResourceType type, string resource)
        {
            return type == ResourceType.AzureStorageTable && resource.ToLowerInvariant() == typeof(Impediment).Name.ToLowerInvariant();
        }

        public Task<StorageControl> GrantAsync(ClaimsPrincipal principal, ResourceType type, string resource, string partition, string row, CancellationToken cancellationToken = default)
        {
            Guid sid = principal.GetClaim<Guid>(ClaimTypes.Sid);
            return partition.StartsWith(sid.ToKey(), StringComparison.InvariantCultureIgnoreCase)
                ? Task.FromResult(new StorageControl((int)DatabasePermissions.Query, typeof(Impediment).Name)
                {
                    StartPartitionKey = partition,
                    EndPartitionKey = partition
                })
                : Task.FromResult<StorageControl>(null);
        }
    }
}
