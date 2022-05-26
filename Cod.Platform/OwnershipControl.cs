using System.Security.Claims;
using Microsoft.WindowsAzure.Storage.Table;

namespace Cod.Platform
{
    public abstract class OwnershipControl<TResource, TOwnership> : IStorageControl
        where TResource : IEntity
        where TOwnership : ITableEntity, new()
    {
        public bool Grantable(StorageType type, string resource) =>
            type == StorageType.Table && resource.ToLowerInvariant() == typeof(TResource).Name.ToLowerInvariant();

        public async Task<StorageControl> GrantAsync(ClaimsPrincipal principal, StorageType type, string resource, string partition, string row)
        {
            var grant = await this.HasPermission(principal, partition);
            if (grant)
            {
                return new StorageControl((int)SharedAccessTablePermissions.Query, typeof(TResource).Name)
                {
                    StartPartitionKey = partition,
                    EndPartitionKey = partition
                };
            }
            return null;
        }

        protected virtual async Task<bool> HasPermission(ClaimsPrincipal principal, string partition)
        {
            var owner = this.GetOwnerID(principal);
            return await this.ExistAsync<TOwnership>(owner, partition);
        }

        protected virtual async Task<bool> ExistAsync<TEntity>(string partitionKey, string rowKey) where TEntity : ITableEntity, new()
        {
            var entity = await CloudStorage.GetTable<TEntity>().RetrieveAsync<TEntity>(partitionKey, rowKey);
            return entity != null;
        }

        protected virtual string GetOwnerID(ClaimsPrincipal principal) => principal.GetClaim<string>(ClaimTypes.NameIdentifier);
    }
}
