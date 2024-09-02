namespace Cod
{
    public abstract class AuthenticationScheme
    {
        public const string BasicLoginScheme = "Basic";
        public const string DigestLoginScheme = "Digest";
        public const string BearerLoginScheme = "Bearer";
        public const string OAuthLoginScheme = "OAuth";

        public const string DeviceLoginScheme = "Device";
        public const string WechatLoginScheme = "OAuth";
    }
}
