using System.Diagnostics.CodeAnalysis;

namespace Cod.Platform.Identity
{
    [method: SetsRequiredMembers]
    public class IdentityServiceOptions()
    {
        public static IdentityServiceOptions? Instance { get; set; }

        public required string AuthenticateEndpoint { get; set; } = "/auth";

        public required string ResourceSharedAccessSignatureEndpoint { get; set; } = "/rsas/{type:int}/{resource}/{partition?}/{id?}";

        public required string IDTokenAudience { get; set; } = "cod.client";

        public required string IDTokenIssuer { get; set; } = "cod.platform";

        public required string AccessTokenAudience { get; set; } = "cod.client";

        public required string AccessTokenIssuer { get; set; } = "cod.platform";

        public required string DefaultRole { get; set; } = "User";

        public string? AccessTokenSecret { get; set; }

        public string? IDTokenPublicKey { get; set; }

        public string? IDTokenPrivateKey { get; set; }

        public string? IDTokenPrivateKeyPasscode { get; set; }

        public required TimeSpan TokenValidity { get; set; } = TimeSpan.FromHours(8);

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
