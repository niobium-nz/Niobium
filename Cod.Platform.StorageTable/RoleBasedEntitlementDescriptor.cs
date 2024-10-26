using Cod.Identity;
using System.Text;

namespace Cod.Platform.StorageTable
{
    internal class RoleBasedEntitlementDescriptor(string roleToGrant, DatabasePermissions permissions, string fullyQualifiedDomainName, string tableName) : IEntitlementDescriptor
    {
        protected string FullyQualifiedDomainName { get; private set; } = fullyQualifiedDomainName;

        public bool IsHighOverhead => false;

        public bool CanDescribe(Guid tenant, Guid user, string role) => roleToGrant == role;

        public Task<IEnumerable<EntitlementDescription>> DescribeAsync(Guid tenant, Guid user, string role)
        {
            var permissionBuilder = new StringBuilder();
            if (permissions.HasFlag(DatabasePermissions.Query))
            {
                permissionBuilder.Append(DatabasePermissions.Query.ToString().ToUpperInvariant());
                permissionBuilder.Append(',');
            }

            if (permissions.HasFlag(DatabasePermissions.Add))
            {
                permissionBuilder.Append(DatabasePermissions.Add.ToString().ToUpperInvariant());
                permissionBuilder.Append(',');
            }

            if (permissions.HasFlag(DatabasePermissions.Update))
            {
                permissionBuilder.Append(DatabasePermissions.Update.ToString().ToUpperInvariant());
                permissionBuilder.Append(',');
            }

            if (permissions.HasFlag(DatabasePermissions.Delete))
            {
                permissionBuilder.Append(DatabasePermissions.Delete.ToString().ToUpperInvariant());
                permissionBuilder.Append(',');
            }

            var permissionDesc = permissionBuilder.ToString();
            if (permissionDesc.Length > 0)
            {
                permissionDesc = permissionDesc[..^1];
            }

            var result = BuildDescription(tenant, user, role, tableName, permissionDesc);
            return Task.FromResult(result);
        }

        protected virtual IEnumerable<EntitlementDescription> BuildDescription(
            Guid tenant,
            Guid user,
            string role,
            string tableName,
            string permissionDescription)
        {
            return [new EntitlementDescription
            {
                Permission = permissionDescription,
                Resource = $"{FullyQualifiedDomainName}/{tableName}/",
                Type = ResourceType.AzureStorageTable,
            }];
        }
    }
}
