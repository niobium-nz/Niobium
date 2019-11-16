using System.Security.Cryptography;
using System.Text;

namespace Cod.Platform
{
    internal static class SHA
    {
        public static string SHA256Hash(string data, string key, int maxLength)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data)).ToHex();
                if (hash.Length > maxLength)
                {
                    hash = hash.Substring(0, maxLength);
                }
                return hash;
            }
        }
    }
}
