using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Cod.Platform
{
    public static class ClaimsPrincipalExtensions
    {
        public static bool PermissionsGrant(this ClaimsPrincipal principal, string entitlement)
        {
            var permissions = principal.GetPermissions();
            return permissions.IsAccessGrant(entitlement);
        }

        public static bool PermissionsGrant(this ClaimsPrincipal principal, string scope, string entitlement)
        {
            var permissions = principal.GetPermissions();
            return permissions.IsAccessGrant(scope, entitlement);
        }

        public static IEnumerable<Permission> GetPermissions(this ClaimsPrincipal principal)
            => principal.Claims
            .Select(c => new KeyValuePair<string, string>(c.Type, c.Value))
            .ToPermissions();

        public static T GetClaim<T>(this ClaimsPrincipal principal, string key)
        {
            if (!TryGetClaim<T>(principal, key, out var result))
            {
                throw new KeyNotFoundException($"The specified claim does not exist: {key}.");
            }
            return result;
        }

        public static bool TryGetClaim<T>(this ClaimsPrincipal principal, string key, out T result)
        {
            var claim = principal.Claims.SingleOrDefault(c => c.Type == key);
            if (claim == null)
            {
                result = default;
                return false;
            }

            result = TypeConverter.Convert<T>(claim.Value);
            return true;
        }
    }
}
