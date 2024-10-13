using Cod.Platform;
using Cod.Table;
using System.Security.Claims;

namespace Cod.Table.Authorization
{
    public abstract class OwnerFullIDControl<T> : IResourceControl
    {
        public bool Grantable(ResourceType type, string resource)
        {
            return type == ResourceType.AzureStorageTable && resource.ToUpperInvariant() == typeof(T).Name.ToUpperInvariant();
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
            return principal.GetClaim<Guid>(ClaimTypes.Sid).ToKey();
        }

        protected virtual string BuildEndPartitionKey(ClaimsPrincipal principal)
        {
            return BuildStartPartitionKey(principal);
        }
    }
}
