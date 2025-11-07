using PasswordManager.Core.Models;
using PasswordManager.Core.Services;
using System;
using System.IO;
using System.Text.Json;

namespace PasswordManager.Core.Repository
{
    public class VaultRepository
    {
        private readonly string _vaultDirectory;
        private readonly string _vaultFilePath;
        private readonly Vault _vault;

        public VaultRepository(string? customPath = null)
        {
            // إذا تم تمرير مسار يدوي نستخدمه، وإلا نحفظ في AppData\PasswordManager
            _vaultDirectory = customPath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PasswordManager"
            );

            Directory.CreateDirectory(_vaultDirectory);
            _vaultFilePath = Path.Combine(_vaultDirectory, "vault.json");

            // تحميل أو إنشاء Vault جديد
            if (File.Exists(_vaultFilePath))
            {
                try
                {
                    string encryptedJson = File.ReadAllText(_vaultFilePath);
                    string decryptedJson = EncryptionService.Decrypt(encryptedJson);
                    _vault = JsonSerializer.Deserialize<Vault>(decryptedJson) ?? new Vault();
                }
                catch
                {
                    // لو حدث خطأ (ملف تالف أو كلمة مرور خاطئة مثلاً)
                    _vault = new Vault();
                }
            }
            else
            {
                _vault = new Vault();
            }
        }

        public Vault LoadVault() => _vault;

        public void AddEntry(VaultEntry entry)
        {
            _vault.Entries.Add(entry);
            SaveVault(_vault);
        }

        public void SaveVault(Vault vault)
        {
            string json = JsonSerializer.Serialize(vault, new JsonSerializerOptions { WriteIndented = true });
            string encrypted = EncryptionService.Encrypt(json);
            File.WriteAllText(_vaultFilePath, encrypted);
        }

        public string GetVaultPath() => _vaultFilePath;
        
        /// <summary>
        /// Gets the vault directory path (where vault.json and DocumentsFiles folder are located)
        /// </summary>
        public string GetVaultDirectory() => _vaultDirectory;
    }
}
