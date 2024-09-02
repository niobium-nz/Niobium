using Cod.Platform;
using System.Security.Claims;

namespace Cod.Storage.Table.Authorization
{
    public abstract class OwnerFullIDControl<T> : IStorageControl
    {
        public bool Grantable(StorageType type, string resource)
        {
            return type == StorageType.Table && resource.ToUpperInvariant() == typeof(T).Name.ToUpperInvariant();
        }

        public Task<StorageControl> GrantAsync(ClaimsPrincipal principal, StorageType type, string resource, string partition, string row, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new StorageControl((int)TablePermissions.Query, typeof(T).Name)
            {
                StartPartitionKey = BuildStartPartitionKey(principal),
                EndPartitionKey = BuildEndPartitionKey(principal)
            });
        }

        protected virtual string BuildStartPartitionKey(ClaimsPrincipal principal)
        {
            return principal.GetClaim<Guid>(ClaimTypes.Sid).ToKey();
        }

        protected virtual string BuildEndPartitionKey(ClaimsPrincipal principal)
        {
            return BuildStartPartitionKey(principal);
        }
    }
}
