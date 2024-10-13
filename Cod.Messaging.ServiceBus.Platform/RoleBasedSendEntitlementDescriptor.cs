using Cod.Identity;

namespace Cod.Messaging.ServiceBus
{
    internal class RoleBasedSendEntitlementDescriptor(string roleToGrant, string fullyQualifiedNamespace, string queueName) : IEntitlementDescriptor
    {
        public bool IsHighOverhead => false;

        public bool CanDescribe(Guid tenant, Guid user, string role) => roleToGrant == role;

        public Task<IEnumerable<EntitlementDescription>> DescribeAsync(Guid tenant, Guid user, string role)
        {
            return Task.FromResult<IEnumerable<EntitlementDescription>>(
            [
                new()
                {
                    Permission = $"{queueName}:{Constants.EntitlementMessagingSend}",
                    Resource = fullyQualifiedNamespace,
                    Type = ResourceType.AzureServiceBus,
                }
            ]);
        }
    }
}
