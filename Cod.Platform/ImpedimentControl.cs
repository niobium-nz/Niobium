using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;

namespace Cod.Platform
{
    internal class ImpedimentControl : IStorageControl
    {

        public bool Grantable(StorageType type, string resource) =>
            type == StorageType.Table && resource.ToLowerInvariant() == typeof(Impediment).Name.ToLowerInvariant();

        public Task<StorageControl> GrantAsync(ClaimsPrincipal principal, StorageType type, string resource, string partition, string row)
        {
            var sid = principal.GetClaim<Guid>(ClaimTypes.Sid);
            if (partition.ToUpperInvariant().StartsWith(sid.ToKey()))
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
