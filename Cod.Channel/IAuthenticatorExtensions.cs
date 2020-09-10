using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cod.Channel
{
    public static class IAuthenticatorExtensions
    {
        public static bool IsAuthenticated(this IAuthenticator authenticator)
            => authenticator != null && authenticator.Token != null && authenticator.Token.Validate();

        public static async Task<IEnumerable<string>> QueryEntitlementsAsync(this IAuthenticator authenticator, string scope)
        {
            var permissions = await authenticator.GetPermissionsAsync();
            return permissions.QueryEntitlements(scope);
        }

        public static async Task<IEnumerable<string>> QueryScopeAsync(this IAuthenticator authenticator, string entitlement)
        {
            var permissions = await authenticator.GetPermissionsAsync();
            return permissions.QueryScope(entitlement);
        }

        public static async Task<bool> PermissionsGrantAsync(this IAuthenticator authenticator, string entitlement)
        {
            var permissions = await authenticator.GetPermissionsAsync();
            return permissions.IsAccessGrant(entitlement);
        }

        public static async Task<bool> PermissionsGrantAsync(this IAuthenticator authenticator, string scope, string entitlement)
        {
            var permissions = await authenticator.GetPermissionsAsync();
            return permissions.IsAccessGrant(scope, entitlement);
        }

        public static async Task<IEnumerable<Permission>> GetPermissionsAsync(this IAuthenticator authenticator)
        {
            var claims = await authenticator.GetClaimsAsync();
            if (!claims.IsSuccess)
            {
                if (claims.Code == InternalError.AuthenticationRequired)
                {
                    return null;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            return claims.Result.ToPermissions();
        }

        public static async Task<string> GetClaimAsync(this IAuthenticator authenticator, string claimType)
        {
            var claims = await authenticator.GetClaimsAsync();
            if (!claims.IsSuccess)
            {
                if (claims.Code == InternalError.AuthenticationRequired)
                {
                    return null;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            if (claims.Result.TryGetClaim(claimType, out var result))
            {
                return result;
            }

            return null;
        }
    }
}
