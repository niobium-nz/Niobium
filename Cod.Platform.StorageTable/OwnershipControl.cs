using System.Security.Claims;

namespace Cod.Platform.StorageTable
{
    public class OwnershipControl<TResource, TOwnership>(Lazy<IRepository<TOwnership>> repo) : IResourceControl
    {
        public bool Grantable(ResourceType type, string resource)
        {
            return type == ResourceType.AzureStorageTable && resource.Equals(typeof(TResource).Name, StringComparison.InvariantCultureIgnoreCase);
        }

        public async Task<StorageControl?> GrantAsync(ClaimsPrincipal principal, ResourceType type, string resource, string? partition, string? row, CancellationToken cancellationToken = default)
        {
            bool grant = await HasPermission(principal, partition, cancellationToken);
            return grant
                ? new StorageControl((int)DatabasePermissions.Query, typeof(TResource).Name)
                {
                    StartPartitionKey = partition,
                    EndPartitionKey = partition
                }
                : null;
        }

        protected virtual async Task<bool> HasPermission(ClaimsPrincipal principal, string? partition, CancellationToken cancellationToken)
        {
            if (partition == null)
            {
                return false;
            }

            string owner = GetOwnerID(principal);
            return await ExistAsync(owner, partition, cancellationToken);
        }

        protected virtual async Task<bool> ExistAsync(string partitionKey, string rowKey, CancellationToken cancellationToken)
        {
            return await repo.Value.ExistsAsync(partitionKey, rowKey, cancellationToken: cancellationToken);
        }

        protected virtual string GetOwnerID(ClaimsPrincipal principal)
        {
            return principal.GetClaim<string>(ClaimTypes.Sid);
        }
    }
}
