using System;

namespace Urkey.Core.Models
{
    public sealed class CardEntry : VaultEntry
    {
        public string HolderName { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public DateOnly ExpiryDate { get; set; } // MM/YY

        public string? Cvv { get; set; }

        /// <summary>Optional ATM / card PIN.</summary>
        public string? Pin { get; set; }

        /// <summary>Deprecated — kept for vault JSON compatibility. Prefer <see cref="Pin"/>.</summary>
        public string Bank { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;
    }
}
