using Cod.Platform;
using System.Security.Claims;

namespace Cod.Messaging.ServiceBus
{
    internal class ServiceBusQueueControl : IResourceControl
    {
        public bool Grantable(ResourceType type, string resource) => type == ResourceType.AzureServiceBus;

        public Task<StorageControl?> GrantAsync(ClaimsPrincipal principal, ResourceType type, string resource, string partition, string row, CancellationToken cancellationToken = default)
        {
            StorageControl? result = null;
            var entitlements = principal.Claims.ToResourcePermissions()
                .Where(p => p.Type == ResourceType.AzureServiceBus && p.Resource == resource && (p.IsWildcard || p.Scope == partition))
                .SelectMany(p => p.Entitlements);

            if (entitlements != null && entitlements.Any())
            {
                MessagingPermissions permisson = MessagingPermissions.None;
                foreach (var entitlement in entitlements)
                {
                    if (entitlement == Constants.EntitlementMessagingSend)
                    {
                        permisson |= MessagingPermissions.Add;
                    }
                    else
                    {
                        throw new NotImplementedException();
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
