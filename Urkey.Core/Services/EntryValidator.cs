using System.Globalization;
using System.Net.Mail;
using System.Text.RegularExpressions;
using Urkey.Core.Models;

namespace Urkey.Core.Services
{
    public sealed class EntryValidationResult
    {
        public bool IsValid => ErrorResourceKey == null;
        public string? ErrorResourceKey { get; init; }

        public static EntryValidationResult Ok() => new();
        public static EntryValidationResult Fail(string key) => new() { ErrorResourceKey = key };
    }

    public static class EntryValidator
    {
        public static EntryValidationResult Validate(VaultEntry entry) => entry switch
        {
            AccountEntry a => ValidateAccount(a),
            CardEntry c => ValidateCard(c),
            AddressEntry a => ValidateAddress(a),
            NoteEntry n => ValidateNote(n),
            DocumentEntry d => ValidateDocument(d),
            _ => EntryValidationResult.Fail("Validation_UnknownEntryType")
        };

        public static EntryValidationResult ValidateAccount(AccountEntry entry)
        {
            if (string.IsNullOrWhiteSpace(entry.ServiceName))
                return EntryValidationResult.Fail("Validation_AccountNameRequired");

            if (string.IsNullOrWhiteSpace(entry.Password) && string.IsNullOrWhiteSpace(entry.Username) && string.IsNullOrWhiteSpace(entry.Email))
                return EntryValidationResult.Fail("Validation_AccountCredentialsRequired");

            if (!string.IsNullOrWhiteSpace(entry.Email) && !IsValidEmail(entry.Email))
                return EntryValidationResult.Fail("Validation_InvalidEmail");

            return EntryValidationResult.Ok();
        }

        public static EntryValidationResult ValidateCard(CardEntry entry)
        {
            if (string.IsNullOrWhiteSpace(entry.HolderName))
                return EntryValidationResult.Fail("Validation_CardNameRequired");

            var digits = DigitsOnly(entry.Number);
            if (digits.Length is < 13 or > 19)
                return EntryValidationResult.Fail("Validation_InvalidCardNumber");

            if (!PassesLuhn(digits))
                return EntryValidationResult.Fail("Validation_InvalidCardNumber");

            if (!string.IsNullOrWhiteSpace(entry.Cvv))
            {
                var cvv = DigitsOnly(entry.Cvv);
                if (cvv.Length is < 3 or > 4)
                    return EntryValidationResult.Fail("Validation_InvalidCvv");
            }

            if (!string.IsNullOrWhiteSpace(entry.Pin))
            {
                var pin = DigitsOnly(entry.Pin);
                if (pin.Length is < 4 or > 8)
                    return EntryValidationResult.Fail("Validation_InvalidPin");
            }

            return EntryValidationResult.Ok();
        }

        public static EntryValidationResult ValidateAddress(AddressEntry entry)
        {
            if (string.IsNullOrWhiteSpace(entry.Name))
                return EntryValidationResult.Fail("Validation_AddressNameRequired");

            if (string.IsNullOrWhiteSpace(entry.Street) && string.IsNullOrWhiteSpace(entry.City))
                return EntryValidationResult.Fail("Validation_AddressLocationRequired");

            return EntryValidationResult.Ok();
        }

        public static EntryValidationResult ValidateNote(NoteEntry entry)
        {
            if (string.IsNullOrWhiteSpace(entry.Title))
                return EntryValidationResult.Fail("Validation_NoteTitleRequired");

            return EntryValidationResult.Ok();
        }

        public static EntryValidationResult ValidateDocument(DocumentEntry entry)
        {
            if (string.IsNullOrWhiteSpace(entry.Name))
                return EntryValidationResult.Fail("Validation_DocumentNameRequired");

            return EntryValidationResult.Ok();
        }

        public static bool TryParseCardExpiry(string? text, out DateOnly expiry)
        {
            expiry = default;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            text = text.Trim();

            // MM/YY or MM/YYYY
            var m = Regex.Match(text, @"^(?<mm>0?[1-9]|1[0-2])\s*/\s*(?<yy>\d{2}|\d{4})$");
            if (m.Success)
            {
                int month = int.Parse(m.Groups["mm"].Value, CultureInfo.InvariantCulture);
                int year = int.Parse(m.Groups["yy"].Value, CultureInfo.InvariantCulture);
                if (year < 100)
                    year += 2000;
                int day = DateTime.DaysInMonth(year, month);
                expiry = new DateOnly(year, month, day);
                return true;
            }

            return DateOnly.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out expiry)
                   || DateOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out expiry);
        }

        public static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private static string DigitsOnly(string? value)
            => value == null ? string.Empty : new string(value.Where(char.IsDigit).ToArray());

        private static bool PassesLuhn(string digits)
        {
            int sum = 0;
            bool alternate = false;
            for (int i = digits.Length - 1; i >= 0; i--)
            {
                int n = digits[i] - '0';
                if (alternate)
                {
                    n *= 2;
                    if (n > 9) n -= 9;
                }
                sum += n;
                alternate = !alternate;
            }
            return sum % 10 == 0;
        }
    }
}
