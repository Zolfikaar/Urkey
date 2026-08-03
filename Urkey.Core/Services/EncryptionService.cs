using System;
using System.Security.Cryptography;
using System.Text;

namespace Urkey.Core.Services
{
    /// <summary>
    /// Vault crypto: PBKDF2-SHA256 key derivation, HKDF domain separation
    /// (verifier vs encryption key), AES-256-GCM at rest.
    /// Supports decrypting legacy AES-CBC blobs for automatic migration.
    /// </summary>
    public static class EncryptionService
    {
        public const int CurrentCryptoVersion = 1;
        public const int CurrentKdfIterations = 600_000;
        public const int LegacyKdfIterations = 100_000;

        private const int KeySizeBytes = 32;
        private const int SaltSizeBytes = 16;
        private const int NonceSizeBytes = 12;
        private const int TagSizeBytes = 16;

        private static readonly byte[] GcmMagic = Encoding.ASCII.GetBytes("URK1");
        private static readonly byte[] HkdfInfoEnc = Encoding.UTF8.GetBytes("Urkey.v1.enc");
        private static readonly byte[] HkdfInfoVerify = Encoding.UTF8.GetBytes("Urkey.v1.verify");

        private static byte[]? _key;

        public static bool IsInitialized => _key is { Length: > 0 };

        /// <summary>
        /// Derive encryption key from password and keep it in memory for Encrypt/Decrypt.
        /// </summary>
        public static void InitializeFromPassword(string masterPassword, string saltBase64, int iterations = CurrentKdfIterations)
        {
            ArgumentException.ThrowIfNullOrEmpty(masterPassword);
            ArgumentException.ThrowIfNullOrEmpty(saltBase64);

            byte[] salt = Convert.FromBase64String(saltBase64);
            var (encKey, verifier) = DeriveKeys(masterPassword, salt, iterations);
            CryptographicOperations.ZeroMemory(verifier);
            SetKey(encKey);
        }

        /// <summary>
        /// Create a verifier that is NOT the encryption key (HKDF domain-separated).
        /// </summary>
        public static string CreateVerifier(string password, string saltBase64, int iterations = CurrentKdfIterations)
        {
            ArgumentException.ThrowIfNullOrEmpty(password);
            ArgumentException.ThrowIfNullOrEmpty(saltBase64);

            byte[] salt = Convert.FromBase64String(saltBase64);
            var (encKey, verifier) = DeriveKeys(password, salt, iterations);
            try
            {
                return Convert.ToBase64String(verifier);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(encKey);
                CryptographicOperations.ZeroMemory(verifier);
            }
        }

        /// <summary>
        /// Verify password and, on success, install the encryption key.
        /// </summary>
        public static bool TryUnlock(string password, string saltBase64, string storedVerifierBase64, int iterations = CurrentKdfIterations)
        {
            if (string.IsNullOrEmpty(password) ||
                string.IsNullOrEmpty(saltBase64) ||
                string.IsNullOrEmpty(storedVerifierBase64))
                return false;

            byte[] salt = Convert.FromBase64String(saltBase64);
            byte[] stored = Convert.FromBase64String(storedVerifierBase64);
            var (encKey, verifier) = DeriveKeys(password, salt, iterations);
            try
            {
                if (!CryptographicOperations.FixedTimeEquals(verifier, stored))
                    return false;

                SetKey(encKey);
                encKey = null!; // ownership transferred
                return true;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(verifier);
                CryptographicOperations.ZeroMemory(stored);
                if (encKey != null)
                    CryptographicOperations.ZeroMemory(encKey);
            }
        }

        /// <summary>
        /// Legacy v0 unlock: stored hash was the raw PBKDF2 output (also used as AES key).
        /// </summary>
        public static bool TryUnlockLegacy(string password, string saltBase64, string storedHashBase64)
        {
            if (string.IsNullOrEmpty(password) ||
                string.IsNullOrEmpty(saltBase64) ||
                string.IsNullOrEmpty(storedHashBase64))
                return false;

            byte[] salt = Convert.FromBase64String(saltBase64);
            byte[] stored = Convert.FromBase64String(storedHashBase64);
            byte[] key = Pbkdf2(password, salt, LegacyKdfIterations);
            try
            {
                if (!CryptographicOperations.FixedTimeEquals(key, stored))
                    return false;

                SetKey(key);
                key = null!;
                return true;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(stored);
                if (key != null)
                    CryptographicOperations.ZeroMemory(key);
            }
        }

        public static string GenerateSaltBase64()
        {
            byte[] salt = new byte[SaltSizeBytes];
            RandomNumberGenerator.Fill(salt);
            return Convert.ToBase64String(salt);
        }

