using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PasswordManager.Core.Models
{
     
    public sealed class CardEntry : VaultEntry
    {
        public string HolderName { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public DateOnly ExpiryDate { get; set; } // MM/YY

        public string? Cvv { get; set; }

        public string Bank { get; set; } = string.Empty;

        public string Notes {  get; set; } = string.Empty;
    }

}
