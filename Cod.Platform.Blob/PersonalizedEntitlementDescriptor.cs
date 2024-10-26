using Cod.File;
using Cod.Identity;

namespace Cod.Platform.Blob
{
    internal class PersonalizedEntitlementDescriptor(string roleToGrant, FilePermissions permissions, string fullyQualifiedDomainName, IEnumerable<string> containerNamePrefix)
        : RoleBasedEntitlementDescriptor(roleToGrant, permissions, fullyQualifiedDomainName, string.Empty)
    {
        protected override IEnumerable<EntitlementDescription> BuildDescription(Guid tenant, Guid user, string role, string container, string permissionDescription)
        {
            return containerNamePrefix.SelectMany(p => base.BuildDescription(tenant, user, role, $"{p}-{user}", permissionDescription));
        }
    }
}
