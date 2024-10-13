namespace Cod.Identity
{
    public static class IAuthenticatorExtensions
    {
        public static async Task<IEnumerable<string>> QueryEntitlementsAsync(this IAuthenticator authenticator, string scope, CancellationToken cancellationToken = default)
        {
            var permissions = await authenticator.GetPermissionsAsync(cancellationToken);
            return permissions.QueryEntitlements(scope);
        }

        public static async Task<IEnumerable<string>> QueryScopeAsync(this IAuthenticator authenticator, string entitlement, CancellationToken cancellationToken = default)
        {
            var permissions = await authenticator.GetPermissionsAsync(cancellationToken);
            return permissions.QueryScope(entitlement);
        }

        public static async Task<bool> PermissionsGrantAsync(this IAuthenticator authenticator, string entitlement, CancellationToken cancellationToken = default)
        {
            var permissions = await authenticator.GetPermissionsAsync(cancellationToken);
            return permissions.IsAccessGrant(entitlement);
        }

        public static async Task<bool> PermissionsGrantAsync(this IAuthenticator authenticator, string scope, string entitlement, CancellationToken cancellationToken = default)
        {
            var permissions = await authenticator.GetPermissionsAsync(cancellationToken);
            return permissions.IsAccessGrant(scope, entitlement);
        }

        public static async Task<IEnumerable<Permission>> GetPermissionsAsync(this IAuthenticator authenticator, CancellationToken cancellationToken = default)
        {
            var claims = await authenticator.GetClaimsAsync(cancellationToken);
            if (claims == null || !claims.Any())
            {
                return [];
            }

            return claims.ToPermissions();
        }

        public static async Task<IEnumerable<ResourcePermission>> GetResourcePermissionsAsync(this IAuthenticator authenticator, CancellationToken cancellationToken = default)
        {
            var claims = await authenticator.GetClaimsAsync(cancellationToken);
            if (claims == null || !claims.Any())
            {
                return [];
            }

            return claims.ToResourcePermissions();
        }

        public static async Task<string?> GetClaimAsync(this IAuthenticator authenticator, string claimType, CancellationToken cancellationToken = default)
        {
            var claims = await authenticator.GetClaimsAsync(cancellationToken);
            if (claims == null || !claims.Any())
            {
                return null;
            }

            var claim = claims.SingleOrDefault(c => c.Type == claimType);
            return claim?.Value;
        }
    }
}
