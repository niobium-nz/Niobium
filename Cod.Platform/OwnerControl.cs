using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;

namespace Cod.Platform
{
    public abstract class OwnerControl<T> : IStorageControl where T : ITableEntity
    {
        public bool Grantable(StorageType type, string resource) =>
            type == StorageType.Table && resource.ToLowerInvariant() == typeof(T).Name.ToLowerInvariant();

        public Task<StorageControl> GrantAsync(ClaimsPrincipal principal, StorageType type, string resource, string partition, string row)
          => Task.FromResult(new StorageControl((int)SharedAccessTablePermissions.Query, typeof(T).Name)
          {
              StartPartitionKey = this.BuildStartPartitionKey(principal),
              EndPartitionKey = this.BuildEndPartitionKey(principal)
          });

        protected virtual string BuildStartPartitionKey(ClaimsPrincipal principal) => principal.GetClaim<string>(ClaimTypes.NameIdentifier);

        protected virtual string BuildEndPartitionKey(ClaimsPrincipal principal) => principal.GetClaim<string>(ClaimTypes.NameIdentifier);
    }
}
