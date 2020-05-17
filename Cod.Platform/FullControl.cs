using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;

namespace Cod.Platform
{
    public abstract class FullControl<T> : IStorageControl where T : IEntity
    {
        public bool Grantable(StorageType type, string resource) =>
            type == StorageType.Table && resource.ToLowerInvariant() == typeof(T).Name.ToLowerInvariant();

        public Task<StorageControl> GrantAsync(ClaimsPrincipal principal, StorageType type, string resource, string partition, string row)
        {
            if (resource == typeof(T).Name && this.HasPermission(principal, partition, row))
            {
                return Task.FromResult(new StorageControl((int)SharedAccessTablePermissions.Query, typeof(T).Name)
                {
                    StartPartitionKey = "-",
                    EndPartitionKey = "z"
                });
            }
            return Task.FromResult(default(StorageControl));
        }

        protected abstract bool HasPermission(ClaimsPrincipal principal, string partition, string row);
    }
}
