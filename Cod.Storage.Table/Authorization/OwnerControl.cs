using Cod.Platform;
using System.Security.Claims;

namespace Cod.Storage.Table.Authorization
{
    public abstract class OwnerControl<T> : IResourceControl
    {
        public bool Grantable(ResourceType type, string resource)
        {
            return type == ResourceType.AzureStorageTable && resource.ToLowerInvariant() == typeof(T).Name.ToLowerInvariant();
        }

        public Task<StorageControl> GrantAsync(ClaimsPrincipal principal, ResourceType type, string resource, string partition, string row, CancellationToken cancellationToken = default)
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
