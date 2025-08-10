namespace Cod.Identity
{
    public class IdentityHelper
    {
        public const string TOTPCredentialSplit = "|";
        public const string TOTPCredentialPrefix = "TOTP";
        public const int TOTPLength = 6;

        public static string BuildTOTPCredential(string totp)
        {
            return $"TOTP|{totp.Trim()}";
        }

        public static bool TryParseTOTP(string credential, out string totp)
        {
            totp = string.Empty;
            string[] parts = credential.Split(TOTPCredentialSplit);
            if (parts.Length != 2
                || parts[0] != TOTPCredentialPrefix
                || parts[1].Length != TOTPLength
                || !parts[1].All(char.IsDigit))
            {
                return false;
            }

            totp = parts[1];
            return true;
        }

        public static string BuildIdentity(Guid app, string username)
        {
            return $"{app}|{username.Trim()}";
        }

        public static bool TryParseAppAndUserName(string identity, out Guid app, out string username)
        {
            app = Guid.Empty;
            username = string.Empty;

            if (string.IsNullOrWhiteSpace(identity))
            {
                return false;
            }

            string[] parts = identity.Split('|');
            if (parts.Length != 2)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
            {
                return false;
            }

            if (!Guid.TryParse(parts[0], out app))
            {
                return false;
            }

            username = parts[1];
            return true;
        }
    }
}
