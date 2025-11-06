namespace PasswordManager.Core.Models
{
    public sealed class NoteEntry : VaultEntry
    {
        public new string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        public string Category {  get; set; } = string.Empty;

        public string Tags {  get; set; } = string.Empty;
    }
}
