using Urkey.Core.Models;

namespace Urkey.Core.Services;

public sealed record PasswordHealthStats(
    int Total,
    int Strong,
    int Medium,
    int Weak,
    int Duplicate,
    int Compromised);

public static class PasswordHealthAnalyzer
{
    public static PasswordHealthStats Analyze(IEnumerable<VaultEntry> entries)
    {
        var accounts = entries
            .OfType<AccountEntry>()
            .Where(a => !string.IsNullOrEmpty(a.Password))
            .ToList();

        if (accounts.Count == 0)
            return new PasswordHealthStats(0, 0, 0, 0, 0, 0);

        var reusedIds = accounts
            .GroupBy(a => a.Password, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g)
            .Select(a => a.Id)
            .ToHashSet();

        int strong = 0, medium = 0, weak = 0, compromised = 0;
        foreach (var account in accounts)
        {
            var analysis = PasswordStrengthEvaluator.Analyze(account.Password);
            switch (analysis.Level)
            {
                case PasswordStrengthLevel.Strong:
                    strong++;
                    break;
                case PasswordStrengthLevel.Medium:
                    medium++;
                    break;
                default:
                    weak++;
                    break;
            }

            if (analysis.IssueKeys.Contains("PasswordCheck_Issue_Common"))
                compromised++;
        }

        return new PasswordHealthStats(
            accounts.Count,
            strong,
            medium,
            weak,
            reusedIds.Count,
            compromised);
    }
}
