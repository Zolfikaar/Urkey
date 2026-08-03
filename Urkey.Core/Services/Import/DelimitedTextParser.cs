using System.Text;

namespace Urkey.Core.Services.Import;

/// <summary>
/// Lightweight CSV/TSV parser with delimiter auto-detection and RFC4180-style quoting.
/// No third-party dependency — keeps plaintext import data in-process only.
/// </summary>
public static class DelimitedTextParser
{
    public static char DetectDelimiter(string sampleLine)
    {
        if (string.IsNullOrEmpty(sampleLine))
            return ',';

        int commas = CountUnquoted(sampleLine, ',');
        int semis = CountUnquoted(sampleLine, ';');
        int tabs = CountUnquoted(sampleLine, '\t');

        if (tabs >= commas && tabs >= semis && tabs > 0)
            return '\t';
        if (semis >= commas && semis > 0)
            return ';';
        return ',';
    }

    public static List<string[]> Parse(string text, char? delimiter = null)
    {
        var rows = new List<string[]>();
        if (string.IsNullOrWhiteSpace(text))
            return rows;

        // Normalize newlines while preserving quoted content via a scan.
        var lines = SplitLinesPreservingQuotes(text);
        if (lines.Count == 0)
            return rows;

        char delim = delimiter ?? DetectDelimiter(lines[0]);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            rows.Add(ParseLine(line, delim).ToArray());
        }

        return rows;
    }

    public static IReadOnlyList<string> ParseLine(string line, char delimiter)
    {
        var fields = new List<string>();
        if (line == null)
            return fields;

        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == delimiter)
                {
                    fields.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }
        }

        fields.Add(sb.ToString());
        return fields;
    }

    public static string DecodeBytes(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);

        // Prefer UTF-8; fall back to system ANSI if clearly invalid UTF-8.
        try
        {
            var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            return utf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.Default.GetString(bytes);
        }
    }

    private static int CountUnquoted(string line, char ch)
    {
        int count = 0;
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    i++;
                    continue;
                }
                inQuotes = !inQuotes;
            }
            else if (!inQuotes && c == ch)
            {
                count++;
            }
        }
        return count;
    }

    private static List<string> SplitLinesPreservingQuotes(string text)
    {
        var lines = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (c == '"')
            {
                sb.Append(c);
                if (inQuotes && i + 1 < text.Length && text[i + 1] == '"')
                {
                    sb.Append(text[i + 1]);
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
                continue;
            }

            if (!inQuotes && (c == '\r' || c == '\n'))
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    i++;

                lines.Add(sb.ToString());
                sb.Clear();
                continue;
            }

            sb.Append(c);
        }

        if (sb.Length > 0)
            lines.Add(sb.ToString());

        return lines;
    }
}
