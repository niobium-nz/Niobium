using Cod.Platform.Database;
using System.Security.Claims;

namespace Cod.Platform.Authorization
{
    public abstract class FullControl<T> : IStorageControl where T : IEntity
    {
        public bool Grantable(StorageType type, string resource)
        {
            return type == StorageType.Table && resource.ToLowerInvariant() == typeof(T).Name.ToLowerInvariant();
        }

        public Task<StorageControl> GrantAsync(ClaimsPrincipal principal, StorageType type, string resource, string partition, string row, CancellationToken cancellationToken = default)
        {
            return resource == typeof(T).Name && HasPermission(principal, partition, row)
                ? Task.FromResult(new StorageControl((int)TablePermissions.Query, typeof(T).Name)
                {
                    StartPartitionKey = "-",
                    EndPartitionKey = "z"
                })
                : Task.FromResult(default(StorageControl));
        }

        protected abstract bool HasPermission(ClaimsPrincipal principal, string partition, string row);
    }
}
