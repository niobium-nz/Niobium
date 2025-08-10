using Cod.File;
using Cod.Identity;
using System.Text;

namespace Cod.Platform.Blob
{
    internal class RoleBasedEntitlementDescriptor(string roleToGrant, FilePermissions permissions, string fullyQualifiedNamespace, string containerName) : IEntitlementDescriptor
    {
        public bool IsHighOverhead => false;

        public bool CanDescribe(Guid tenant, Guid user, string role)
        {
            return roleToGrant == role;
        }

        public Task<IEnumerable<EntitlementDescription>> DescribeAsync(Guid tenant, Guid user, string role)
        {
            StringBuilder permissionBuilder = new();
            if (permissions.HasFlag(FilePermissions.Read))
            {
                permissionBuilder.Append(FilePermissions.Read.ToString().ToUpperInvariant());
                permissionBuilder.Append(',');
            }

            if (permissions.HasFlag(FilePermissions.Add))
            {
                permissionBuilder.Append(FilePermissions.Add.ToString().ToUpperInvariant());
                permissionBuilder.Append(',');
            }

            if (permissions.HasFlag(FilePermissions.Write))
            {
                permissionBuilder.Append(FilePermissions.Write.ToString().ToUpperInvariant());
                permissionBuilder.Append(',');
            }

            if (permissions.HasFlag(FilePermissions.Delete))
            {
                permissionBuilder.Append(FilePermissions.Delete.ToString().ToUpperInvariant());
                permissionBuilder.Append(',');
            }

            if (permissions.HasFlag(FilePermissions.List))
            {
                permissionBuilder.Append(FilePermissions.List.ToString().ToUpperInvariant());
                permissionBuilder.Append(',');
            }

            if (permissions.HasFlag(FilePermissions.Create))
            {
                permissionBuilder.Append(FilePermissions.Create.ToString().ToUpperInvariant());
                permissionBuilder.Append(',');
            }

            string permissionDesc = permissionBuilder.ToString();
            if (permissionDesc.Length > 0)
            {
                permissionDesc = permissionDesc[..^1];
            }

            IEnumerable<EntitlementDescription> result = BuildDescription(tenant, user, role, containerName, permissionDesc);
            return Task.FromResult(result);
        }

        protected virtual IEnumerable<EntitlementDescription> BuildDescription(
            Guid tenant,
            Guid user,
            string role,
            string container,
            string permissionDescription)
        {
            return [new EntitlementDescription
            {
                Permission = permissionDescription,
                Resource = $"{fullyQualifiedNamespace}/{container}",
                Type = ResourceType.AzureStorageBlob,
            }];
        }
    }
}
