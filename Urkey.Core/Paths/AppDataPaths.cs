using System.Diagnostics;
using System.IO;

namespace Urkey.Core.Paths;

/// <summary>
/// Canonical local storage for UrKey under
/// <see cref="Environment.SpecialFolder.LocalApplicationData"/>\<c>UrKey</c>.
/// Migrates once from the legacy Roaming <c>%AppData%\Urkey</c> folder when needed.
/// </summary>
public static class AppDataPaths
{
    public const string AppFolderName = "UrKey";
    public const string LegacyAppFolderName = "Urkey";

    private static readonly object Gate = new();
    private static bool _initialized;
    private static string? _rootDirectory;

    public static string RootDirectory
    {
        get
        {
            EnsureInitialized();
            return _rootDirectory!;
        }
    }

    public static string VaultFileName => "vault.json";
    public static string UserFileName => "user.json";
    public static string SettingsFileName => "settings.json";
    public static string SecureStorageFileName => "secure.dat";
    public static string DocumentsFolderName => "DocumentsFiles";

    public static string VaultFilePath => Path.Combine(RootDirectory, VaultFileName);
    public static string UserFilePath => Path.Combine(RootDirectory, UserFileName);
    public static string SettingsFilePath => Path.Combine(RootDirectory, SettingsFileName);
    public static string SecureStorageFilePath => Path.Combine(RootDirectory, SecureStorageFileName);
    public static string DocumentsDirectory => Path.Combine(RootDirectory, DocumentsFolderName);

    /// <summary>
    /// Ensures the app data root exists and migrates legacy Roaming data if present.
    /// </summary>
    public static void EnsureInitialized()
    {
        lock (Gate)
        {
            if (_initialized)
                return;

            _rootDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppFolderName);

            Directory.CreateDirectory(_rootDirectory);
            MigrateFromLegacyLocations(_rootDirectory);
            _initialized = true;
        }
    }

    /// <summary>
    /// Redirect storage to an isolated folder (screenshot / test harness only).
    /// Must be called before any other AppDataPaths access.
    /// </summary>
    public static void OverrideRootDirectory(string absolutePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(absolutePath);

        lock (Gate)
        {
            _rootDirectory = Path.GetFullPath(absolutePath);
            Directory.CreateDirectory(_rootDirectory);
            _initialized = true;
        }
    }

    /// <summary>
    /// Deletes all UrKey local data (vault, user, settings, documents, secure store)
    /// from the canonical folder and legacy locations. Retries briefly on locked files.
    /// Does not throw for individual file failures.
    /// </summary>
    public static void ClearAllLocalData()
    {
        // Release any finalizers that might still hold file handles.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        lock (Gate)
        {
            string root = _rootDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppFolderName);

            TryDeleteDirectoryContents(root);

            // Prevent a later startup migration from restoring wiped data.
            foreach (string legacy in GetLegacyRootCandidates())
            {
                if (PathsEqual(legacy, root))
                    continue;

                TryDeleteDirectoryTree(legacy);
            }

            Directory.CreateDirectory(root);
            _rootDirectory = root;
            _initialized = true;
        }
    }

    private static IEnumerable<string> GetLegacyRootCandidates()
    {
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            LegacyAppFolderName);

        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            LegacyAppFolderName);
    }

    private static void MigrateFromLegacyLocations(string newRoot)
    {
        bool destHasVaultOrUser =
            File.Exists(Path.Combine(newRoot, VaultFileName)) ||
            File.Exists(Path.Combine(newRoot, UserFileName));

        if (destHasVaultOrUser)
            return;

        foreach (string legacy in GetLegacyRootCandidates())
        {
            if (PathsEqual(legacy, newRoot))
                continue;

            if (TryMigrateDirectory(legacy, newRoot))
                return;
        }
    }

    private static bool TryMigrateDirectory(string source, string dest)
    {
        try
        {
            if (!Directory.Exists(source))
                return false;

            bool sourceHasData =
                File.Exists(Path.Combine(source, VaultFileName)) ||
                File.Exists(Path.Combine(source, UserFileName)) ||
                File.Exists(Path.Combine(source, SettingsFileName)) ||
                Directory.Exists(Path.Combine(source, DocumentsFolderName));

            if (!sourceHasData)
                return false;

            foreach (string file in Directory.GetFiles(source))
            {
                string destFile = Path.Combine(dest, Path.GetFileName(file));
                if (File.Exists(destFile))
                    continue;

                TryMoveOrCopyFile(file, destFile);
            }

            string srcDocs = Path.Combine(source, DocumentsFolderName);
            string destDocs = Path.Combine(dest, DocumentsFolderName);
            if (Directory.Exists(srcDocs))
            {
                Directory.CreateDirectory(destDocs);
                foreach (string file in Directory.GetFiles(srcDocs))
                {
                    string destFile = Path.Combine(destDocs, Path.GetFileName(file));
                    if (File.Exists(destFile))
                        continue;

                    TryMoveOrCopyFile(file, destFile);
                }
            }

            return File.Exists(Path.Combine(dest, VaultFileName)) ||
                   File.Exists(Path.Combine(dest, UserFileName)) ||
                   File.Exists(Path.Combine(dest, SettingsFileName));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"UrKey data migration failed from '{source}': {ex.Message}");
            return false;
        }
    }

    private static void TryMoveOrCopyFile(string source, string dest)
    {
        try
        {
            File.Move(source, dest);
        }
        catch
        {
            try
            {
                File.Copy(source, dest, overwrite: false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UrKey failed to migrate '{source}': {ex.Message}");
            }
        }
    }

    private static void TryDeleteDirectoryContents(string directory)
    {
        if (!Directory.Exists(directory))
            return;

        foreach (string file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
            TryDeleteFileWithRetry(file);

        foreach (string dir in Directory.GetDirectories(directory)
                     .OrderByDescending(d => d.Length))
        {
            TryDeleteDirectoryTree(dir);
        }
    }

    private static void TryDeleteDirectoryTree(string directory)
    {
        if (!Directory.Exists(directory))
            return;

        foreach (string file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
            TryDeleteFileWithRetry(file);

        for (int attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, recursive: true);
                return;
            }
            catch (IOException)
            {
                Thread.Sleep(40 * (attempt + 1));
            }
            catch (UnauthorizedAccessException)
            {
                Thread.Sleep(40 * (attempt + 1));
            }
        }
    }

    private static void TryDeleteFileWithRetry(string path)
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                if (!File.Exists(path))
                    return;

                File.SetAttributes(path, FileAttributes.Normal);
                File.Delete(path);
                return;
            }
            catch (IOException)
            {
                Thread.Sleep(40 * (attempt + 1));
            }
            catch (UnauthorizedAccessException)
            {
                Thread.Sleep(40 * (attempt + 1));
            }
        }
    }

    private static bool PathsEqual(string a, string b)
    {
        try
        {
            string fullA = Path.GetFullPath(a).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string fullB = Path.GetFullPath(b).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return string.Equals(fullA, fullB, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
