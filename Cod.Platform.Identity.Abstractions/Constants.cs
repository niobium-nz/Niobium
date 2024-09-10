namespace Cod.Platform.Identity
{
    public abstract class Constants
    {
        public const string TenantIDHeaderKey = "X-Tenant";
        public const string DefaultRole = "User";

        public const string IDTokenDefaultIssuer = "cod.platform";
        public const string IDTokenDefaultAudience = "cod.client";
        public const string IDTokenPrivateKey = "ID_TOKEN_PRIVATE_KEY";
        public const string IDTokenPrivateKeyPasscode = "ID_TOKEN_PRIVATE_KEY_PASSCODE";
    }
}
