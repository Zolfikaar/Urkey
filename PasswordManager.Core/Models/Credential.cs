using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PasswordManager.Core.Models
{
    public class Credential
    {
        public string ServiceName { get; set; } = string.Empty; // e.g., "Google", "Facebook"
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // will encrypt later
    }
}
