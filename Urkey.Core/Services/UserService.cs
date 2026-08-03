using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Urkey.Core.Managers;
using Urkey.Core.Models;
using Urkey.Core.Repository;

namespace Urkey.Core.Services
{
    public class UserService
    {
        private User _userModel;
        private readonly UserRepository _userRepository;

        public enum FirstSetupError
        {
            None,
            EmptyMasterPassword,
            InvalidMasterPassword,
            InvalidEmail,
            InvalidPhone
        }

        public class FirstSetupResult
        {
            public bool Success { get; set; }
            public FirstSetupError Error { get; set; }
        }

        public enum UnlockStatus
        {
            Success,
            InvalidCredentials,
            MigrationFailed,
            MissingUserData
        }

        public class UnlockResult
        {
            public UnlockStatus Status { get; init; }
            public bool Success => Status == UnlockStatus.Success;
            public bool Migrated { get; init; }
        }

        public UserService()
        {
            _userRepository = new UserRepository();
            _userModel = _userRepository.Load();
        }

        public void Load() => _userModel = _userRepository.Load();

        public void Save() => _userRepository.Save(_userModel);

        public string GetSalt() => _userModel.Salt;

        public User CurrentUser => _userModel;

        public FirstSetupResult FirstSetup(string? email, string? phone, string enteredMasterPassword)
        {
            if (string.IsNullOrWhiteSpace(enteredMasterPassword))
                return new FirstSetupResult { Success = false, Error = FirstSetupError.EmptyMasterPassword };

            if (enteredMasterPassword.Length < 4)
                return new FirstSetupResult { Success = false, Error = FirstSetupError.InvalidMasterPassword };

            if (!string.IsNullOrEmpty(email) && !CheckValidEmail(email))
                return new FirstSetupResult { Success = false, Error = FirstSetupError.InvalidEmail };

            if (!string.IsNullOrEmpty(phone) && !CheckValidPhone(phone))
                return new FirstSetupResult { Success = false, Error = FirstSetupError.InvalidPhone };

            string salt = EncryptionService.GenerateSaltBase64();
            _userModel.Email = email ?? "";
            _userModel.Phone = phone ?? "";
            _userModel.Salt = salt;
            _userModel.KdfIterations = EncryptionService.CurrentKdfIterations;
            _userModel.CryptoVersion = EncryptionService.CurrentCryptoVersion;
            _userModel.HashPassword = EncryptionService.CreateVerifier(
                enteredMasterPassword,
                salt,
                EncryptionService.CurrentKdfIterations);
            _userModel.FirstOpenDate = DateOnly.FromDateTime(DateTime.Now);

            return new FirstSetupResult { Success = true, Error = FirstSetupError.None };
        }

        /// <summary>
        /// Verify master password, unlock encryption key, and auto-migrate legacy vaults.
        /// </summary>
        public UnlockResult Unlock(string enteredMasterPassword)
        {
            Load();

            if (string.IsNullOrEmpty(_userModel.Salt) || string.IsNullOrEmpty(_userModel.HashPassword))
                return new UnlockResult { Status = UnlockStatus.MissingUserData };

            bool isLegacy = _userModel.CryptoVersion < EncryptionService.CurrentCryptoVersion;

            if (isLegacy)
            {
                if (!EncryptionService.TryUnlockLegacy(
                        enteredMasterPassword,
                        _userModel.Salt,
                        _userModel.HashPassword))
                {
                    return new UnlockResult { Status = UnlockStatus.InvalidCredentials };
                }

                try
                {
                    MigrateLegacyVault(enteredMasterPassword);
                }
                catch
                {
                    EncryptionService.Clear();
                    VaultManager.Lock();
                    return new UnlockResult { Status = UnlockStatus.MigrationFailed };
                }

                VaultManager.Unlock();
                return new UnlockResult { Status = UnlockStatus.Success, Migrated = true };
            }

            int iterations = _userModel.KdfIterations > 0
                ? _userModel.KdfIterations
                : EncryptionService.CurrentKdfIterations;

            if (!EncryptionService.TryUnlock(
                    enteredMasterPassword,
                    _userModel.Salt,
                    _userModel.HashPassword,
                    iterations))
            {
                return new UnlockResult { Status = UnlockStatus.InvalidCredentials };
            }

            VaultManager.Unlock();
            return new UnlockResult { Status = UnlockStatus.Success };
        }

