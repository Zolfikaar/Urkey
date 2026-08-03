using System.Text.RegularExpressions;

namespace Urkey.Core.Services
{
    public enum PasswordStrengthLevel
    {
        Empty = 0,
        Weak = 1,
        Medium = 2,
        Strong = 3
    }

    public sealed class PasswordAnalysis
    {
        public PasswordStrengthLevel Level { get; init; }
        public double EntropyBits { get; init; }
        public IReadOnlyList<string> IssueKeys { get; init; } = Array.Empty<string>();
        public bool HasLower { get; init; }
        public bool HasUpper { get; init; }
        public bool HasDigit { get; init; }
        public bool HasSymbol { get; init; }
        public int Length { get; init; }
    }

    public static class PasswordStrengthEvaluator
    {
        private static readonly string[] CommonPasswords =
        {
            "password", "password1", "123456", "12345678", "123456789", "qwerty",
            "abc123", "admin", "welcome", "letmein", "monkey", "dragon", "master",
            "login", "princess", "football", "iloveyou", "welcome1", "passw0rd"
        };

        public static PasswordStrengthLevel Evaluate(string? password)
            => Analyze(password).Level;

        public static PasswordAnalysis Analyze(string? password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return new PasswordAnalysis
                {
                    Level = PasswordStrengthLevel.Empty,
                    EntropyBits = 0,
                    IssueKeys = new[] { "PasswordCheck_Issue_Empty" }
                };
            }

            bool hasLower = Regex.IsMatch(password, "[a-z]");
            bool hasUpper = Regex.IsMatch(password, "[A-Z]");
            bool hasDigit = Regex.IsMatch(password, @"\d");
            bool hasSymbol = Regex.IsMatch(password, @"[^a-zA-Z0-9]");

            int pool = 0;
            if (hasLower) pool += 26;
            if (hasUpper) pool += 26;
            if (hasDigit) pool += 10;
            if (hasSymbol) pool += 33;
            if (pool == 0) pool = 1;

            double entropy = password.Length * Math.Log2(pool);

            var issues = new List<string>();
            if (password.Length < 8)
                issues.Add("PasswordCheck_Issue_TooShort");
            else if (password.Length < 12)
                issues.Add("PasswordCheck_Issue_Short");

            if (!(hasLower && hasUpper))
                issues.Add("PasswordCheck_Issue_Case");
            if (!hasDigit)
                issues.Add("PasswordCheck_Issue_Digit");
            if (!hasSymbol)
                issues.Add("PasswordCheck_Issue_Symbol");

            if (Regex.IsMatch(password, @"(.)\1{2,}"))
                issues.Add("PasswordCheck_Issue_Repeated");

            if (Regex.IsMatch(password, @"(0123|1234|2345|3456|4567|5678|6789|7890|abcd|bcde|cdef|qwer|asdf)", RegexOptions.IgnoreCase))
                issues.Add("PasswordCheck_Issue_Sequence");

            string lower = password.ToLowerInvariant();
            if (CommonPasswords.Any(c => lower == c || lower.StartsWith(c, StringComparison.Ordinal)))
                issues.Add("PasswordCheck_Issue_Common");

            PasswordStrengthLevel level;
            if (issues.Contains("PasswordCheck_Issue_Common") || password.Length < 8)
                level = PasswordStrengthLevel.Weak;
            else if (entropy >= 60 && password.Length >= 12 && hasLower && hasUpper && hasDigit && hasSymbol && issues.Count <= 1)
                level = PasswordStrengthLevel.Strong;
            else if (entropy >= 40 && password.Length >= 8 && issues.Count <= 2)
                level = PasswordStrengthLevel.Medium;
            else
                level = PasswordStrengthLevel.Weak;

            return new PasswordAnalysis
            {
                Level = level,
                EntropyBits = Math.Round(entropy, 1),
                IssueKeys = issues,
                HasLower = hasLower,
                HasUpper = hasUpper,
                HasDigit = hasDigit,
                HasSymbol = hasSymbol,
                Length = password.Length
            };
        }

        public static double ToScoreUnit(PasswordStrengthLevel level) => level switch
        {
            PasswordStrengthLevel.Strong => 1.0,
            PasswordStrengthLevel.Medium => 0.55,
            PasswordStrengthLevel.Weak => 0.15,
            _ => 0.0
        };
    }
}
