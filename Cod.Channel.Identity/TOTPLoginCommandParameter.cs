using System.Diagnostics.CodeAnalysis;

namespace Cod.Channel.Identity
{
    public class TOTPLoginCommandParameter
    {
        [SetsRequiredMembers]
        public TOTPLoginCommandParameter(string username)
        {
            this.Username = username;
            this.Remember = false;
        }

        [SetsRequiredMembers]
        public TOTPLoginCommandParameter(string username, string totp)
            : this(username, totp, false, null)
        {
        }

        [SetsRequiredMembers]
        public TOTPLoginCommandParameter(string username, string totp, bool remember)
            : this(username, totp, remember, null)
        {
        }

        [SetsRequiredMembers]
        public TOTPLoginCommandParameter(string username, string totp, bool remember, string? returnUrl)
        {
            this.Username = username;
            this.TOTP = totp;
            this.ReturnUrl = returnUrl;
            this.Remember = remember;
        }

        public required string Username { get; set; }

        public required bool Remember { get; set; }

        public string? TOTP { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
