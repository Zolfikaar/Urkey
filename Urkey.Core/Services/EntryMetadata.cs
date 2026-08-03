using Urkey.Core.Models;

namespace Urkey.Core.Services
{
    public static class EntryMetadata
    {
        public const string TypeAccount = "account";
        public const string TypeCard = "card";
        public const string TypeAddress = "address";
        public const string TypeNote = "note";
        public const string TypeDocument = "document";

        public const string ActionAdded = "added";
        public const string ActionEdited = "edited";
        public const string ActionDeleted = "deleted";
        public const string ActionUploaded = "uploaded";
        public const string ActionImported = "imported";

        public static string GetTypeKey(VaultEntry entry) => entry switch
        {
            AccountEntry => TypeAccount,
            CardEntry => TypeCard,
            AddressEntry => TypeAddress,
            NoteEntry => TypeNote,
            DocumentEntry => TypeDocument,
            _ => "unknown"
        };

        public static string GetDisplayName(VaultEntry entry) => entry switch
        {
            AccountEntry a => FirstNonEmpty(a.ServiceName, a.Username, a.Email, "Account"),
            CardEntry c => FirstNonEmpty(c.HolderName, MaskCard(c.Number), "Card"),
            AddressEntry a => FirstNonEmpty(a.Name, a.City, "Address"),
            NoteEntry n => FirstNonEmpty(n.Title, "Note"),
            DocumentEntry d => FirstNonEmpty(d.Name, d.FileName, "Document"),
            _ => "Entry"
        };

        public static string GetSecondaryText(VaultEntry entry) => entry switch
        {
            AccountEntry a => FirstNonEmpty(a.Username, a.Email, a.Url, a.ApplicationPath),
            CardEntry c => MaskCard(c.Number),
            AddressEntry a => string.Join(", ", new[] { a.Street, a.City, a.Country }.Where(s => !string.IsNullOrWhiteSpace(s))),
            NoteEntry n => Truncate(n.Content, 80),
            DocumentEntry d => FirstNonEmpty(d.Type, d.Number, d.Issuer),
            _ => string.Empty
        };

        public static string MaskCard(string? number)
        {
            if (string.IsNullOrWhiteSpace(number))
                return string.Empty;

            var digits = new string(number.Where(char.IsDigit).ToArray());
            if (digits.Length < 4)
                return "****";

            return "**** **** **** " + digits[^4..];
        }

        private static string FirstNonEmpty(params string?[] values)
            => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim() ?? string.Empty;

        private static string Truncate(string? text, int max)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;
            text = text.Trim();
            return text.Length <= max ? text : text[..max] + "…";
        }
    }
}
