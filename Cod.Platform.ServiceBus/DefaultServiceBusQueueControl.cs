using Cod.Platform;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Cod.Messaging.ServiceBus
{
    internal class DefaultServiceBusQueueControl(IOptions<ServiceBusOptions> options) : IResourceControl
    {
        public bool Grantable(ResourceType type, string resource) => type == ResourceType.AzureServiceBus && resource == options.Value.FullyQualifiedNamespace;

        public Task<StorageControl?> GrantAsync(ClaimsPrincipal principal, ResourceType type, string resource, string? partition, string? row, CancellationToken cancellationToken = default)
        {
            StorageControl? result = null;
            var permissions = principal.Claims.ToResourcePermissions();
            var entitlements = permissions
                .Where(p => p.Type == ResourceType.AzureServiceBus
                    && p.Resource == resource
                    && (partition == p.Partition || (partition != null && p.Partition != null && partition.StartsWith(p.Partition))))
                .SelectMany(p => p.Entitlements);

            if (entitlements != null && entitlements.Any())
            {
                MessagingPermissions permisson = MessagingPermissions.None;
                foreach (var entitlement in entitlements)
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
