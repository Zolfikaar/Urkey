using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PasswordManager.Core.Models
{
    public class Vault
    {
        public List<Credential> Credentials { get; set; } = new List<Credential>();
    }
}
