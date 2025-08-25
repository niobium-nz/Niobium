using Microsoft.Extensions.Options;
using Niobium.Platform;
using System.Security.Claims;

namespace Niobium.Messaging.ServiceBus
{
    internal sealed class DefaultServiceBusQueueControl(IOptions<ServiceBusOptions> options) : IResourceControl
    {
        public bool Grantable(ResourceType type, string resource)
        {
            return type == ResourceType.AzureServiceBus && resource == options.Value.FullyQualifiedNamespace;
        }

        public Task<StorageControl?> GrantAsync(ClaimsPrincipal principal, ResourceType type, string resource, string? partition, string? row, CancellationToken cancellationToken = default)
        {
            StorageControl? result = null;
            IEnumerable<ResourcePermission> permissions = principal.Claims.ToResourcePermissions();
            IEnumerable<string> entitlements = permissions
                .Where(p => p.Type == ResourceType.AzureServiceBus
                    && p.Resource == resource
                    && (partition == p.Partition || (partition != null && p.Partition != null && partition.StartsWith(p.Partition))))
                .SelectMany(p => p.Entitlements);

            if (entitlements != null && entitlements.Any())
            {
                MessagingPermissions permisson = MessagingPermissions.None;
                foreach (string? entitlement in entitlements)
                {
                    if (Enum.TryParse(entitlement, true, out MessagingPermissions p))
                    {
                        permisson |= p;
                    }
                }

                if (permisson != MessagingPermissions.None)
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
