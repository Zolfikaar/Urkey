using System.Globalization;
using System.IO;
using System.Text;
using Urkey.Core.Models;
using Urkey.Core.Paths;

namespace Urkey.Core.Services;

public static class VaultExportService
{
    public static int ExportAccountsCsv(IEnumerable<VaultEntry> entries, string filePath)
    {
        var accounts = entries.OfType<AccountEntry>().ToList();
        var sb = new StringBuilder();
        sb.AppendLine("name,url,username,email,password,notes,type");

        foreach (var account in accounts)
        {
            sb.Append(Csv(account.ServiceName)).Append(',');
            sb.Append(Csv(account.Url)).Append(',');
            sb.Append(Csv(account.Username)).Append(',');
            sb.Append(Csv(account.Email)).Append(',');
            sb.Append(Csv(account.Password)).Append(',');
            sb.Append(Csv(account.Notes)).Append(',');
            sb.Append(Csv(account.AccountType)).AppendLine();
        }

        File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        return accounts.Count;
    }

    public static int ExportToText(IEnumerable<VaultEntry> entries, string filePath)
    {
        var list = entries.ToList();
        var sb = new StringBuilder();
        int written = 0;

        var websites = list.OfType<AccountEntry>().Where(a => a.AccountType != "Application").ToList();
        var apps = list.OfType<AccountEntry>().Where(a => a.AccountType == "Application").ToList();
        var cards = list.OfType<CardEntry>().ToList();
        var addresses = list.OfType<AddressEntry>().ToList();
        var notes = list.OfType<NoteEntry>().ToList();

        if (websites.Count > 0)
        {
            sb.AppendLine("Websites");
            foreach (var account in websites)
            {
                WriteAccountBlock(sb, account, website: true);
                written++;
            }
        }

        if (apps.Count > 0)
        {
            sb.AppendLine("Applications");
            foreach (var account in apps)
            {
                WriteAccountBlock(sb, account, website: false);
                written++;
            }
        }

        if (cards.Count > 0)
        {
            sb.AppendLine("Bank cards");
            foreach (var card in cards)
            {
                sb.AppendLine("Name: " + card.HolderName);
                sb.AppendLine("Number: " + card.Number);
                sb.AppendLine("Expiry: " + card.ExpiryDate.ToString("MM/yy", CultureInfo.InvariantCulture));
                sb.AppendLine("CVV: " + (card.Cvv ?? string.Empty));
                sb.AppendLine("Comment: " + card.Notes);
                sb.AppendLine("---");
                written++;
            }
        }

        if (addresses.Count > 0)
        {
            sb.AppendLine("Addresses");
            foreach (var address in addresses)
            {
                sb.AppendLine("Name: " + address.Name);
                sb.AppendLine("Street: " + address.Street);
                sb.AppendLine("City: " + address.City);
                sb.AppendLine("Country: " + address.Country);
                sb.AppendLine("Zip: " + (address.ZipCode ?? string.Empty));
                sb.AppendLine("Comment: " + address.Notes);
                sb.AppendLine("---");
                written++;
            }
        }

        if (notes.Count > 0)
        {
            sb.AppendLine("Notes");
            foreach (var note in notes)
            {
                sb.AppendLine("Name: " + note.Title);
                sb.AppendLine("Comment: " + note.Content);
                sb.AppendLine("---");
                written++;
            }
        }

        File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        return written;
    }

    public static string CreateBackup(string vaultDirectory, string destinationFolder)
    {
        Directory.CreateDirectory(destinationFolder);

        CopyIfExists(Path.Combine(vaultDirectory, AppDataPaths.VaultFileName), destinationFolder);
        CopyIfExists(Path.Combine(vaultDirectory, AppDataPaths.UserFileName), destinationFolder);
        CopyIfExists(Path.Combine(vaultDirectory, AppDataPaths.SettingsFileName), destinationFolder);

        string docsSource = Path.Combine(vaultDirectory, AppDataPaths.DocumentsFolderName);
        if (Directory.Exists(docsSource))
        {
            string docsDest = Path.Combine(destinationFolder, AppDataPaths.DocumentsFolderName);
            Directory.CreateDirectory(docsDest);
            foreach (var file in Directory.EnumerateFiles(docsSource))
            {
                File.Copy(file, Path.Combine(docsDest, Path.GetFileName(file)), overwrite: true);
            }
        }

        return destinationFolder;
    }

    public static IEnumerable<T> OrderEntries<T>(IEnumerable<T> items, bool alphabetical)
        where T : VaultEntry
    {
        return alphabetical
            ? items.OrderBy(e => EntryMetadata.GetDisplayName(e), StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(e => e.CreatedAt)
            : items.OrderByDescending(e => e.UpdatedAt);
    }

    private static void WriteAccountBlock(StringBuilder sb, AccountEntry account, bool website)
    {
        if (website)
        {
            sb.AppendLine("Website name: " + account.ServiceName);
            sb.AppendLine("Website URL: " + (account.Url ?? string.Empty));
        }
        else
        {
            sb.AppendLine("Application: " + account.ServiceName);
        }

        sb.AppendLine("Login: " + FirstNonEmpty(account.Username, account.Email));
        sb.AppendLine("Password: " + account.Password);
        sb.AppendLine("Comment: " + account.Notes);
        sb.AppendLine("---");
    }

    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim() ?? string.Empty;

    private static string Csv(string? value)
    {
        var text = value ?? string.Empty;
        if (text.Contains('"') || text.Contains(',') || text.Contains('\n') || text.Contains('\r'))
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        return text;
    }

    private static void CopyIfExists(string source, string destFolder)
    {
        if (!File.Exists(source))
            return;

        File.Copy(source, Path.Combine(destFolder, Path.GetFileName(source)), overwrite: true);
    }
}
