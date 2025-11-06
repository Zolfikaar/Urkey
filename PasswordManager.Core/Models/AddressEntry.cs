namespace PasswordManager.Core.Models
{
    public sealed class AddressEntry : VaultEntry
    {
        public string Name { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Governorate {  get; set; } = string.Empty;
        public string? ZipCode { get; set; }
        public string Country { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

    }
}
