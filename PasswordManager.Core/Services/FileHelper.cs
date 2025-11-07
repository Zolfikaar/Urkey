using System;
using System.IO;
using System.Security.Cryptography;

namespace PasswordManager.Core.Services
{
    public static class FileHelper
    {
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

            // إنشاء مجلد فرعي داخل AppData\PasswordManager\DocumentsFiles
            string docImagesDir = Path.Combine(vaultRootPath, "DocumentsFiles");
            Directory.CreateDirectory(docImagesDir);

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
            File.WriteAllText(fullPath, encryptedBase64, utf8NoBom);

            return fullPath; // نعيد المسار حتى نخزّنه داخل DocumentEntry.ExternalImagePath
        }

        /// <summary>
        /// يفك تشفير ملف الصورة المحفوظ في DocumentsFiles ويعيده كصورة مؤقتة للعرض.
        /// </summary>
        public static string ExtractDocumentImage(string encryptedImagePath)
        {
            if (!File.Exists(encryptedImagePath))
                throw new FileNotFoundException("Encrypted image not found.", encryptedImagePath);

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

                // نحفظ الصورة مؤقتًا في مجلد Temp
                string tempPath = Path.Combine(Path.GetTempPath(), $"{Path.GetFileNameWithoutExtension(encryptedImagePath)}.png");
                File.WriteAllBytes(tempPath, bytes);

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
