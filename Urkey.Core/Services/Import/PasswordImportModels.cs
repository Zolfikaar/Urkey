namespace Urkey.Core.Services.Import;

public enum ImportColumnRole
{
    None = 0,
    Name,
    Url,
    Username,
    Email,
    Password,
    Notes
}

public enum DuplicateResolution
{
    Skip = 0,
    ImportAnyway,
    Overwrite
}

public sealed class ImportColumnMapping
{
    public int NameIndex { get; set; } = -1;
    public int UrlIndex { get; set; } = -1;
    public int UsernameIndex { get; set; } = -1;
    public int EmailIndex { get; set; } = -1;
    public int PasswordIndex { get; set; } = -1;
    public int NotesIndex { get; set; } = -1;

    /// <summary>
    /// True when password and at least one identity column (name/url/username/email) were matched by known headers.
    /// </summary>
    public bool IsConfident { get; set; }

    public bool HasPassword => PasswordIndex >= 0;

    public void Clear()
    {
        NameIndex = UrlIndex = UsernameIndex = EmailIndex = PasswordIndex = NotesIndex = -1;
        IsConfident = false;
    }
}

public sealed class ParsedImportTable
{
    public required IReadOnlyList<string> Headers { get; init; }
    public required IReadOnlyList<string[]> Rows { get; init; }
    public char Delimiter { get; init; }
    public required ImportColumnMapping AutoMapping { get; init; }
    public int EmptyRowCount { get; init; }
    public int MalformedRowCount { get; init; }

    /// <summary>Overwrite and drop raw cell values after candidates have been built.</summary>
    public void ClearRawRows()
    {
        if (Rows is List<string[]> list)
        {
            foreach (var row in list)
            {
                for (int i = 0; i < row.Length; i++)
                    row[i] = string.Empty;
            }
            list.Clear();
        }
        else
        {
            foreach (var row in Rows)
            {
                for (int i = 0; i < row.Length; i++)
                    row[i] = string.Empty;
            }
        }
    }
}

public sealed class ImportCandidate
{
    public int SourceRowNumber { get; init; }
    public string ServiceName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string AccountType { get; set; } = "Website";

    /// <summary>Resource key explaining why this row cannot be imported, or null if OK.</summary>
    public string? ErrorResourceKey { get; set; }

    public bool HasError => ErrorResourceKey != null;

    public void ClearSensitiveData()
    {
        Password = string.Empty;
        Notes = string.Empty;
        Username = string.Empty;
        Email = string.Empty;
        Url = string.Empty;
        ServiceName = string.Empty;
    }
}

/// <summary>Result of reading an import file — either delimited CSV/TSV or a structured export.</summary>
public sealed class ImportFileParseResult
{
    public ParsedImportTable? Table { get; init; }
    public List<ImportCandidate>? StructuredCandidates { get; init; }
    public bool IsStructured => StructuredCandidates != null;
}

public sealed class PasswordImportCommitItem
{
    public required ImportCandidate Candidate { get; init; }
    public DuplicateResolution Resolution { get; init; }
    public Guid? ExistingEntryId { get; init; }
}

public sealed class PasswordImportSummary
{
    public int Imported { get; set; }
    public int SkippedDuplicates { get; set; }
    public int Failed { get; set; }
    public int Overwritten { get; set; }
    public List<(int RowNumber, string ReasonResourceKey)> Failures { get; } = new();
}
