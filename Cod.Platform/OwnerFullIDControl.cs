using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;

namespace Cod.Platform
{
    public abstract class OwnerFullIDControl<T> : IStorageControl where T : IEntity
    {
        public bool Grantable(StorageType type, string resource) =>
            type == StorageType.Table && resource.ToLowerInvariant() == typeof(T).Name.ToLowerInvariant();

        public Task<StorageControl> GrantAsync(ClaimsPrincipal principal, StorageType type, string resource, string partition, string row)
          => Task.FromResult(new StorageControl((int)SharedAccessTablePermissions.Query, typeof(T).Name)
          {
              StartPartitionKey = this.BuildStartPartitionKey(principal),
              EndPartitionKey = this.BuildEndPartitionKey(principal)
          });

        protected virtual string BuildStartPartitionKey(ClaimsPrincipal principal)
            => $"{principal.GetClaim<int>(Claims.OPENID_PROVIDER)}|{principal.GetClaim<string>(Claims.OPENID_APP)}|{principal.GetClaim<string>(ClaimTypes.NameIdentifier)}";

        protected virtual string BuildEndPartitionKey(ClaimsPrincipal principal) => this.BuildStartPartitionKey(principal);
    }
}
