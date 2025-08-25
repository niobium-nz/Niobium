using System.Diagnostics.CodeAnalysis;

namespace Niobium.Channel.Identity
{
    public class TOTPLoginCommandParameter
    {
        [SetsRequiredMembers]
        public TOTPLoginCommandParameter(string username)
        {
            Username = username;
            Remember = false;
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
            Username = username;
            TOTP = totp;
            ReturnUrl = returnUrl;
            Remember = remember;
        }

        public required string Username { get; set; }

        public required bool Remember { get; set; }

        public string? TOTP { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
