using System.Security.Claims;

namespace Cod.Platform
{
    public abstract class OwnerControl<T> : IStorageControl where T : IEntity
    {
        public bool Grantable(StorageType type, string resource) =>
            type == StorageType.Table && resource.ToLowerInvariant() == typeof(T).Name.ToLowerInvariant();

        public Task<StorageControl> GrantAsync(ClaimsPrincipal principal, StorageType type, string resource, string partition, string row, CancellationToken cancellationToken = default)
          => Task.FromResult(new StorageControl((int)TablePermissions.Query, typeof(T).Name)
          {
              StartPartitionKey = this.BuildStartPartitionKey(principal),
              EndPartitionKey = this.BuildEndPartitionKey(principal)
          });

        protected virtual string BuildStartPartitionKey(ClaimsPrincipal principal) => principal.GetClaim<string>(ClaimTypes.NameIdentifier);

        protected virtual string BuildEndPartitionKey(ClaimsPrincipal principal) => principal.GetClaim<string>(ClaimTypes.NameIdentifier);
    }
}
