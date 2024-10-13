using System.Diagnostics.CodeAnalysis;

namespace Cod.Channel.Identity
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
            this.Scheme = scheme;
            this.Identity = identity;
            this.Remember = remember;
        }

        [SetsRequiredMembers]
        public LoginCommandParameter(string scheme, string identity, string credential, bool remember)
            : this(scheme, identity, credential, remember, null)
        {
        }

        [SetsRequiredMembers]
        public LoginCommandParameter(string scheme, string identity, string credential, bool remember, string? returnUrl)
        {
            this.Scheme = scheme;
            this.Identity = identity;
            this.Credential = credential;
            this.ReturnUrl = returnUrl;
            this.Remember = remember;
        }

        public required string Scheme { get; set; }

        public required string Identity { get; set; }

        public required bool Remember { get; set; }

        public string? Credential { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
