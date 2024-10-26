using Cod.Identity;
using System.Text;

namespace Cod.Messaging.ServiceBus
{
    internal class RoleBasedSendEntitlementDescriptor(string roleToGrant, string fullyQualifiedNamespace, string queueName, MessagingPermissions permissions) : IEntitlementDescriptor
    {
        public bool IsHighOverhead => false;

        public bool CanDescribe(Guid tenant, Guid user, string role) => roleToGrant == role;

        public Task<IEnumerable<EntitlementDescription>> DescribeAsync(Guid tenant, Guid user, string role)
        {
            var permissionBuilder = new StringBuilder();
            if (permissions.HasFlag(MessagingPermissions.Read))
            {
                permissionBuilder.Append(MessagingPermissions.Read.ToString().ToUpperInvariant());
                permissionBuilder.Append(',');
            }

            if (permissions.HasFlag(MessagingPermissions.Add))
            {
                permissionBuilder.Append(MessagingPermissions.Add.ToString().ToUpperInvariant());
                permissionBuilder.Append(',');
            }

            if (permissions.HasFlag(MessagingPermissions.ProcessMessages))
            {
                permissionBuilder.Append(MessagingPermissions.ProcessMessages.ToString().ToUpperInvariant());
                permissionBuilder.Append(',');
            }

            if (permissions.HasFlag(MessagingPermissions.Update))
            {
                permissionBuilder.Append(MessagingPermissions.Update.ToString().ToUpperInvariant());
                permissionBuilder.Append(',');
            }

            var permissionDesc = permissionBuilder.ToString();
            if (permissionDesc.Length > 0)
            {
                permissionDesc = permissionDesc[..^1];
            }

            return Task.FromResult<IEnumerable<EntitlementDescription>>(
            [
                new()
                {
                    Permission = permissionDesc,
                    Resource = $"{fullyQualifiedNamespace}/{queueName}",
                    Type = ResourceType.AzureServiceBus,
                }
            ]);
        }
    }
}
