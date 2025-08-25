using System.Security.Claims;

namespace Niobium.Platform.StorageTable
{
    public abstract class FullControl<T> : IResourceControl
    {
        public bool Grantable(ResourceType type, string resource)
        {
            return type == ResourceType.AzureStorageTable && resource.Equals(typeof(T).Name, StringComparison.InvariantCultureIgnoreCase);
        }

        public Task<StorageControl?> GrantAsync(ClaimsPrincipal principal, ResourceType type, string resource, string? partition, string? row, CancellationToken cancellationToken = default)
        {
            return resource == typeof(T).Name && HasPermission(principal, partition, row)
                ? Task.FromResult<StorageControl?>(new StorageControl((int)DatabasePermissions.Query, typeof(T).Name)
                {
                    StartPartitionKey = "-",
                    EndPartitionKey = "z"
                })
                : Task.FromResult<StorageControl?>(null);
        }

        protected abstract bool HasPermission(ClaimsPrincipal principal, string? partition, string? row);
    }
}
