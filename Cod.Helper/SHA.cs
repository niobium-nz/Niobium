using System.Security.Cryptography;
using System.Text;

namespace Cod
{
    public class SHA
    {
        public static string SHA384Hash(string data, int maxLength = -1)
        {
            using HMACSHA384 hmac = new();
            string hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data)).ToHex();
            if (maxLength > 0 && hash.Length > maxLength)
            {
                hash = hash[..maxLength];
            }
            return hash;
        }

        public static string SHA256Hash(string data, int maxLength = -1)
        {
            using HMACSHA256 hmac = new();
            string hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data)).ToHex();
            if (maxLength > 0 && hash.Length > maxLength)
            {
                hash = hash[..maxLength];
            }
            return hash;
        }

        public static string SHA256Hash(string data, string key, int maxLength = -1)
        {
            using HMACSHA256 hmac = new(Encoding.UTF8.GetBytes(key));
            string hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data)).ToHex();
            if (maxLength > 0 && hash.Length > maxLength)
            {
                hash = hash[..maxLength];
            }
            return hash;
        }

        public static byte[] SHA256HashBytes(string data, string key)
        {
            using HMACSHA256 hmac = new(Encoding.UTF8.GetBytes(key));
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms", Justification = "for backward compatiability")]
        public static string SHA1Base64(string data, string key)
        {
            using HMACSHA1 hmac = new(Encoding.UTF8.GetBytes(key));
            byte[] hashValue = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hashValue);
        }
    }
}
