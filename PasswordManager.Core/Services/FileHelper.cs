using System;
using System.IO;

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
            string encryptedBase64 = EncryptionService.Encrypt(Convert.ToBase64String(bytes));

            // اسم ملف جديد فريد
            string fileName = $"{Guid.NewGuid()}.img";
            string fullPath = Path.Combine(docImagesDir, fileName);

            File.WriteAllText(fullPath, encryptedBase64);

            return fullPath; // نعيد المسار حتى نخزّنه داخل DocumentEntry.ExternalImagePath
        }

        /// <summary>
        /// يفك تشفير ملف الصورة المحفوظ في DocumentsFiles ويعيده كصورة مؤقتة للعرض.
        /// </summary>
        public static string ExtractDocumentImage(string encryptedImagePath)
        {
            if (!File.Exists(encryptedImagePath))
                throw new FileNotFoundException("Encrypted image not found.", encryptedImagePath);

            string encryptedBase64 = File.ReadAllText(encryptedImagePath);
            string decryptedBase64 = EncryptionService.Decrypt(encryptedBase64);
            byte[] bytes = Convert.FromBase64String(decryptedBase64);

            // نحفظ الصورة مؤقتًا في مجلد Temp
            string tempPath = Path.Combine(Path.GetTempPath(), $"{Path.GetFileNameWithoutExtension(encryptedImagePath)}.png");
            File.WriteAllBytes(tempPath, bytes);

            return tempPath;
        }
    }
}
