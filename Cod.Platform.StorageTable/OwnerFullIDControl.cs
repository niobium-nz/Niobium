using System.Security.Claims;

namespace Cod.Platform.StorageTable
{
    public abstract class OwnerFullIDControl<T> : IResourceControl
    {
        public bool Grantable(ResourceType type, string resource)
        {
            return type == ResourceType.AzureStorageTable && resource.Equals(typeof(T).Name, StringComparison.InvariantCultureIgnoreCase);
        }

        public Task<StorageControl> GrantAsync(ClaimsPrincipal principal, ResourceType type, string resource, string partition, string row, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new StorageControl((int)DatabasePermissions.Query, typeof(T).Name)
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
