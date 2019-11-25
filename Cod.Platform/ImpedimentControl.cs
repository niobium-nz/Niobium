using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace Cod.Platform
{
    internal class ImpedimentControl : IStorageControl
    {

        public bool Grantable(StorageType type, string resource) =>
            type == StorageType.Table && resource.ToLowerInvariant() == typeof(Impediment).Name.ToLowerInvariant();

        public Task<StorageControl> GrantAsync(ClaimsPrincipal principal, StorageType type, string resource, string partition, string row)
        {
            var nameIdentifier = principal.GetClaim<string>(ClaimTypes.NameIdentifier);
            if (partition.ToLowerInvariant().Contains(nameIdentifier.ToLowerInvariant()))
            {
                return Task.FromResult(new StorageControl((int)SharedAccessTablePermissions.Query, typeof(Impediment).Name)
                {
                    StartPartitionKey = partition,
                    EndPartitionKey = partition
                });
            }
            return Task.FromResult<StorageControl>(null);
        }
    }
}
