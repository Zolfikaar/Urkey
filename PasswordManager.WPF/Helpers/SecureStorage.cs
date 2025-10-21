using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PasswordManager.WPF.Helpers
{
    public static class SecureStorage
    {
        private static readonly string _filePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PasswordManager", "secure.dat");

        public static void SaveSecure(string key, string value)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            string encrypted = EncryptString(value);
            File.WriteAllText(_filePath, encrypted);
        }

        public static string? LoadSecure(string key)
        {
            if (!File.Exists(_filePath))
                return null;

            string encrypted = File.ReadAllText(_filePath);
            return DecryptString(encrypted);
        }

        private static string EncryptString(string plainText)
        {
            byte[] data = Encoding.UTF8.GetBytes(plainText);
            byte[] encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encrypted);
        }

        private static string DecryptString(string encryptedText)
        {
            byte[] data = Convert.FromBase64String(encryptedText);
            byte[] decrypted = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
    }
}
