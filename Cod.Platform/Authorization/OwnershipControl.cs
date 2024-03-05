using Azure.Data.Tables;
using Cod.Platform.Database;
using System.Security.Claims;

namespace Cod.Platform.Authorization
{
    public abstract class OwnershipControl<TResource, TOwnership> : IStorageControl
        where TResource : IEntity
        where TOwnership : ITableEntity, new()
    {
        private readonly Lazy<IRepository<TOwnership>> repo;

        public OwnershipControl(Lazy<IRepository<TOwnership>> repo)
        {
            this.repo = repo;
        }

        public bool Grantable(StorageType type, string resource)
        {
            return type == StorageType.Table && resource.ToLowerInvariant() == typeof(TResource).Name.ToLowerInvariant();
        }

        public async Task<StorageControl> GrantAsync(ClaimsPrincipal principal, StorageType type, string resource, string partition, string row, CancellationToken cancellationToken = default)
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
