using System;
using System.Security.Cryptography;

namespace Cod
{
    public static class MD5Helper
    {
        public static string ComputeHashAsBase64String(byte[] input)
        {
            using MD5 md5 = MD5.Create();
            byte[] bs = md5.ComputeHash(input);
            return Convert.ToBase64String(bs);
        }
    }
}
