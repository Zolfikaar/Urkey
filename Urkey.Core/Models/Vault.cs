namespace Urkey.Core.Models
{
    public class Vault
    {
        public List<VaultEntry> Entries { get; set; } = new();
        public string Title { get; set; } = "Urkey Vault";
        public List<ActivityLogEntry> ActivityLog { get; set; } = new();
    }
}
