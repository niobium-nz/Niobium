namespace Cod.Platform.Identity
{
    public enum CredentialKind : int
    {
        Unknown = 0,

        TOTP = 1,

        Authenticator = 2,
    }
}
