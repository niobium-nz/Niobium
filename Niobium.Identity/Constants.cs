namespace Niobium.Identity
{
    public abstract class Constants : Niobium.Constants
    {
        public const string DefaultIDTokenEndpoint = "auth";
        public const string DefaultAccessTokenEndpoint = "token";
        public const string DefaultResourceTokenEndpoint = "rsas";
        public const string DefaultProfileEndpoint = "profile";
        public const string DefaultIDTokenAudience = "niobium.client";
        public const string DefaultIDTokenIssuer = "niobium.platform";
        public const string DefaultAccessTokenAudience = "niobium.client";
        public const string DefaultAccessTokenIssuer = "niobium.platform";
        public const string DefaultRole = "User";

        public const string ClaimKeyPrefix = "NB-";
        public const string IDTokenHeaderKey = "X-IDT";
    }
}
