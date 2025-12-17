using System.IO;
using System.Text.Json;
using System.Windows;
using Urkey.Core.Models;
using Urkey.Core.Services;

namespace Urkey.Core.Repository
{
    public class VaultRepository
    {
        private static string _vaultDirectory = string.Empty;
        private static string _vaultFilePath = string.Empty;
        private static Vault? _vault;

        public VaultRepository(string? customVaultDirectory = null)
        {
            _vaultDirectory = string.IsNullOrWhiteSpace(customVaultDirectory)
                ? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Urkey"
                )
                : customVaultDirectory;

            Directory.CreateDirectory(_vaultDirectory);

            _vaultFilePath = Path.Combine(_vaultDirectory, "vault.json");

            LoadOrCreateVault();
        }

        private static void LoadOrCreateVault()
        {
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
                    _vault = new Vault();
                }
            }
            else
            {
                _vault = new Vault();
            }
        }
        
        public static Vault LoadVault() => _vault ??= new Vault();

        public static Vault ReloadFromDisk()
        {
            if (!File.Exists(_vaultFilePath))
            {
                _vault = new Vault();
                return _vault;
            }

            try
            {
                string encryptedJson = File.ReadAllText(_vaultFilePath);
                string decryptedJson = EncryptionService.Decrypt(encryptedJson);
                _vault = JsonSerializer.Deserialize<Vault>(decryptedJson) ?? new Vault();
            }
            catch
            {
                _vault = new Vault();
            }

            return _vault;
        }

        public static void AddEntry(VaultEntry entry)
        {
            if (_vault != null)
            {
                _vault.Entries.Add(entry);
                SaveVault(_vault);
            }
            else
            {
                MessageBox.Show("(msg from AddEntry Method)Vault is not initialized yet", "Error", MessageBoxButton.OK);
                // المفروض نخلي زر لإنشاء الحافظة يدوياً بهذه الحالة
            }
        }

        public static void SaveVault(Vault vault)
        {
            string json = JsonSerializer.Serialize(vault, new JsonSerializerOptions { WriteIndented = true });
            string encrypted = EncryptionService.Encrypt(json);
            File.WriteAllText(_vaultFilePath, encrypted);
        }

        public static string GetVaultPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Urkey",
                "vault.json"
            );
        }

        public static bool VaultExists()
        {
            return File.Exists(GetVaultPath());
        }
        
        /// <summary>
        /// Gets the vault directory path (where vault.json and DocumentsFiles folder are located)
        /// </summary>
        public static string GetVaultDirectory() => _vaultDirectory;


        
    }
}
