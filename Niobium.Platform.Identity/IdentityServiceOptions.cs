namespace Niobium.Platform.Identity
{
    public class IdentityServiceOptions
    {
        public static IdentityServiceOptions? Instance { get; set; }

        public string IDTokenEndpoint { get; set; } = Niobium.Identity.Constants.DefaultIDTokenEndpoint;

        public string AccessTokenEndpoint { get; set; } = Niobium.Identity.Constants.DefaultAccessTokenEndpoint;

        public string ResourceTokenEndpoint { get; set; } = Niobium.Identity.Constants.DefaultResourceTokenEndpoint;

        public string IDTokenAudience { get; set; } = Niobium.Identity.Constants.DefaultIDTokenAudience;

        public string IDTokenIssuer { get; set; } = Niobium.Identity.Constants.DefaultIDTokenIssuer;

        public string AccessTokenAudience { get; set; } = Niobium.Identity.Constants.DefaultAccessTokenAudience;

        public string AccessTokenIssuer { get; set; } = Niobium.Identity.Constants.DefaultAccessTokenIssuer;

        public string DefaultRole { get; set; } = Niobium.Identity.Constants.DefaultRole;

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
