using System.Security.Claims;

namespace Niobium.Identity
{
    public static class IAuthenticatorExtensions
    {
        public static async Task<IEnumerable<string>> QueryEntitlementsAsync(this IAuthenticator authenticator, string scope, CancellationToken? cancellationToken = null)
        {
            IEnumerable<Permission> permissions = await authenticator.GetPermissionsAsync(cancellationToken);
            return permissions.QueryEntitlements(scope);
        }

        public static async Task<IEnumerable<string>> QueryScopeAsync(this IAuthenticator authenticator, string entitlement, CancellationToken? cancellationToken = null)
        {
            IEnumerable<Permission> permissions = await authenticator.GetPermissionsAsync(cancellationToken);
            return permissions.QueryScope(entitlement);
        }

        public static async Task<bool> PermissionsGrantAsync(this IAuthenticator authenticator, string entitlement, CancellationToken? cancellationToken = null)
        {
            IEnumerable<Permission> permissions = await authenticator.GetPermissionsAsync(cancellationToken);
            return permissions.IsAccessGrant(entitlement);
        }

        public static async Task<bool> PermissionsGrantAsync(this IAuthenticator authenticator, string scope, string entitlement, CancellationToken? cancellationToken = null)
        {
            IEnumerable<Permission> permissions = await authenticator.GetPermissionsAsync(cancellationToken);
            return permissions.IsAccessGrant(scope, entitlement);
        }

        public static async Task<IEnumerable<Permission>> GetPermissionsAsync(this IAuthenticator authenticator, CancellationToken? cancellationToken = null)
        {
            cancellationToken ??= CancellationToken.None;
            IEnumerable<Claim>? claims = await authenticator.GetClaimsAsync(cancellationToken.Value);
            return claims == null || !claims.Any() ? [] : claims.ToPermissions();
        }

        public static async Task<IEnumerable<ResourcePermission>> GetResourcePermissionsAsync(this IAuthenticator authenticator, CancellationToken? cancellationToken = null)
        {
            cancellationToken ??= CancellationToken.None;
            IEnumerable<Claim>? claims = await authenticator.GetClaimsAsync(cancellationToken.Value);
            return claims == null || !claims.Any() ? [] : claims.ToResourcePermissions();
        }

        public static async Task<string?> GetClaimAsync(this IAuthenticator authenticator, string claimType, CancellationToken? cancellationToken = null)
        {
            cancellationToken ??= CancellationToken.None;
            IEnumerable<Claim>? claims = await authenticator.GetClaimsAsync(cancellationToken.Value);
            if (claims == null || !claims.Any())
            {
                return null;
            }

            Claim? claim = claims.SingleOrDefault(c => c.Type == claimType);
            return claim?.Value;
        }

        public static async Task<Guid?> GetUserIDAsync(this IAuthenticator authenticator, CancellationToken? cancellationToken = null)
        {
            cancellationToken ??= CancellationToken.None;
            bool authenticated = await authenticator.GetAuthenticateStatus(cancellationToken.Value);
            if (!authenticated)
            {
                return null;
            }

            string? id = await authenticator.GetClaimAsync(ClaimTypes.NameIdentifier, cancellationToken.Value);
            return !string.IsNullOrWhiteSpace(id) && Guid.TryParse(id, out Guid result) ? result : null;
        }
    }
}
