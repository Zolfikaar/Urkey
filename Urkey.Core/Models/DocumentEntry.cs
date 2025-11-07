namespace Urkey.Core.Models
{
    public sealed class DocumentEntry : VaultEntry
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // e.g., ID, Passport, License
        public string Number { get; set; } = string.Empty;
        public string Issuer {  get; set; } = string.Empty;
        public DateOnly? ExpiryDate { get; set; }
        public string? Notes { get; set; }

        // اسم الملف أو وصفه
        public string? FileName { get; set; }

        // محتوى الصورة المشفر Base64 (نخزنه داخل JSON أو بملف خارجي مرتبط)
        public string? FileContentBase64 { get; set; }


        // لفصل الصور في مجلد منفصل لاحقاً
        /*
         الغاية:
        تحفظ الصور بشكل مستقل في مجلد DocumentsFiles/ داخل مجلد البرنامج.

وتخزن فقط المسار النسبي في Vault.

لما يعمل التشفير العام، VaultRepository يشمل الميتاداتا فقط، أما الصور تنحفظ مشفرة بنفس EncryptionService لكن بملف مستقل.
         */
        public string? ExternalImagePath { get; set; }

    }
}
