using System.IO;
using Urkey.Core.Models;
using Urkey.Core.Paths;

namespace Urkey.Core.Services;

public sealed record DocumentStorageUsage(long UsedBytes, int FileCount, long MaxTotalBytes)
{
    public const long DefaultQuotaBytes = 50L * 1024 * 1024;

    public double Percent => MaxTotalBytes <= 0
        ? 0
        : Math.Clamp(UsedBytes / (double)MaxTotalBytes * 100.0, 0, 100);
}

public static class DocumentStorageStats
{
    public static DocumentStorageUsage Calculate(IEnumerable<VaultEntry> entries, string? vaultDirectory = null)
    {
        string docsDir = string.IsNullOrWhiteSpace(vaultDirectory)
            ? AppDataPaths.DocumentsDirectory
            : Path.Combine(vaultDirectory, AppDataPaths.DocumentsFolderName);

        long used = 0;
        int files = 0;

        if (Directory.Exists(docsDir))
        {
            foreach (var file in Directory.EnumerateFiles(docsDir, "*", SearchOption.AllDirectories))
            {
                try
                {
                    used += new FileInfo(file).Length;
                    files++;
                }
                catch
                {
                    // skip locked / inaccessible files
                }
            }
        }

        foreach (var doc in entries.OfType<DocumentEntry>())
        {
            if (!string.IsNullOrWhiteSpace(doc.FileContentBase64))
            {
                used += (long)Math.Ceiling(doc.FileContentBase64.Length * 3.0 / 4.0);
                files++;
            }
        }

        return new DocumentStorageUsage(used, files, DocumentStorageUsage.DefaultQuotaBytes);
    }

    public static string FormatSize(long bytes)
    {
        const double kb = 1024;
        const double mb = kb * 1024;
        if (bytes >= mb)
            return $"{bytes / mb:0.00} MB";
        if (bytes >= kb)
            return $"{bytes / kb:0.00} KB";
        return $"{bytes} B";
    }
}
