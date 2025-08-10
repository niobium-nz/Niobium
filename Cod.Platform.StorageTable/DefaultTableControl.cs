using Cod.Database.StorageTable;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Cod.Platform.StorageTable
{
    internal class DefaultTableControl(IOptions<StorageTableOptions> options) : IResourceControl
    {
        public bool Grantable(ResourceType type, string resource) => type == ResourceType.AzureStorageTable && options.Value.FullyQualifiedDomainName != null && options.Value.Key != null;

        public Task<StorageControl?> GrantAsync(ClaimsPrincipal principal, ResourceType type, string resource, string? partition, string? row, CancellationToken cancellationToken = default)
        {
            StorageControl? result = null;
            var permissions = principal.Claims.ToResourcePermissions();
            var entitlements = permissions
                .Where(p => p.Type == ResourceType.AzureStorageTable
                            && p.Partition == resource
                            && (p.Scope != null && partition != null && partition.StartsWith(p.Scope) || (p.Scope == null && partition == null)))
                .SelectMany(p => p.Entitlements);

            if (entitlements != null && entitlements.Any())
            {
                DatabasePermissions permisson = DatabasePermissions.None;
                foreach (var entitlement in entitlements)
                {
                    if (Enum.TryParse(entitlement, true, out DatabasePermissions p))
                    {
                        permisson |= p;
                    }
                }

                if (permisson != DatabasePermissions.None)
                {
                    result = new StorageControl((int)permisson, resource)
                    {
                        StartPartitionKey = partition,
                        EndPartitionKey = partition,
                    };
                }
            }

            return Task.FromResult(result);
        }
    }
}
