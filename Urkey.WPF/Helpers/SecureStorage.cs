using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Urkey.Core.Paths;

namespace Urkey.WPF.Helpers
{
    public static class SecureStorage
    {
        private static string FilePath
        {
            get
            {
                AppDataPaths.EnsureInitialized();
                return AppDataPaths.SecureStorageFilePath;
            }
        }

        public static void SaveSecure(string key, string value)
        {
            string path = FilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string encrypted = EncryptString(value);
            File.WriteAllText(path, encrypted);
        }

        public static string? LoadSecure(string key)
        {
            string path = FilePath;
            if (!File.Exists(path))
                return null;

            string encrypted = File.ReadAllText(path);
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
