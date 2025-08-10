using System.Security.Cryptography;
using System.Text;

namespace Cod
{
    public static class AES
    {
        public static string Encrypt(string plainText, string key)
        {
            using Aes aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.GenerateIV();
            aes.Mode = CipherMode.CBC;
            using ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using MemoryStream ms = new();
            ms.Write(aes.IV, 0, aes.IV.Length);
            using (CryptoStream cs = new(ms, encryptor, CryptoStreamMode.Write))
            {
                using StreamWriter sw = new(cs);
                sw.Write(plainText);
            }
            return Convert.ToBase64String(ms.ToArray());
        }

        public static string Decrypt(string encryptedText, string key)
        {
            using Aes aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key);
            byte[] fullCipher = Convert.FromBase64String(encryptedText);
            byte[] iv = new byte[aes.IV.Length];
            Array.Copy(fullCipher, 0, iv, 0, iv.Length);
            aes.IV = iv;
            using ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using MemoryStream ms = new(fullCipher, iv.Length, fullCipher.Length - iv.Length);
            using CryptoStream cs = new(ms, decryptor, CryptoStreamMode.Read);
            using StreamReader sr = new(cs);
            return sr.ReadToEnd();
        }
    }
}
