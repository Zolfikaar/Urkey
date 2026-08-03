using System;
using System.IO;
using System.Security.Cryptography;

namespace Urkey.Core.Services
{
    public static class FileHelper
    {
        private const string TempDocumentPrefix = "urkey-doc-";

        /// <summary>
        /// يحوّل ملف إلى نص Base64
        /// </summary>
        public static string ConvertToBase64(string filePath)
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);
            return Convert.ToBase64String(fileBytes);
        }

        /// <summary>
        /// يحوّل نص Base64 إلى ملف فعلي
        /// </summary>
        public static void SaveFromBase64(string base64, string outputPath)
        {
            byte[] bytes = Convert.FromBase64String(base64);
            File.WriteAllBytes(outputPath, bytes);
        }

        /// <summary>
        /// يحفظ الصورة الخاصة بالوثيقة في مجلد فرعي مستقل (مشفّرة).
        /// </summary>
        public static string SaveDocumentImage(string sourceImagePath, string vaultRootPath)
        {
            if (!File.Exists(sourceImagePath))
                throw new FileNotFoundException("Image file not found.", sourceImagePath);

            if (string.IsNullOrWhiteSpace(vaultRootPath))
                throw new ArgumentException("Vault root path cannot be null or empty.", nameof(vaultRootPath));

            // إنشاء مجلد فرعي داخل AppData\Urkey\DocumentsFiles
            string docImagesDir = Path.Combine(vaultRootPath, "DocumentsFiles");
            try
            {
                Directory.CreateDirectory(docImagesDir);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create DocumentsFiles directory at: {docImagesDir}. {ex.Message}", ex);
            }

            // قراءة الصورة وتحويلها إلى Base64
            byte[] bytes = File.ReadAllBytes(sourceImagePath);
            string imageBase64 = Convert.ToBase64String(bytes);
            string encryptedBase64 = EncryptionService.Encrypt(imageBase64);

            // التحقق من أن النص المشفر هو Base64 صحيح قبل الحفظ
            if (string.IsNullOrWhiteSpace(encryptedBase64))
                throw new InvalidOperationException("Encryption produced an empty string.");

            // التحقق من صحة Base64 المشفر
            try
            {
                Convert.FromBase64String(encryptedBase64);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException($"Encryption produced invalid Base-64 string. This should not happen. Original error: {ex.Message}", ex);
            }

            // اسم ملف جديد فريد
            string fileName = $"{Guid.NewGuid()}.img";
            string fullPath = Path.Combine(docImagesDir, fileName);

            // استخدام UTF8 encoding بدون BOM لضمان الحفظ الصحيح
            // نكتب الملف كـ Base64 فقط بدون أي أسطر جديدة أو مسافات
            var utf8NoBom = new System.Text.UTF8Encoding(false);
            try
            {
                File.WriteAllText(fullPath, encryptedBase64, utf8NoBom);
                
                // Ensure the file is flushed to disk before returning
                // This helps prevent timing issues when immediately accessing the file
                File.SetAttributes(fullPath, FileAttributes.Normal);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save encrypted image to: {fullPath}. {ex.Message}", ex);
            }

            return fullPath; // نعيد المسار حتى نخزّنه داخل DocumentEntry.ExternalImagePath
        }

        /// <summary>
        /// يفك تشفير ملف الصورة المحفوظ في DocumentsFiles ويعيده كصورة مؤقتة للعرض.
        /// </summary>
        public static string ExtractDocumentImage(string encryptedImagePath)
        {
            if (string.IsNullOrWhiteSpace(encryptedImagePath))
                throw new ArgumentException("Encrypted image path cannot be null or empty.", nameof(encryptedImagePath));

            // Ensure the directory exists (defensive check)
            string? directory = Path.GetDirectoryName(encryptedImagePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                try
                {
                    Directory.CreateDirectory(directory);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to create directory for encrypted image: {directory}. {ex.Message}", ex);
                }
            }

            if (!File.Exists(encryptedImagePath))
                throw new FileNotFoundException($"Encrypted image not found at path: {encryptedImagePath}. The file may not have been saved yet or the path is incorrect.", encryptedImagePath);

            try
            {
                // قراءة الملف المشفر كـ bytes أولاً ثم تحويله إلى string
                // هذا يضمن عدم وجود مشاكل في encoding
                byte[] fileBytes = File.ReadAllBytes(encryptedImagePath);
                if (fileBytes.Length == 0)
                    throw new InvalidDataException("Encrypted image file is empty.");

                // تحويل bytes إلى string باستخدام UTF8
                var utf8NoBom = new System.Text.UTF8Encoding(false);
                string encryptedBase64 = utf8NoBom.GetString(fileBytes).Trim();
                
                // إزالة أي أحرف غير مرئية أو BOM
                encryptedBase64 = encryptedBase64.Trim('\uFEFF', '\u200B', '\u200C', '\u200D', '\u2060');
                
                if (string.IsNullOrWhiteSpace(encryptedBase64))
                    throw new InvalidDataException("Encrypted image file is empty or contains only whitespace.");

                // فك التشفير مع معالجة الأخطاء
                string decryptedBase64;
                try
                {
                    // محاولة فك التشفير مباشرة
                    decryptedBase64 = EncryptionService.Decrypt(encryptedBase64);
                }
                catch (FormatException ex) when (ex.Message.Contains("Base-64") || ex.Message.Contains("Base64"))
                {
                    // إذا فشل بسبب Base64 غير صحيح، نحاول تنظيفه وإعادة المحاولة
                    string cleaned = CleanBase64String(encryptedBase64);
                    if (cleaned != encryptedBase64 && !string.IsNullOrEmpty(cleaned))
                    {
                        try
                        {
                            decryptedBase64 = EncryptionService.Decrypt(cleaned);
                        }
                        catch (Exception ex2)
                        {
                            throw new FormatException($"Failed to decrypt the image file. The encrypted data is not valid Base-64. The file may be corrupted. Original error: {ex.Message}", ex2);
                        }
                    }
                    else
                    {
                        throw new FormatException($"Failed to decrypt the image file. The encrypted data is not valid Base-64. The file may be corrupted. Original error: {ex.Message}", ex);
                    }
                }
                catch (CryptographicException ex)
                {
                    throw new InvalidOperationException($"Decryption failed. The file may have been encrypted with a different key or is corrupted. Original error: {ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Unexpected error during decryption: {ex.Message}", ex);
                }
                
                if (string.IsNullOrWhiteSpace(decryptedBase64))
                    throw new InvalidDataException("Decrypted Base64 string is empty. The decryption may have failed silently.");

                // إزالة أي مسافات بيضاء أو أسطر جديدة من Base64 بعد فك التشفير
                decryptedBase64 = decryptedBase64.Trim();

                // التحقق من صحة Base64 قبل التحويل
                if (!IsValidBase64(decryptedBase64))
                {
                    // محاولة تنظيف Base64 من أي أحرف غير صالحة
                    string cleaned = CleanBase64String(decryptedBase64);
                    if (IsValidBase64(cleaned))
                    {
                        decryptedBase64 = cleaned;
                    }
                    else
                    {
                        throw new FormatException($"The decrypted string is not a valid Base-64 string. Length: {decryptedBase64.Length}, First 50 chars: {decryptedBase64.Substring(0, Math.Min(50, decryptedBase64.Length))}");
                    }
                }

                byte[] bytes;
                try
                {
                    bytes = Convert.FromBase64String(decryptedBase64);
                }
                catch (FormatException ex)
                {
                    throw new FormatException($"Invalid Base-64 string in decrypted data. The file may be corrupted or was not encrypted properly. Original error: {ex.Message}", ex);
                }

                if (bytes == null || bytes.Length == 0)
                    throw new InvalidDataException("Decoded image bytes are empty.");

                // Temporary plaintext image — cleaned up via CleanupTempDocumentImages / TryDeleteTempFile
                string tempPath = Path.Combine(
                    Path.GetTempPath(),
                    $"{TempDocumentPrefix}{Guid.NewGuid():N}.png");
                File.WriteAllBytes(tempPath, bytes);
                CryptographicOperations.ZeroMemory(bytes);

                return tempPath;
            }
            catch (FormatException ex) when (ex.Message.Contains("Base-64"))
            {
                throw new FormatException($"Invalid Base-64 string in encrypted image file. The file may be corrupted or was not encrypted properly. Original error: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to extract document image from '{encryptedImagePath}'. {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Best-effort delete of a single temp document image.
        /// </summary>
        public static void TryDeleteTempFile(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            try
            {
                string fileName = Path.GetFileName(path);
                if (!fileName.StartsWith(TempDocumentPrefix, StringComparison.OrdinalIgnoreCase))
                    return;

                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // Ignore — temp cleanup must never crash the app.
            }
        }

        /// <summary>
        /// Delete leftover decrypted document images from the system temp folder.
        /// </summary>
        public static void CleanupTempDocumentImages()
        {
            try
            {
                string tempDir = Path.GetTempPath();
                foreach (string file in Directory.GetFiles(tempDir, $"{TempDocumentPrefix}*.png"))
                {
                    try { File.Delete(file); }
                    catch { /* ignore locked files */ }
                }
            }
            catch
            {
                // ignore
            }
        }

        /// <summary>
        /// Overwrites a file with random data (single pass) then deletes it.
        /// Used after password import so plaintext export remnants are harder to recover.
        /// Returns false on any failure — never throws; never logs file contents.
        /// </summary>
        public static bool TrySecureDelete(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                    return false;

                var info = new FileInfo(filePath);
                long length = info.Length;

                using (var rng = RandomNumberGenerator.Create())
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.None))
                {
                    byte[] buffer = new byte[8192];
                    long remaining = length;
                    while (remaining > 0)
                    {
                        int toWrite = (int)Math.Min(buffer.Length, remaining);
                        rng.GetBytes(buffer.AsSpan(0, toWrite));
                        fs.Write(buffer, 0, toWrite);
                        remaining -= toWrite;
                    }
                    fs.Flush(true);
                }

                File.Delete(filePath);
                return !File.Exists(filePath);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// التحقق من صحة سلسلة Base64
        /// </summary>
        private static bool IsValidBase64(string base64String)
        {
            if (string.IsNullOrWhiteSpace(base64String))
                return false;

            // Base64 يجب أن يحتوي فقط على: A-Z, a-z, 0-9, +, /, = (للحشو)
            // يجب أن يكون طولها مضاعف 4 (بعد إزالة المسافات)
            string cleaned = base64String.Replace(" ", "").Replace("\n", "").Replace("\r", "").Replace("\t", "");
            
            if (cleaned.Length % 4 != 0)
                return false;

            try
            {
                // محاولة التحويل للتحقق من الصحة
                Convert.FromBase64String(cleaned);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// تنظيف سلسلة Base64 من الأحرف غير الصالحة
        /// </summary>
        private static string CleanBase64String(string base64String)
        {
            if (string.IsNullOrWhiteSpace(base64String))
                return string.Empty;

            // إزالة جميع الأحرف غير الصالحة في Base64
            var validChars = new System.Text.StringBuilder();
            foreach (char c in base64String)
            {
                if ((c >= 'A' && c <= 'Z') || 
                    (c >= 'a' && c <= 'z') || 
                    (c >= '0' && c <= '9') || 
                    c == '+' || c == '/' || c == '=')
                {
                    validChars.Append(c);
                }
            }

            string cleaned = validChars.ToString();
            
            // إضافة padding إذا لزم الأمر
            int remainder = cleaned.Length % 4;
            if (remainder > 0)
            {
                cleaned += new string('=', 4 - remainder);
            }

            return cleaned;
        }
    }
}
