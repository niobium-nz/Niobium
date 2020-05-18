using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Claims;
using Newtonsoft.Json;

namespace Cod.Platform
{
    public static class ClaimsPrincipalExtensions
    {
        private static readonly Type[] ConvertibleTypes = new[] { typeof(Guid), typeof(DateTime), typeof(DateTimeOffset), typeof(TimeSpan), };

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

            var t = typeof(T);
            if (typeof(IConvertible).IsAssignableFrom(t) || ConvertibleTypes.Contains(t))
            {
                var converter = TypeDescriptor.GetConverter(t);
                result = (T)converter.ConvertFrom(claim.Value);
                return true;
            }
            else
            {
                result = JsonConvert.DeserializeObject<T>(claim.Value);
                return true;
            }
        }
    }
}
