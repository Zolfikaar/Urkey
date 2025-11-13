namespace Urkey.Core.Models
{
    public sealed class AccountEntry : VaultEntry
    {
        public string ServiceName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? ApplicationPath { get; set; }
        public string Notes {  get; set; } = string.Empty;

        // ✅ جديدة: نوع الحساب (Website / Application / Other)
        public string AccountType { get; set; } = "Website";
        public string? LicenseKey { get; set; }
        public string? Category { get; set; }
        public bool IsFavorite { get; set; }

    }

}
