using System.IO;
using System.Text.RegularExpressions;

namespace Urkey.Core.Services.Import;

/// <summary>
/// Parses Kaspersky Password Manager .txt exports.
/// Format is key-value blocks separated by "---", not CSV columns:
///   Application: Name
///   Login: user
///   Password: secret
///   Comment:
///   ---
/// </summary>
public static class KasperskyTextParser
{
    private static readonly HashSet<string> SectionHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Applications",
        "Application",
        "Websites",
        "Website",
        "Other Accounts",
        "Other Account",
        "Notes",
        "Note"
    };

    private static readonly HashSet<string> KnownKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Application",
        "Website name",
        "Website URL",
        "Account name",
        "Login name",
        "Login",
        "Password",
        "Comment",
        "Name",
        "URL",
        "Web site",
        "Website"
    };

    private static readonly Regex FieldLineRegex = new(
        @"^(?<key>[^:\r\n]{1,80}):\s*(?<value>.*)$",
        RegexOptions.Compiled);

    public static bool LooksLikeFormat(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        int fieldHits = 0;
        int separators = 0;
        int sectionHits = 0;

        using var reader = new StringReader(text);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0)
                continue;

            if (trimmed is "---" or "–" or "—")
            {
                separators++;
                continue;
            }

            if (SectionHeaders.Contains(trimmed))
            {
                sectionHits++;
                continue;
            }

            if (TryParseField(trimmed, out var key, out _) && IsKnownKey(key))
                fieldHits++;

            if (fieldHits >= 4 && (separators >= 1 || sectionHits >= 1))
                return true;
        }

        // Smaller exports: still accept if we saw several Kaspersky field labels.
        return fieldHits >= 3 && separators >= 1;
    }

    public static List<ImportCandidate> Parse(string text)
    {
        var candidates = new List<ImportCandidate>();
        if (string.IsNullOrWhiteSpace(text))
            return candidates;

        string currentSection = "Website";
        var block = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        int blockStartLine = 1;
        int lineNumber = 0;

        void FlushBlock()
        {
            if (block.Count == 0)
                return;

            var candidate = ToCandidate(block, currentSection, blockStartLine);
            if (candidate != null)
                candidates.Add(candidate);

            block.Clear();
        }

        using var reader = new StringReader(text);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            lineNumber++;
            var trimmed = line.TrimEnd('\r');
            var content = trimmed.Trim();

            if (content.Length == 0)
                continue;

            if (content is "---" or "–" or "—")
            {
                FlushBlock();
                blockStartLine = lineNumber + 1;
                continue;
            }

            if (SectionHeaders.Contains(content) && !content.Contains(':', StringComparison.Ordinal))
            {
                FlushBlock();
                currentSection = NormalizeSection(content);
                blockStartLine = lineNumber + 1;
                continue;
            }

            if (!TryParseField(trimmed, out var key, out var value))
                continue;

            if (!IsKnownKey(key))
                continue;

            if (block.Count == 0)
                blockStartLine = lineNumber;

            // Keep first non-empty value; allow overwriting empty.
            if (!block.TryGetValue(key, out var existing) || string.IsNullOrWhiteSpace(existing))
                block[key] = value;
        }

        FlushBlock();

        if (candidates.Count == 0)
            throw new InvalidDataException("Import_Error_UnrecognizedFormat");

        return candidates;
    }

    private static ImportCandidate? ToCandidate(
        Dictionary<string, string> block,
        string section,
        int sourceLine)
    {
        string name = FirstValue(block, "Application", "Website name", "Account name", "Name", "Website");
        string url = FirstValue(block, "Website URL", "URL", "Web site");
        string loginName = FirstValue(block, "Login name");
        string login = FirstValue(block, "Login");
        string password = FirstValue(block, "Password");
        string comment = FirstValue(block, "Comment");

        string username = !string.IsNullOrWhiteSpace(login) ? login : loginName;
        string email = string.Empty;
        if (username.Contains('@', StringComparison.Ordinal))
            email = username;

        if (string.IsNullOrWhiteSpace(name))
        {
            if (!string.IsNullOrWhiteSpace(url))
                name = InferHost(url);
            else if (!string.IsNullOrWhiteSpace(username))
                name = username;
        }

        // Skip completely empty blocks.
        if (string.IsNullOrWhiteSpace(name)
            && string.IsNullOrWhiteSpace(username)
            && string.IsNullOrWhiteSpace(password)
            && string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        string accountType = section switch
        {
            "Application" => "Application",
            "Other" => "Other",
            _ => "Website"
        };

        var candidate = new ImportCandidate
        {
            SourceRowNumber = sourceLine,
            ServiceName = name.Trim(),
            Url = url.Trim(),
            Username = username.Trim(),
            Email = email.Trim(),
            Password = password, // preserve as exported (may include leading spaces intentionally — trim ends only)
            Notes = comment.Trim()
        };
        candidate.Password = password.TrimEnd('\r', '\n');

        // Stash account type in Notes prefix? Better: use a parallel approach via ServiceName only.
        // AccountEntry.AccountType is set in ToAccountEntry — extend ImportCandidate.
        candidate.AccountType = accountType;

        if (string.IsNullOrWhiteSpace(candidate.ServiceName)
            && string.IsNullOrWhiteSpace(candidate.Username)
            && string.IsNullOrWhiteSpace(candidate.Email)
            && string.IsNullOrWhiteSpace(candidate.Password))
        {
            candidate.ErrorResourceKey = "Import_FailReason_EmptyRow";
        }
        else
        {
            var entry = PasswordImportService.ToAccountEntry(candidate);
            var validation = EntryValidator.ValidateAccount(entry);
            if (!validation.IsValid)
                candidate.ErrorResourceKey = validation.ErrorResourceKey ?? "Import_FailReason_Validation";
        }

        return candidate;
    }

    private static string InferHost(string url)
    {
        try
        {
            var uriText = url.Contains("://", StringComparison.Ordinal) ? url : "https://" + url;
            if (Uri.TryCreate(uriText, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Host))
            {
                var host = uri.Host;
                if (host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
                    host = host[4..];
                return host;
            }
        }
        catch
        {
            // ignore
        }
        return url.Trim();
    }

    private static string FirstValue(Dictionary<string, string> block, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (block.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                return value;
            // also try exact key match for empty-allowed later
        }

        foreach (var key in keys)
        {
            if (block.TryGetValue(key, out var value))
                return value ?? string.Empty;
        }

        return string.Empty;
    }

    private static string NormalizeSection(string header) => header.Trim().ToLowerInvariant() switch
    {
        "applications" or "application" => "Application",
        "other accounts" or "other account" => "Other",
        "notes" or "note" => "Other",
        _ => "Website"
    };

    private static bool IsKnownKey(string key)
    {
        if (KnownKeys.Contains(key))
            return true;

        // Tolerate slight label variants.
        var n = key.Trim().ToLowerInvariant();
        return n is "application" or "website name" or "website url" or "account name"
            or "login name" or "login" or "password" or "comment" or "name" or "url"
            or "web site" or "website";
    }

    private static bool TryParseField(string line, out string key, out string value)
    {
        key = string.Empty;
        value = string.Empty;

        var m = FieldLineRegex.Match(line.TrimStart());
        if (!m.Success)
            return false;

        key = m.Groups["key"].Value.Trim();
        value = m.Groups["value"].Success ? m.Groups["value"].Value : string.Empty;
        // Values may intentionally start with spaces; trim only trailing whitespace from export padding.
        value = value.TrimEnd();
        return key.Length > 0;
    }
}
