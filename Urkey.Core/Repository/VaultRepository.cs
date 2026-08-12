using System.IO;
using System.Text.Json;
using Urkey.Core.Models;
using Urkey.Core.Paths;
using Urkey.Core.Services;

namespace Urkey.Core.Repository
{
    public class VaultRepository
    {
        private readonly string _vaultDirectory;
        private readonly string _vaultFilePath;

        public VaultRepository(string? customVaultDirectory = null)
        {
            if (string.IsNullOrWhiteSpace(customVaultDirectory))
            {
                AppDataPaths.EnsureInitialized();
                _vaultDirectory = AppDataPaths.RootDirectory;
                _vaultFilePath = AppDataPaths.VaultFilePath;
            }
            else
            {
                _vaultDirectory = customVaultDirectory;
                Directory.CreateDirectory(_vaultDirectory);
                _vaultFilePath = Path.Combine(_vaultDirectory, AppDataPaths.VaultFileName);
            }
        }

        public bool VaultExists() => File.Exists(_vaultFilePath);

        public Vault Load()
        {
            if (!VaultExists())
                return new Vault();

            var encryptedJson = File.ReadAllText(_vaultFilePath);
            if (string.IsNullOrWhiteSpace(encryptedJson))
                return new Vault();

            try
            {
                var decryptedJson = EncryptionService.Decrypt(encryptedJson);
                return JsonSerializer.Deserialize<Vault>(decryptedJson) ?? new Vault();
            }
            catch (Exception ex)
            {
                // Never return an empty vault when ciphertext exists — that risked overwriting real data.
                throw new InvalidOperationException(
                    "Failed to decrypt vault. Wrong key or corrupted vault file.", ex);
            }
        }

        public void Save(Vault vault)
        {
            var json = JsonSerializer.Serialize(vault, new JsonSerializerOptions { WriteIndented = true });
            var encrypted = EncryptionService.Encrypt(json);
            File.WriteAllText(_vaultFilePath, encrypted);
        }

        public string GetVaultDirectory() => _vaultDirectory;
        public string GetVaultPath() => _vaultFilePath;
    }
}
