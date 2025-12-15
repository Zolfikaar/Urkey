using System.Collections.Generic;

namespace Urkey.Core.Models
{
    public class Vault
    {
        public List<VaultEntry> Entries { get; set; } = new List<VaultEntry>();
        
        // public Guid Id { get; set; } = Guid.NewGuid();
        // public string Title { get; set; } = string.Empty;
        
        public  bool IsLocked { get; set; } = true;
    }
}
