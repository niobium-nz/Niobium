using System;
using System.Security.Cryptography;

namespace Cod
{
    public static class MD5Helper
    {
        public static string ComputeHashAsBase64String(byte[] input)
        {
            using var md5 = MD5.Create();
            var bs = md5.ComputeHash(input);
            return Convert.ToBase64String(bs);
        }
    }
}