        /// <summary>
        /// Back-compat wrapper used by older call sites.
        /// </summary>
        public bool NormalSetup(string enteredMasterPassword)
            => Unlock(enteredMasterPassword).Success;

        /// <summary>
        /// Re-encrypt vault + document files under a new salt/verifier/key.
        /// Caller must already be unlocked with the current key.
        /// </summary>
        public bool ChangeMasterPassword(string currentPassword, string newPassword)
        {
            if (!EncryptionService.IsInitialized || !VaultManager.IsUnlocked)
                return false;

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4)
                return false;

            int iterations = _userModel.KdfIterations > 0
                ? _userModel.KdfIterations
                : EncryptionService.CurrentKdfIterations;

            string check = EncryptionService.CreateVerifier(currentPassword, _userModel.Salt, iterations);
            if (!FixedTimeEqualsBase64(check, _userModel.HashPassword))
                return false;

            var vaultRepo = new VaultRepository();
            Vault vault = vaultRepo.Load();
            var documentPlaintexts = LoadDocumentPlaintexts(vaultRepo.GetVaultDirectory());

            string newSalt = EncryptionService.GenerateSaltBase64();
            string newVerifier = EncryptionService.CreateVerifier(
                newPassword,
                newSalt,
                EncryptionService.CurrentKdfIterations);

            EncryptionService.Clear();
            EncryptionService.InitializeFromPassword(
                newPassword,
                newSalt,
                EncryptionService.CurrentKdfIterations);

            WriteDocumentPlaintexts(documentPlaintexts);
            vaultRepo.Save(vault);

            _userModel.Salt = newSalt;
            _userModel.HashPassword = newVerifier;
            _userModel.CryptoVersion = EncryptionService.CurrentCryptoVersion;
            _userModel.KdfIterations = EncryptionService.CurrentKdfIterations;
            Save();

            VaultManager.Unlock();
            return true;
        }

        private void MigrateLegacyVault(string masterPassword)
        {
            var vaultRepo = new VaultRepository();
            Vault vault = vaultRepo.Load();
            var documentPlaintexts = LoadDocumentPlaintexts(vaultRepo.GetVaultDirectory());

            string newSalt = EncryptionService.GenerateSaltBase64();
            string newVerifier = EncryptionService.CreateVerifier(
                masterPassword,
                newSalt,
                EncryptionService.CurrentKdfIterations);

            EncryptionService.Clear();
            EncryptionService.InitializeFromPassword(
                masterPassword,
                newSalt,
                EncryptionService.CurrentKdfIterations);

            WriteDocumentPlaintexts(documentPlaintexts);
            vaultRepo.Save(vault);

            _userModel.Salt = newSalt;
            _userModel.HashPassword = newVerifier;
            _userModel.CryptoVersion = EncryptionService.CurrentCryptoVersion;
            _userModel.KdfIterations = EncryptionService.CurrentKdfIterations;
            Save();
        }

        private static List<(string Path, string Plaintext)> LoadDocumentPlaintexts(string vaultDirectory)
        {
            var result = new List<(string, string)>();
            string docDir = Path.Combine(vaultDirectory, "DocumentsFiles");
            if (!Directory.Exists(docDir))
                return result;

            foreach (string file in Directory.GetFiles(docDir, "*.img"))
            {
                try
                {
                    string encrypted = File.ReadAllText(file).Trim();
                    if (string.IsNullOrWhiteSpace(encrypted))
                        continue;

                    string plain = EncryptionService.Decrypt(encrypted);
                    result.Add((file, plain));
                }
                catch
                {
                    // Skip unreadable/corrupt document blobs; vault metadata still migrates.
                }
            }

            return result;
        }

        private static void WriteDocumentPlaintexts(List<(string Path, string Plaintext)> documents)
        {
            var utf8NoBom = new System.Text.UTF8Encoding(false);
            foreach (var (path, plaintext) in documents)
            {
                string encrypted = EncryptionService.Encrypt(plaintext);
                File.WriteAllText(path, encrypted, utf8NoBom);
            }
        }

        private static bool FixedTimeEqualsBase64(string a, string b)
        {
            try
            {
                byte[] ba = Convert.FromBase64String(a);
                byte[] bb = Convert.FromBase64String(b);
                try
                {
                    return CryptographicOperations.FixedTimeEquals(ba, bb);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(ba);
                    CryptographicOperations.ZeroMemory(bb);
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool CheckValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private static bool CheckValidPhone(string phone)
            => Regex.IsMatch(phone, @"^(077|078)\d{8}$");
    }
}
