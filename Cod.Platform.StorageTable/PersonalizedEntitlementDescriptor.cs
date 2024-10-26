using Cod.Identity;

namespace Cod.Platform.StorageTable
{
    internal class PersonalizedEntitlementDescriptor(string roleToGrant, DatabasePermissions permissions, string fullyQualifiedDomainName, string tableName)
        : RoleBasedEntitlementDescriptor(roleToGrant, permissions, fullyQualifiedDomainName, tableName)
    {
        protected override IEnumerable<EntitlementDescription> BuildDescription(Guid tenant, Guid user, string role, string tableName, string permissionDescription)
        {
            return [new EntitlementDescription
            {
                Permission = permissionDescription,
                Resource = $"{FullyQualifiedDomainName}/{tableName}/{user}",
                Type = ResourceType.AzureStorageTable,
            }];
        }
    }
}