        public static string Encrypt(string plainText)
        {
            if (_key == null)
                throw new InvalidOperationException("EncryptionService not initialized.");
            ArgumentNullException.ThrowIfNull(plainText);

            byte[] plaintext = Encoding.UTF8.GetBytes(plainText);
            byte[] nonce = new byte[NonceSizeBytes];
            RandomNumberGenerator.Fill(nonce);
            byte[] ciphertext = new byte[plaintext.Length];
            byte[] tag = new byte[TagSizeBytes];

            try
            {
                using var aes = new AesGcm(_key, TagSizeBytes);
                aes.Encrypt(nonce, plaintext, ciphertext, tag);

                byte[] blob = new byte[GcmMagic.Length + NonceSizeBytes + TagSizeBytes + ciphertext.Length];
                int offset = 0;
                Buffer.BlockCopy(GcmMagic, 0, blob, offset, GcmMagic.Length);
                offset += GcmMagic.Length;
                Buffer.BlockCopy(nonce, 0, blob, offset, nonce.Length);
                offset += nonce.Length;
                Buffer.BlockCopy(tag, 0, blob, offset, tag.Length);
                offset += tag.Length;
                Buffer.BlockCopy(ciphertext, 0, blob, offset, ciphertext.Length);

                return Convert.ToBase64String(blob);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plaintext);
                CryptographicOperations.ZeroMemory(nonce);
            }
        }

        public static string Decrypt(string cipherText)
        {
            if (_key == null)
                throw new InvalidOperationException("EncryptionService not initialized.");
            ArgumentException.ThrowIfNullOrEmpty(cipherText);

            byte[] blob = Convert.FromBase64String(cipherText.Trim());
            if (blob.Length == 0)
                throw new CryptographicException("Ciphertext is empty.");

            if (IsGcmBlob(blob))
                return DecryptGcm(blob);

            return DecryptLegacyCbc(blob);
        }

        /// <summary>
        /// Zero and discard the in-memory encryption key (call on lock/logout).
        /// </summary>
        public static void Clear()
        {
            if (_key == null) return;
            CryptographicOperations.ZeroMemory(_key);
            _key = null;
        }

        private static bool IsGcmBlob(byte[] blob)
        {
            if (blob.Length < GcmMagic.Length + NonceSizeBytes + TagSizeBytes)
                return false;

            for (int i = 0; i < GcmMagic.Length; i++)
            {
                if (blob[i] != GcmMagic[i])
                    return false;
            }

            return true;
        }

        private static string DecryptGcm(byte[] blob)
        {
            int offset = GcmMagic.Length;
            byte[] nonce = new byte[NonceSizeBytes];
            byte[] tag = new byte[TagSizeBytes];
            int cipherLen = blob.Length - GcmMagic.Length - NonceSizeBytes - TagSizeBytes;
            if (cipherLen < 0)
                throw new CryptographicException("Invalid GCM ciphertext length.");

            byte[] ciphertext = new byte[cipherLen];
            Buffer.BlockCopy(blob, offset, nonce, 0, NonceSizeBytes);
            offset += NonceSizeBytes;
            Buffer.BlockCopy(blob, offset, tag, 0, TagSizeBytes);
            offset += TagSizeBytes;
            Buffer.BlockCopy(blob, offset, ciphertext, 0, cipherLen);

            byte[] plaintext = new byte[cipherLen];
            try
            {
                using var aes = new AesGcm(_key!, TagSizeBytes);
                aes.Decrypt(nonce, ciphertext, tag, plaintext);
                return Encoding.UTF8.GetString(plaintext);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plaintext);
                CryptographicOperations.ZeroMemory(nonce);
            }
        }

        private static string DecryptLegacyCbc(byte[] fullCipher)
        {
            if (fullCipher.Length <= 16)
                throw new CryptographicException("Invalid legacy ciphertext.");

            using var aes = Aes.Create();
            aes.Key = _key!;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            byte[] iv = new byte[16];
            Array.Copy(fullCipher, 0, iv, 0, iv.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var ms = new System.IO.MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new System.IO.StreamReader(cs, Encoding.UTF8);
            return sr.ReadToEnd();
        }

        private static (byte[] EncKey, byte[] Verifier) DeriveKeys(string password, byte[] salt, int iterations)
        {
            byte[] master = Pbkdf2(password, salt, iterations);
            try
            {
                byte[] encKey = HKDF.DeriveKey(HashAlgorithmName.SHA256, master, KeySizeBytes, salt: null, info: HkdfInfoEnc);
                byte[] verifier = HKDF.DeriveKey(HashAlgorithmName.SHA256, master, KeySizeBytes, salt: null, info: HkdfInfoVerify);
                return (encKey, verifier);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(master);
            }
        }

        private static byte[] Pbkdf2(string password, byte[] salt, int iterations)
        {
            return Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                KeySizeBytes);
        }

        private static void SetKey(byte[] key)
        {
            Clear();
            _key = key;
        }
    }
}
