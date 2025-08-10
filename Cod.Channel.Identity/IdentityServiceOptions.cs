namespace Cod.Channel.Identity
{
    public class IdentityServiceOptions
    {
        public const string DefaultLoginUri = "/login";

        public required Guid App { get; set; }

        public string? PlatformAPIEndpoint { get; set; }

        public required string IDTokenHost { get; set; }

        public required string IDTokenEndpoint { get; set; } = Cod.Identity.Constants.DefaultIDTokenEndpoint;

        public required string AccessTokenHost { get; set; }

        public required string AccessTokenEndpoint { get; set; } = Cod.Identity.Constants.DefaultAccessTokenEndpoint;

        public required string ResourceTokenHost { get; set; }

        public required string ResourceTokenEndpoint { get; set; } = Cod.Identity.Constants.DefaultResourceTokenEndpoint;

        public string IDTokenAudience { get; set; } = Cod.Identity.Constants.DefaultIDTokenAudience;

        public string IDTokenIssuer { get; set; } = Cod.Identity.Constants.DefaultIDTokenIssuer;

        public string AccessTokenAudience { get; set; } = Cod.Identity.Constants.DefaultAccessTokenAudience;

        public string AccessTokenIssuer { get; set; } = Cod.Identity.Constants.DefaultAccessTokenIssuer;

        public string LoginUri { get; set; } = DefaultLoginUri;

        public TimeSpan MaxClockSkewTolerence { get; set; } = TimeSpan.FromMinutes(5);

        public void Validate()
        {
            ArgumentNullException.ThrowIfNull(IDTokenHost, nameof(IDTokenHost));
            IDTokenHost = IDTokenHost.Trim();
            if (IDTokenHost.EndsWith('/'))
            {
                IDTokenHost = IDTokenHost[..^1];
            }

            ArgumentNullException.ThrowIfNull(AccessTokenHost, nameof(AccessTokenHost));
            AccessTokenHost = AccessTokenHost.Trim();
            if (AccessTokenHost.EndsWith('/'))
            {
                AccessTokenHost = AccessTokenHost[..^1];
            }

            ArgumentNullException.ThrowIfNull(ResourceTokenHost, nameof(ResourceTokenHost));
            ResourceTokenHost = ResourceTokenHost.Trim();
            if (ResourceTokenHost.EndsWith('/'))
            {
                ResourceTokenHost = ResourceTokenHost[..^1];
            }

            ArgumentNullException.ThrowIfNull(IDTokenEndpoint, nameof(IDTokenEndpoint));
            IDTokenEndpoint = IDTokenEndpoint.Trim();
            if (!IDTokenEndpoint.StartsWith('/'))
            {
                IDTokenEndpoint = $"/{IDTokenEndpoint}";
            }

            ArgumentNullException.ThrowIfNull(AccessTokenEndpoint, nameof(AccessTokenEndpoint));
            AccessTokenEndpoint = AccessTokenEndpoint.Trim();
            if (!AccessTokenEndpoint.StartsWith('/'))
            {
                AccessTokenEndpoint = $"/{AccessTokenEndpoint}";
            }

            ArgumentNullException.ThrowIfNull(ResourceTokenEndpoint, nameof(ResourceTokenEndpoint));
            ResourceTokenEndpoint = ResourceTokenEndpoint.Trim();
            if (!ResourceTokenEndpoint.StartsWith('/'))
            {
                ResourceTokenEndpoint = $"/{ResourceTokenEndpoint}";
            }

            ArgumentNullException.ThrowIfNull(LoginUri, nameof(LoginUri));
            LoginUri = LoginUri.Trim();
            if (!LoginUri.StartsWith('/'))
            {
                LoginUri = $"/{LoginUri}";
            }
        }
    }
}
