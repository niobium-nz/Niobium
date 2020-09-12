using System;
using System.Security.Cryptography;
using System.Text;

namespace Cod.Platform
{
    internal static class SignatureExtensions
    {
        public static string GetTenantSecret(this Guid tenant, string authSecret)
        {
            using var sha256 = new HMACSHA256(Encoding.UTF8.GetBytes(authSecret));
            var source = tenant.ToByteArray();
            var crypto = sha256.ComputeHash(source);

            var result = new StringBuilder();
            for (var i = 0; i < crypto.Length; i++)
            {
                result.Append(crypto[i].ToString("X2"));
            }
            return result.ToString().Substring(16, 32).ToLower();
        }
    }
}
