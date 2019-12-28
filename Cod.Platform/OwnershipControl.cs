using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;

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
            var ownership = await CloudStorage.GetTable<TOwnership>().RetrieveAsync<TOwnership>(owner, partition);
            return ownership != null;
        }

        protected virtual string GetOwnerID(ClaimsPrincipal principal) => principal.GetClaim<string>(ClaimTypes.NameIdentifier);
    }
}
