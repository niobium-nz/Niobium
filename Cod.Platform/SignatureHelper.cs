using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Cod.Platform
{
    internal static class SignatureHelper
    {
        public static string GetTenantSecret(byte[] tenant, string authSecret)
        {
            using var sha256 = new HMACSHA256(Encoding.UTF8.GetBytes(authSecret));
            var crypto = sha256.ComputeHash(tenant);

            var result = new StringBuilder();
            for (var i = 0; i < crypto.Length; i++)
            {
                result.Append(crypto[i].ToString("X2", CultureInfo.InvariantCulture));
            }
            return result.ToString().Substring(16, 32).ToLowerInvariant();
        }
    }
}
