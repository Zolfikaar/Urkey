using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PasswordManager.Core.Services
{
    public static class EncryptionService
    {
        private static byte[] _key;

        /// <summary>
        /// Initialize the service with a master password.
        /// Derives a key from the password using PBKDF2.
        /// </summary>
        public static void Initialize(string masterPassword)
        {
            // Use a fixed application salt (later you can persist a unique salt per user/vault)
            byte[] salt = Encoding.UTF8.GetBytes("YourAppFixedSalt123!");

            using var keyDerivation = new Rfc2898DeriveBytes(
                masterPassword,
                salt,
                100_000,
                HashAlgorithmName.SHA256);

            _key = keyDerivation.GetBytes(32); // AES-256 key
        }

        public static string Encrypt(string plainText)
        {
            if (_key == null) throw new InvalidOperationException("EncryptionService not initialized.");

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();
            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        public static string Decrypt(string cipherText)
        {
            if (_key == null) throw new InvalidOperationException("EncryptionService not initialized.");

            var fullCipher = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.Key = _key;

            byte[] iv = new byte[16];
            Array.Copy(fullCipher, 0, iv, 0, iv.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }
    }
}
