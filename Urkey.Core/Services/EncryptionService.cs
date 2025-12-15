using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Urkey.Core.Services
{
    public static class EncryptionService
    {
        private static byte[] _key;

        /// <summary>
        /// Initialize the service with a master password.
        /// Derives a key from the password using PBKDF2.
        /// </summary>
        public static void Initialize(string masterPassword = "123456")
        {
            
            // Use a fixed application salt (later you can persist a unique salt per user/vault)
            // byte[] salt = Encoding.UTF8.GetBytes("YourAppFixedSalt123!");
            byte[] salt = 

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
            // استخدام UTF8 encoding بشكل صريح لضمان كتابة صحيحة
            using (var sw = new StreamWriter(cs, Encoding.UTF8))
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
            // استخدام UTF8 encoding بشكل صريح لضمان قراءة صحيحة
            using var sr = new StreamReader(cs, Encoding.UTF8);

            return sr.ReadToEnd();
        }
        
        public static string HashPassword(string password, string salt)
        {
            using var deriveBytes = new Rfc2898DeriveBytes(
                password,
                Convert.FromBase64String(salt),
                100_000,
                HashAlgorithmName.SHA256
            );

            return Convert.ToBase64String(deriveBytes.GetBytes(32));
        }
    }
}
