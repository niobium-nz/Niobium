using System.Diagnostics.CodeAnalysis;

namespace Cod.Platform.Identity
{
    [method: SetsRequiredMembers]
    public class IdentityServiceOptions()
    {
        public required bool EnableAuthenticationEndpoint { get; set; } = true;

        public required string AuthenticateEndpoint { get; set; } = "/auth";

        public string? IDTokenPublicKey { get; set; }

        public required string IDTokenAudience { get; set; } = "cod.client";

        public required string IDTokenIssuer { get; set; } = "cod.platform";

        public string? AccessTokenSecret { get; set; }

        public void Validate()
        {
            if (EnableAuthenticationEndpoint)
            {
                ArgumentNullException.ThrowIfNull(IDTokenPublicKey, nameof(IDTokenPublicKey));
                ArgumentNullException.ThrowIfNull(AccessTokenSecret, nameof(AccessTokenSecret));
            }
        }
    }
}
