using System.Security.Claims;

namespace Niobium.Platform.StorageTable
{
    public abstract class OwnerControl<T> : IResourceControl
    {
        public bool Grantable(ResourceType type, string resource)
        {
            return type == ResourceType.AzureStorageTable && resource.Equals(typeof(T).Name, StringComparison.InvariantCultureIgnoreCase);
        }

        public Task<StorageControl?> GrantAsync(ClaimsPrincipal principal, ResourceType type, string resource, string? partition, string? row, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<StorageControl?>(new StorageControl((int)DatabasePermissions.Query, typeof(T).Name)
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
