namespace Cod.Identity
{
    public abstract class Constants
    {
        public const string DefaultIDTokenEndpoint = "auth";
        public const string DefaultAccessTokenEndpoint = "token";
        public const string DefaultResourceTokenEndpoint = "rsas";
        public const string DefaultIDTokenAudience = "cod.client";
        public const string DefaultIDTokenIssuer = "cod.platform";
        public const string DefaultAccessTokenAudience = "cod.client";
        public const string DefaultAccessTokenIssuer = "cod.platform";
        public const string DefaultRole = "User";

        public const string ClaimKeyPrefix = "COD-";
        public const string TenantIDHeaderKey = "X-Tenant";
    }
}
