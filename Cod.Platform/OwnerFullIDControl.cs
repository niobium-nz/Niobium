using System.Security.Claims;

namespace Cod.Platform
{
    public abstract class OwnerFullIDControl<T> : IStorageControl where T : IEntity
    {
        public bool Grantable(StorageType type, string resource) =>
            type == StorageType.Table && resource.ToUpperInvariant() == typeof(T).Name.ToUpperInvariant();

        public Task<StorageControl> GrantAsync(ClaimsPrincipal principal, StorageType type, string resource, string partition, string row, CancellationToken cancellationToken = default)
          => Task.FromResult(new StorageControl((int)TablePermissions.Query, typeof(T).Name)
          {
              StartPartitionKey = this.BuildStartPartitionKey(principal),
              EndPartitionKey = this.BuildEndPartitionKey(principal)
          });

        protected virtual string BuildStartPartitionKey(ClaimsPrincipal principal)
            => principal.GetClaim<Guid>(ClaimTypes.Sid).ToKey();

        protected virtual string BuildEndPartitionKey(ClaimsPrincipal principal) => this.BuildStartPartitionKey(principal);
    }
}
