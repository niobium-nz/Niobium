namespace Cod.Platform.Identity
{
    public class IdentityServiceOptions
    {
        public static IdentityServiceOptions? Instance { get; set; }

        public string IDTokenEndpoint { get; set; } = Constants.DefaultIDTokenEndpoint;

        public string AccessTokenEndpoint { get; set; } = Constants.DefaultAccessTokenEndpoint;

        public string ResourceTokenEndpoint { get; set; } = Constants.DefaultResourceTokenEndpoint;

        public string IDTokenAudience { get; set; } = Constants.DefaultIDTokenAudience;

        public string IDTokenIssuer { get; set; } = Constants.DefaultIDTokenIssuer;

        public string AccessTokenAudience { get; set; } = Constants.DefaultAccessTokenAudience;

        public string AccessTokenIssuer { get; set; } = Constants.DefaultAccessTokenIssuer;

        public string DefaultRole { get; set; } = Constants.DefaultRole;

        public string? AccessTokenSecret { get; set; }

        public string? IDTokenPublicKey { get; set; }

        public string? IDTokenPrivateKey { get; set; }

        public string? IDTokenPrivateKeyPasscode { get; set; }

        public TimeSpan TokenValidity { get; set; } = TimeSpan.FromHours(8);

        public void Validate()
        {
            if (string.IsNullOrEmpty(IDTokenPrivateKey))
            {
                ArgumentNullException.ThrowIfNull(IDTokenPublicKey, nameof(IDTokenPublicKey));
                ArgumentNullException.ThrowIfNull(AccessTokenSecret, nameof(AccessTokenSecret));
            }
        }
    }
}
