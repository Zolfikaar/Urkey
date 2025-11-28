using System.Collections.Generic;

namespace Urkey.Core.Models
{
    public class Vault
    {
        public List<VaultEntry> Entries { get; set; } = new List<VaultEntry>();
        public string Password { get; set; } = string.Empty;
    }
}
