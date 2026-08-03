using Urkey.Core.Models;

namespace Urkey.Core.Services
{
    /// <summary>
    /// Security score formula (Phase 2 default):
    /// - Consider only account entries that have a non-empty password.
    /// - Each password starts with a strength unit: Strong=1.0, Medium=0.55, Weak=0.15.
    /// - If the same password is reused across 2+ accounts, those entries are capped at 0.25.
    /// - Final score = round(average(unit) * 100). If there are no passwords, score = 100.
    /// </summary>
    public static class SecurityScoreCalculator
    {
        public static int Calculate(IEnumerable<VaultEntry> entries)
        {
            var passwords = entries
                .OfType<AccountEntry>()
                .Select(a => a.Password)
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList();

            if (passwords.Count == 0)
                return 100;

            var reuseCounts = passwords
                .GroupBy(p => p, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

            double total = 0;
            foreach (var password in passwords)
            {
                double unit = PasswordStrengthEvaluator.ToScoreUnit(
                    PasswordStrengthEvaluator.Evaluate(password));

                if (reuseCounts.TryGetValue(password, out int count) && count > 1)
                    unit = Math.Min(unit, 0.25);

                total += unit;
            }

            return (int)Math.Round(total / passwords.Count * 100.0, MidpointRounding.AwayFromZero);
        }

        public static int CountPasswords(IEnumerable<VaultEntry> entries)
            => entries.OfType<AccountEntry>().Count(a => !string.IsNullOrEmpty(a.Password));
    }
}
