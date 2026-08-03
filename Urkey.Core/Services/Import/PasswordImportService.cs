using System.IO;
using System.Security.Cryptography;
using Urkey.Core.Models;

namespace Urkey.Core.Services.Import;

/// <summary>
/// Parses password-manager exports and maps rows to <see cref="AccountEntry"/> values.
/// Does not persist — callers must use <see cref="VaultService"/> so vault encryption is unchanged.
/// </summary>
public static class PasswordImportService
{
    private static readonly Dictionary<string, ImportColumnRole> HeaderAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        // Name / title
        ["name"] = ImportColumnRole.Name,
        ["title"] = ImportColumnRole.Name,
        ["service"] = ImportColumnRole.Name,
        ["account"] = ImportColumnRole.Name,
        ["account name"] = ImportColumnRole.Name,
        ["account_name"] = ImportColumnRole.Name,
        ["entry name"] = ImportColumnRole.Name,

        // URL
        ["url"] = ImportColumnRole.Url,
        ["website"] = ImportColumnRole.Url,
        ["web site"] = ImportColumnRole.Url,
        ["web_site"] = ImportColumnRole.Url,
        ["login_uri"] = ImportColumnRole.Url,
        ["login uri"] = ImportColumnRole.Url,
        ["uri"] = ImportColumnRole.Url,
        ["link"] = ImportColumnRole.Url,
        ["hostname"] = ImportColumnRole.Url,
        ["host"] = ImportColumnRole.Url,

        // Username
        ["username"] = ImportColumnRole.Username,
        ["user"] = ImportColumnRole.Username,
        ["user name"] = ImportColumnRole.Username,
        ["user_name"] = ImportColumnRole.Username,
        ["login"] = ImportColumnRole.Username,
        ["login name"] = ImportColumnRole.Username,
        ["login_name"] = ImportColumnRole.Username,
        ["account login"] = ImportColumnRole.Username,

        // Email
        ["email"] = ImportColumnRole.Email,
        ["e-mail"] = ImportColumnRole.Email,
        ["email address"] = ImportColumnRole.Email,
        ["mail"] = ImportColumnRole.Email,

        // Password
        ["password"] = ImportColumnRole.Password,
        ["pass"] = ImportColumnRole.Password,
        ["passwd"] = ImportColumnRole.Password,
        ["pwd"] = ImportColumnRole.Password,
        ["secret"] = ImportColumnRole.Password,

