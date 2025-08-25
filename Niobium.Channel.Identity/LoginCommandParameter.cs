using System.Diagnostics.CodeAnalysis;

namespace Niobium.Channel.Identity
{
    public class LoginCommandParameter
    {
        [SetsRequiredMembers]
        public LoginCommandParameter(string scheme, string identity)
            : this(scheme, identity, false)
        {
        }

        [SetsRequiredMembers]
        public LoginCommandParameter(string scheme, string identity, string credential)
            : this(scheme, identity, credential, false, null)
        {
        }

        [SetsRequiredMembers]
        public LoginCommandParameter(string scheme, string identity, bool remember)
        {
            Scheme = scheme;
            Identity = identity;
            Remember = remember;
        }

        [SetsRequiredMembers]
        public LoginCommandParameter(string scheme, string identity, string credential, bool remember)
            : this(scheme, identity, credential, remember, null)
        {
        }

        [SetsRequiredMembers]
        public LoginCommandParameter(string scheme, string identity, string credential, bool remember, string? returnUrl)
        {
            Scheme = scheme;
            Identity = identity;
            Credential = credential;
            ReturnUrl = returnUrl;
            Remember = remember;
        }

        public required string Scheme { get; set; }

        public required string Identity { get; set; }

        public required bool Remember { get; set; }

        public string? Credential { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
