namespace PasswordManager.Core.Models
{
    public sealed class AccountEntry : VaultEntry
    {
        public string ServiceName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Url { get; set; } 
        public string Notes {  get; set; } = string.Empty;

    }

}
