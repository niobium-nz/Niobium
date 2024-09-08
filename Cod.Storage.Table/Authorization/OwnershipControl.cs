using Cod.Platform;
using System.Security.Claims;

namespace Cod.Storage.Table.Authorization
{
    public abstract class OwnershipControl<TResource, TOwnership> : IResourceControl
    {
        private readonly Lazy<IRepository<TOwnership>> repo;

        public OwnershipControl(Lazy<IRepository<TOwnership>> repo)
        {
            this.repo = repo;
        }

        public bool Grantable(ResourceType type, string resource)
        {
            return type == ResourceType.AzureStorageTable && resource.ToLowerInvariant() == typeof(TResource).Name.ToLowerInvariant();
        }

        public async Task<StorageControl> GrantAsync(ClaimsPrincipal principal, ResourceType type, string resource, string partition, string row, CancellationToken cancellationToken = default)
        {
            bool grant = await HasPermission(principal, partition, cancellationToken);
            return grant
                ? new StorageControl((int)TablePermissions.Query, typeof(TResource).Name)
                {
                    StartPartitionKey = partition,
                    EndPartitionKey = partition
                }
                : null;
        }

        protected virtual async Task<bool> HasPermission(ClaimsPrincipal principal, string partition, CancellationToken cancellationToken)
        {
            string owner = GetOwnerID(principal);
            return await ExistAsync(owner, partition, cancellationToken);
        }

        protected virtual async Task<bool> ExistAsync(string partitionKey, string rowKey, CancellationToken cancellationToken)
        {
            TOwnership entity = await repo.Value.RetrieveAsync(partitionKey, rowKey, cancellationToken: cancellationToken);
            return entity != null;
        }

        protected virtual string GetOwnerID(ClaimsPrincipal principal)
        {
            return principal.GetClaim<string>(ClaimTypes.Sid);
        }
    }
}
