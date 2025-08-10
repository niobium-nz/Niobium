using Cod.File;
using Cod.File.Blob;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Cod.Platform.Blob
{
    internal sealed class DefaultBlobControl(IOptions<StorageBlobOptions> options) : IResourceControl
    {
        public bool Grantable(ResourceType type, string resource)
        {
            return type == ResourceType.AzureStorageBlob && options.Value.FullyQualifiedDomainName == resource && options.Value.Key != null;
        }

        public Task<StorageControl?> GrantAsync(ClaimsPrincipal principal, ResourceType type, string resource, string? partition, string? row, CancellationToken cancellationToken = default)
        {
            StorageControl? result = null;
            IEnumerable<string> entitlements = principal.Claims.ToResourcePermissions()
                .Where(p => p.Type == ResourceType.AzureStorageBlob
                    && p.Resource == resource
                    && (partition == p.Partition || (partition != null && p.Partition != null && partition.StartsWith(p.Partition))))
                .SelectMany(p => p.Entitlements);

            if (entitlements != null && entitlements.Any())
            {
                FilePermissions permisson = FilePermissions.None;
                foreach (string? entitlement in entitlements)
                {
                    if (Enum.TryParse(entitlement, true, out FilePermissions p))
                    {
                        permisson |= p;
                    }
                }

                if (permisson != FilePermissions.None)
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
