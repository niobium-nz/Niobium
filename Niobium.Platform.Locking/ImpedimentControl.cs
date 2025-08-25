using System.Security.Claims;

namespace Niobium.Platform.Locking
{
    internal sealed class ImpedimentControl : IResourceControl
    {
        public bool Grantable(ResourceType type, string resource)
        {
            return type == ResourceType.AzureStorageTable && resource.Equals(typeof(Impediment).Name, StringComparison.OrdinalIgnoreCase);
        }

        public Task<StorageControl?> GrantAsync(ClaimsPrincipal principal, ResourceType type, string resource, string? partition, string? row, CancellationToken cancellationToken = default)
        {
            Guid sid = principal.GetClaim<Guid>(ClaimTypes.Sid);
            return partition != null && partition.StartsWith(sid.ToKey(), StringComparison.InvariantCultureIgnoreCase)
                ? Task.FromResult<StorageControl?>(new StorageControl((int)DatabasePermissions.Query, typeof(Impediment).Name)
                {
                    StartPartitionKey = partition,
                    EndPartitionKey = partition
                })
                : Task.FromResult<StorageControl?>(null);
        }
    }
}
