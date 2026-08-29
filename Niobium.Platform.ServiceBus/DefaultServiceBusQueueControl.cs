using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Niobium.Messaging;
using Niobium.Messaging.ServiceBus;

namespace Niobium.Platform.ServiceBus
{
    internal sealed class DefaultServiceBusQueueControl(IOptions<ServiceBusOptions> options, IConfiguration configuration) : IResourceControl
    {
        public bool Grantable(ResourceType type, string resource)
        {
            string? fdqn = options.Value.FullyQualifiedNamespace
                ?? configuration[Messaging.ServiceBus.Constants.DefaultServiceBusFQDNSetting];
            return type == ResourceType.AzureServiceBus && resource == fdqn;
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