        // Notes
        ["note"] = ImportColumnRole.Notes,
        ["notes"] = ImportColumnRole.Notes,
        ["comment"] = ImportColumnRole.Notes,
        ["comments"] = ImportColumnRole.Notes,
        ["extra"] = ImportColumnRole.Notes,
        ["description"] = ImportColumnRole.Notes
    };

    /// <summary>
    /// Reads a file fully into memory, parses it, then clears the raw byte buffer.
    /// Detects Kaspersky-style key-value exports before falling back to CSV/TSV.
    /// Never copies the file to a temp location. Does not log file contents.
    /// </summary>
    public static ImportFileParseResult ReadImportFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            throw new FileNotFoundException("Import file not found.", filePath);

        byte[] bytes = File.ReadAllBytes(filePath);
        try
        {
            if (bytes.Length == 0)
                throw new InvalidDataException("Import_Error_EmptyFile");

            string text = DelimitedTextParser.DecodeBytes(bytes);

            if (KasperskyTextParser.LooksLikeFormat(text))
            {
                var structured = KasperskyTextParser.Parse(text);
                return new ImportFileParseResult { StructuredCandidates = structured };
            }

            return new ImportFileParseResult { Table = ParseText(text) };
        }
        finally
        {
            CryptographicOperations.ZeroMemory(bytes);
        }
    }

    /// <summary>Legacy helper — delimited files only. Prefer <see cref="ReadImportFile"/>.</summary>
    public static ParsedImportTable ReadAndParseFile(string filePath)
    {
        var result = ReadImportFile(filePath);
        if (result.Table != null)
            return result.Table;
        throw new InvalidDataException("Import_Error_UnrecognizedFormat");
    }

    public static ParsedImportTable ParseText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidDataException("Import_Error_EmptyFile");

        char delimiter = DetectDelimiterFromText(text);
        var allRows = DelimitedTextParser.Parse(text, delimiter);
        if (allRows.Count == 0)
            throw new InvalidDataException("Import_Error_EmptyFile");

        // First non-empty row is headers when it looks like headers; otherwise treat as data-only.
        var headerRow = allRows[0];
        bool looksLikeHeaders = LooksLikeHeaderRow(headerRow);
        IReadOnlyList<string> headers;
        IReadOnlyList<string[]> dataRows;

        if (looksLikeHeaders)
        {
            headers = headerRow.Select(h => (h ?? string.Empty).Trim()).ToArray();
            dataRows = allRows.Skip(1).ToList();
        }
        else
        {
            // Unrecognized headerless dump — expose generic Column 1..N for manual mapping.
            int colCount = allRows.Max(r => r.Length);
            if (colCount == 0)
                throw new InvalidDataException("Import_Error_UnrecognizedFormat");

            headers = Enumerable.Range(1, colCount)
                .Select(i => "Column " + i.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .ToArray();
            dataRows = allRows;
        }

        int emptyRows = 0;
        int malformed = 0;
        var normalized = new List<string[]>();
        int expectedCols = headers.Count;

        foreach (var row in dataRows)
        {
            if (row.All(string.IsNullOrWhiteSpace))
            {
                emptyRows++;
                continue;
            }

            if (row.Length < expectedCols)
            {
                var padded = new string[expectedCols];
                Array.Copy(row, padded, row.Length);
                for (int i = row.Length; i < expectedCols; i++)
                    padded[i] = string.Empty;
                normalized.Add(padded);
            }
            else if (row.Length > expectedCols)
            {
                malformed++;
                normalized.Add(row.Take(expectedCols).ToArray());
            }
            else
            {
                normalized.Add(row);
            }
        }

        if (normalized.Count == 0)
            throw new InvalidDataException("Import_Error_EmptyFile");

        var mapping = DetectColumnMapping(headers);
        return new ParsedImportTable
        {
            Headers = headers,
            Rows = normalized,
            Delimiter = delimiter,
            AutoMapping = mapping,
            EmptyRowCount = emptyRows,
            MalformedRowCount = malformed
        };
    }

    private static char DetectDelimiterFromText(string text)
    {
        using var reader = new StringReader(text);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (!string.IsNullOrWhiteSpace(line))
                return DelimitedTextParser.DetectDelimiter(line);
        }
        return ',';
    }

    public static ImportColumnMapping DetectColumnMapping(IReadOnlyList<string> headers)
    {
        var mapping = new ImportColumnMapping();
        var assigned = new HashSet<ImportColumnRole>();

        for (int i = 0; i < headers.Count; i++)
        {
            string raw = (headers[i] ?? string.Empty).Trim();
            if (raw.Length == 0)
                continue;

            string key = NormalizeHeader(raw);
            if (!HeaderAliases.TryGetValue(key, out var role))
                continue;

            if (assigned.Contains(role))
                continue;

            SetIndex(mapping, role, i);
            assigned.Add(role);
        }

        bool hasIdentity = mapping.NameIndex >= 0
                           || mapping.UrlIndex >= 0
                           || mapping.UsernameIndex >= 0
                           || mapping.EmailIndex >= 0;

        mapping.IsConfident = mapping.HasPassword && hasIdentity;
        return mapping;
    }

    public static List<ImportCandidate> BuildCandidates(ParsedImportTable table, ImportColumnMapping mapping)
    {
        var list = new List<ImportCandidate>(table.Rows.Count);

        for (int i = 0; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            int rowNumber = i + 2; // 1-based data row assuming header on line 1

            string name = Get(row, mapping.NameIndex);
            string url = Get(row, mapping.UrlIndex);
            string username = Get(row, mapping.UsernameIndex);
            string email = Get(row, mapping.EmailIndex);
            string password = Get(row, mapping.PasswordIndex);
            string notes = Get(row, mapping.NotesIndex);

            if (string.IsNullOrWhiteSpace(email)
                && !string.IsNullOrWhiteSpace(username)
                && username.Contains('@', StringComparison.Ordinal))
            {
                email = username;
            }

            if (string.IsNullOrWhiteSpace(name))
                name = InferServiceName(url, username, email);

            var candidate = new ImportCandidate
            {
                SourceRowNumber = rowNumber,
                ServiceName = name.Trim(),
                Url = url.Trim(),
                Username = username.Trim(),
                Email = email.Trim(),
                Password = password, // preserve exact password (no trim of leading/trailing intentional spaces in rare cases — trim is usually fine)
                Notes = notes.Trim()
            };

            // Prefer trimmed password for normal exports
            candidate.Password = password.Trim().TrimEnd('\r');

            if (string.IsNullOrWhiteSpace(candidate.Password)
                && string.IsNullOrWhiteSpace(candidate.Username)
                && string.IsNullOrWhiteSpace(candidate.Email)
                && string.IsNullOrWhiteSpace(candidate.ServiceName))
            {
                candidate.ErrorResourceKey = "Import_FailReason_EmptyRow";
            }
            else
            {
                var entry = ToAccountEntry(candidate);
                var validation = EntryValidator.ValidateAccount(entry);
                if (!validation.IsValid)
                    candidate.ErrorResourceKey = validation.ErrorResourceKey ?? "Import_FailReason_Validation";
            }

            list.Add(candidate);
        }

        return list;
    }

    public static AccountEntry ToAccountEntry(ImportCandidate c) => new()
    {
        ServiceName = c.ServiceName,
        Username = c.Username,
        Email = c.Email,
        Password = c.Password,
        Url = string.IsNullOrWhiteSpace(c.Url) ? null : c.Url,
        Notes = c.Notes,
        AccountType = string.IsNullOrWhiteSpace(c.AccountType) ? "Website" : c.AccountType
    };

    public static Guid? FindDuplicateId(ImportCandidate candidate, IEnumerable<VaultEntry> existingEntries)
    {
        string urlKey = NormalizeUrl(candidate.Url);
        string userKey = NormalizeUsername(candidate.Username, candidate.Email);

        if (string.IsNullOrEmpty(urlKey) && string.IsNullOrEmpty(userKey))
            return null;

        foreach (var entry in existingEntries.OfType<AccountEntry>())
        {
            string existingUrl = NormalizeUrl(entry.Url);
            string existingUser = NormalizeUsername(entry.Username, entry.Email);

            if (string.IsNullOrEmpty(urlKey) || string.IsNullOrEmpty(existingUrl))
            {
                // Require both sides to have a URL for URL+username matching per spec.
                continue;
            }

            if (urlKey == existingUrl && userKey == existingUser && !string.IsNullOrEmpty(userKey))
                return entry.Id;
        }

        return null;
    }

    public static string NormalizeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        var value = url.Trim();
        if (value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            value = value[8..];
        else if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            value = value[7..];

        if (value.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            value = value[4..];

        while (value.EndsWith('/'))
            value = value[..^1];

        // Drop path query for duplicate matching? Keep full host+path without query for better accuracy.
        int q = value.IndexOf('?', StringComparison.Ordinal);
        if (q >= 0)
            value = value[..q];

        return value.ToLowerInvariant();
    }

    public static string NormalizeUsername(string? username, string? email)
    {
        var value = !string.IsNullOrWhiteSpace(username) ? username : email;
        return (value ?? string.Empty).Trim().ToLowerInvariant();
    }

    public static void ClearCandidates(IList<ImportCandidate>? candidates)
    {
        if (candidates == null)
            return;

        foreach (var c in candidates)
            c.ClearSensitiveData();

        candidates.Clear();
    }

    private static string InferServiceName(string url, string username, string email)
    {
        if (!string.IsNullOrWhiteSpace(url))
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
                // ignore — fall through
            }
        }

        if (!string.IsNullOrWhiteSpace(username))
            return username;
        if (!string.IsNullOrWhiteSpace(email))
            return email;
        return string.Empty;
    }

    private static bool LooksLikeHeaderRow(string[] row)
    {
        int hits = 0;
        foreach (var cell in row)
        {
            var key = NormalizeHeader(cell ?? string.Empty);
            if (HeaderAliases.ContainsKey(key))
                hits++;
        }
        return hits >= 1;
    }

    private static string NormalizeHeader(string header)
    {
        header = header.Trim().Trim('\uFEFF');
        // Collapse whitespace
        while (header.Contains("  ", StringComparison.Ordinal))
            header = header.Replace("  ", " ", StringComparison.Ordinal);
        return header;
    }

    private static void SetIndex(ImportColumnMapping mapping, ImportColumnRole role, int index)
    {
        switch (role)
        {
            case ImportColumnRole.Name: mapping.NameIndex = index; break;
            case ImportColumnRole.Url: mapping.UrlIndex = index; break;
            case ImportColumnRole.Username: mapping.UsernameIndex = index; break;
            case ImportColumnRole.Email: mapping.EmailIndex = index; break;
            case ImportColumnRole.Password: mapping.PasswordIndex = index; break;
            case ImportColumnRole.Notes: mapping.NotesIndex = index; break;
        }
    }

    private static string Get(string[] row, int index)
    {
        if (index < 0 || index >= row.Length)
            return string.Empty;
        return row[index] ?? string.Empty;
    }

}
