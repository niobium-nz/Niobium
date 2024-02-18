using System;
using System.Security.Cryptography;
using System.Text;

namespace Cod
{
    public static class AES
    {
        public static string Encrypt(string plainText, string key)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            using RijndaelManaged rijndaelManaged = GetRijndaelManaged(key);
            return Encrypt(plainBytes, rijndaelManaged).ToHex();
        }

        public static string Decrypt(string encryptedText, string key)
        {
            byte[] encryptedBytes = encryptedText.FromHex();
            using RijndaelManaged rijndaelManaged = GetRijndaelManaged(key);
            return Encoding.UTF8.GetString(Decrypt(encryptedBytes, rijndaelManaged));
        }

        private static byte[] Encrypt(byte[] plainBytes, RijndaelManaged rijndaelManaged)
        {
            return rijndaelManaged.CreateEncryptor()
                .TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        }

        private static byte[] Decrypt(byte[] encryptedData, RijndaelManaged rijndaelManaged)
        {
            return rijndaelManaged.CreateDecryptor()
                .TransformFinalBlock(encryptedData, 0, encryptedData.Length);
        }

        private static RijndaelManaged GetRijndaelManaged(string secretKey)
        {
            byte[] keyBytes = new byte[16];
            byte[] secretKeyBytes = Encoding.UTF8.GetBytes(secretKey);
            Array.Copy(secretKeyBytes, keyBytes, Math.Min(keyBytes.Length, secretKeyBytes.Length));
            return new RijndaelManaged
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                KeySize = 128,
                BlockSize = 128,
                Key = keyBytes,
                IV = new byte[16] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, }
            };
        }
    }
}
