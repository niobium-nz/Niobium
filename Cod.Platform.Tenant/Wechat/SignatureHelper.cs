using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Cod.Platform.Tenant.Wechat
{
    internal static class SignatureHelper
    {
        public static string GetTenantSecret(byte[] tenant, string authSecret)
        {
            using HMACSHA256 sha256 = new(Encoding.UTF8.GetBytes(authSecret));
            byte[] crypto = sha256.ComputeHash(tenant);

            StringBuilder result = new();
            for (int i = 0; i < crypto.Length; i++)
            {
                result.Append(crypto[i].ToString("X2", CultureInfo.InvariantCulture));
            }
            return result.ToString().Substring(16, 32).ToLowerInvariant();
        }
    }
}
