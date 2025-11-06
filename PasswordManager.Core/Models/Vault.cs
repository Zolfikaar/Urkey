using System.Collections.Generic;

namespace PasswordManager.Core.Models
{
    public class Vault
    {
        public List<VaultEntry> Entries { get; set; } = new List<VaultEntry>();
    }
}
