using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Niobium.Database.StorageTable;

namespace Niobium.Platform.StorageTable
{
    internal sealed class DefaultTableControl(IOptions<StorageTableOptions> options, IConfiguration configuration) : IResourceControl
    {
        public bool Grantable(ResourceType type, string resource)
            => type == ResourceType.AzureStorageTable
                && (!String.IsNullOrWhiteSpace(options.Value.FullyQualifiedDomainName) || !String.IsNullOrWhiteSpace(configuration[Database.StorageTable.Constants.DefaultTableServiceUriSetting]))
                && options.Value.Key != null;

        public Task<StorageControl?> GrantAsync(ClaimsPrincipal principal, ResourceType type, string resource, string? partition, string? row, CancellationToken cancellationToken = default)
        {
            StorageControl? result = null;
            IEnumerable<ResourcePermission> permissions = principal.Claims.ToResourcePermissions();
            IEnumerable<string> entitlements = permissions
                .Where(p => p.Type == ResourceType.AzureStorageTable
                            && p.Partition == resource
                            && ((p.Scope != null && partition != null && partition.StartsWith(p.Scope)) || (p.Scope == null && partition == null)))
                .SelectMany(p => p.Entitlements);

            if (entitlements != null && entitlements.Any())
            {
                DatabasePermissions permisson = DatabasePermissions.None;
                foreach (string? entitlement in entitlements)
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
