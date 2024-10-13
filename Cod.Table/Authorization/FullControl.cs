using Cod.Platform;
using Cod.Table;
using System.Security.Claims;

namespace Cod.Table.Authorization
{
    public abstract class FullControl<T> : IResourceControl
    {
        public bool Grantable(ResourceType type, string resource)
        {
            return type == ResourceType.AzureStorageTable && resource.ToLowerInvariant() == typeof(T).Name.ToLowerInvariant();
        }

        public Task<StorageControl> GrantAsync(ClaimsPrincipal principal, ResourceType type, string resource, string partition, string row, CancellationToken cancellationToken = default)
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
