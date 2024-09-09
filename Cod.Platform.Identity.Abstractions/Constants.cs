namespace Cod.Platform.Identity
{
    public abstract class Constants
    {
        public const string IDTokenIssuer = "cod.platform";
        public const string IDTokenAudience = "ID_TOKEN_AUDIENCE";
        public const string IDTokenPublicKey = "ID_TOKEN_PUBLIC_KEY";
        public const string IDTokenPrivateKey = "ID_TOKEN_PRIVATE_KEY";
        public const string IDTokenPrivateKeyPasscode = "ID_TOKEN_PRIVATE_KEY_PASSCODE";
        public const string AuthenticateEndpoint = "/auth";
    }
}
