using System.Security.Cryptography;

namespace Cod
{
    public static class MD5Helper
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms", Justification = "for backward compatiability")]
        public static string ComputeHashAsBase64String(byte[] input)
        {
            byte[] bs = MD5.HashData(input);
            return Convert.ToBase64String(bs);
        }
    }
}
