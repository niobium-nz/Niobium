using System.Security.Claims;

namespace Cod
{
    public static class ClaimsPrincipalExtensions
    {
        public static bool PermissionsGrant(this ClaimsPrincipal principal, string entitlement)
        {
            IEnumerable<Permission> permissions = principal.GetPermissions();
            return permissions.IsAccessGrant(entitlement);
        }

        public static bool PermissionsGrant(this ClaimsPrincipal principal, string scope, string entitlement)
        {
            IEnumerable<Permission> permissions = principal.GetPermissions();
            return permissions.IsAccessGrant(scope, entitlement);
        }

        public static IEnumerable<string> QueryScope(this ClaimsPrincipal principal, string entitlement)
        {
            IEnumerable<Permission> permissions = principal.GetPermissions();
            return permissions.Where(c => c.Entitlements.Contains(entitlement)).Select(p => p.Scope).Distinct();
        }

        public static IEnumerable<Permission> GetPermissions(this ClaimsPrincipal principal)
        {
            return principal.Claims.ToPermissions();
        }

        public static T GetClaim<T>(this ClaimsPrincipal principal, string key)
        {
            return !principal.TryGetClaim<T>(key, out T result)
                ? throw new KeyNotFoundException($"The specified claim does not exist: {key}.")
                : result;
        }

        public static bool TryGetClaim<T>(this ClaimsPrincipal principal, string key, out T result)
        {
            Claim claim = principal.Claims.SingleOrDefault(c => c.Type == key);
            if (claim == null)
            {
                result = default;
                return false;
            }

            result = TypeConverter.Convert<T>(claim.Value);
            return true;
        }

        public static bool TryGetClaims<T>(this ClaimsPrincipal principal, string key, out IEnumerable<T> result)
        {
            var claim = principal.Claims.Where(c => c.Type == key);
            if (!claim.Any())
            {
                result = default;
                return false;
            }

            result = claim.Select(c => TypeConverter.Convert<T>(c.Value));
            return true;
        }
    }
}
