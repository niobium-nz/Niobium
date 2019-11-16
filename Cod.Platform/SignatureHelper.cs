using System.Security.Cryptography;
using System.Text;

namespace Cod.Platform
{
    public static class SignatureHelper
    {
        public static string GetSignature(string input, string key)
        {
            using (var sha256 = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
            {
                byte[] source = Encoding.UTF8.GetBytes(input);
                byte[] crypto = sha256.ComputeHash(source);
                return crypto.ToHex();
            }
        }

        public static string ToHex(this byte[] input)
        {
            var result = new StringBuilder();
            for (int i = 0; i < input.Length; i++)
            {
                result.Append(input[i].ToString("X2"));
            }
            return result.ToString().ToLower();
        }
    }
}
