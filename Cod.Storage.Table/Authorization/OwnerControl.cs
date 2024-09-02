using Cod.Platform;
using System.Security.Claims;

namespace Cod.Storage.Table.Authorization
{
    public abstract class OwnerControl<T> : IStorageControl
    {
        public bool Grantable(StorageType type, string resource)
        {
            return type == StorageType.Table && resource.ToLowerInvariant() == typeof(T).Name.ToLowerInvariant();
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
            return principal.GetClaim<string>(ClaimTypes.NameIdentifier);
        }

        protected virtual string BuildEndPartitionKey(ClaimsPrincipal principal)
        {
            return principal.GetClaim<string>(ClaimTypes.NameIdentifier);
        }
    }
}
