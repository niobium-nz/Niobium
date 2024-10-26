using System.IO;
using System.Linq;
using System.Security.Claims;

namespace Cod
{
    public static class PermissionExtensions
    {
        public static string BuildEntitlement(this Permission permission)
        {
            if (permission is null)
            {
                throw new ArgumentNullException(nameof(permission));
            }

            string wildcard = permission.IsWildcard ? "*" : string.Empty;
            return $"{permission.Scope}{wildcard}{Entitlements.ScopeSplitor}{string.Join(Entitlements.ValueSplitor[0].ToString(), permission.Entitlements)}";
        }

        public static bool TryGetClaim(this IEnumerable<KeyValuePair<string, string>> claims, string key, out string value)
        {
            if (claims.Any(kv => kv.Key == key))
            {
                value = claims.Single(kv => kv.Key == key).Value;
                return true;
            }
            value = null;
            return false;
        }

        public static IEnumerable<string> QueryEntitlements(this IEnumerable<Permission> permissions, string scope)
        {
            if (scope is null)
            {
                throw new ArgumentNullException(nameof(scope));
            }

            if (permissions == null || !permissions.Any())
            {
                return Enumerable.Empty<string>();
            }

            scope = scope.Trim();

            return permissions
                .Where(p => (scope.Length > 0 && p.Scope == scope) || (p.IsWildcard && scope.StartsWith(p.Scope)))
                .SelectMany(p => p.Entitlements);
        }

        public static IEnumerable<string> QueryScope(this IEnumerable<Permission> permissions, string entitlement)
        {
            if (string.IsNullOrWhiteSpace(entitlement))
            {
                throw new ArgumentNullException(nameof(entitlement));
            }

            if (permissions == null || !permissions.Any())
            {
                return Enumerable.Empty<string>();
            }

            entitlement = entitlement.Trim().ToUpperInvariant();

            return permissions
                .Where(p => p.Entitlements.Contains(entitlement))
                .Select(p => p.IsWildcard ? $"{p.Scope}*" : p.Scope);
        }

        public static bool IsAccessGrant(this IEnumerable<Permission> permissions, string entitlement)
        {
            return permissions.IsAccessGrant(null, entitlement);
        }

        public static bool IsAccessGrant(this IEnumerable<Permission> permissions, string scope, string entitlement)
        {
            if (scope != null)
            {
                scope = scope.Trim();
            }

            if (permissions == null || !permissions.Any())
            {
                return false;
            }

            entitlement = string.IsNullOrWhiteSpace(entitlement)
                ? throw new ArgumentNullException(nameof(entitlement))
                : entitlement.Trim().ToUpperInvariant();

            return permissions
                .Where(p => scope == null || (scope.Length > 0 && p.Scope == scope) || (p.IsWildcard && scope.StartsWith(p.Scope)))
                .Any(p => p.Entitlements.Contains(entitlement));
        }

        public static IEnumerable<Permission> ToPermissions(this IEnumerable<Claim> input)
        {
            return input.Where(c => c.Type != null && c.Value != null && c.Type.StartsWith(Entitlements.CategoryNamingPrefix))
                .Select(c => new
                {
                    c.Type,
                    Parts = c.Value.Split(Entitlements.ScopeSplitor, StringSplitOptions.RemoveEmptyEntries),
                })
                .Where(c => c.Parts.Length == 2)
                .Select(c => new
                {
                    c.Type,
                    Scope = c.Parts[0].Trim(),
                    Entitlements = c.Parts[1],
                })
                .Select(c => new
                {
                    c.Type,
                    IsWildcard = c.Scope.EndsWith("*"),
                    c.Scope,
                    Entitlements = c.Entitlements.Split(Entitlements.ValueSplitor, StringSplitOptions.RemoveEmptyEntries),
                })
                .Select(c => new Permission
                {
                    Category = c.Type,
                    Entitlements = c.Entitlements.Select(e => e.Trim().ToUpperInvariant()),
                    IsWildcard = c.IsWildcard,
                    Scope = c.IsWildcard ? c.Scope.Substring(0, c.Scope.Length == 1 ? 1 : c.Scope.Length - 1) : c.Scope,
                });
        }

        public static IEnumerable<ResourcePermission> ToResourcePermissions(this IEnumerable<Claim> input)
        {
            return input.Where(c => !string.IsNullOrWhiteSpace(c.Type) && !string.IsNullOrWhiteSpace(c.Value) && c.Type.StartsWith("COD-"))
                .Select(c => new
                {
                    Parts = c.Type.Split(new[] { "://" }, StringSplitOptions.RemoveEmptyEntries),
                    Entitlements = c.Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                })
                .Where(c => c.Parts.Length == 2 && c.Parts[0].Length > "COD-".Length)
                .Select(c => new
                {
                    Scheme = c.Parts[0].Substring("COD-".Length),
                    Parts = c.Parts[1].Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries),
                    c.Entitlements,
                })
                .Where(c => c.Parts.Length > 0 && int.TryParse(c.Scheme, out _))
                .Select(c => new ResourcePermission
                {
                    Type = (ResourceType)int.Parse(c.Scheme),
                    Resource = c.Parts[0],
                    Partition = c.Parts.Length > 1 ? c.Parts[1] : null,
                    Scope = c.Parts.Length > 2 ? c.Parts[2] : null,
                    Entitlements = c.Entitlements,
                });
        }
    }
}
