using Urkey.Core.Managers;
using Urkey.Core.Models;
using Urkey.Core.Repository;
using Urkey.Core.Services.Import;

namespace Urkey.Core.Services;

public class VaultService
{
    private const int MaxActivityEntries = 50;

    private readonly VaultRepository _repo;
    private Vault? _vault;

    public VaultService()
    {
        _repo = new VaultRepository();
        _vault = new Vault();
    }

    public bool VaultExists() => _repo.VaultExists();

    public Vault Load()
    {
        _vault = _repo.Load();
        return _vault;
    }

    public Vault LoadVault() => Load();

    public Vault EnsureLoaded()
    {
        if (_vault == null)
            _vault = _repo.Load();
        return _vault;
    }

    public Vault? CurrentVault => _vault;

    public IReadOnlyList<VaultEntry> GetEntries()
        => EnsureLoaded().Entries;

    public void AddEntry(VaultEntry entry, bool logActivity = true)
    {
        var vault = EnsureLoaded();
        entry.CreatedAt = DateTime.UtcNow;
        entry.UpdatedAt = entry.CreatedAt;
        vault.Entries.Add(entry);

        if (logActivity)
        {
            LogActivity(
                EntryMetadata.ActionAdded,
                EntryMetadata.GetTypeKey(entry),
                EntryMetadata.GetDisplayName(entry),
                entry.Id,
                save: false);
        }

        _repo.Save(vault);
    }

    /// <summary>
    /// Imports account entries in a single vault save. Logs one "imported" activity entry.
    /// Reuses the same in-memory model path as manual Add/Update (encryption happens in the repository).
    /// </summary>
    public PasswordImportSummary ImportAccounts(IReadOnlyList<PasswordImportCommitItem> items)
    {
        var summary = new PasswordImportSummary();
        if (items == null || items.Count == 0)
            return summary;

        var vault = EnsureLoaded();
        var now = DateTime.UtcNow;
        int importedForLog = 0;

        foreach (var item in items)
        {
            var candidate = item.Candidate;
            if (candidate.HasError)
            {
                summary.Failed++;
                summary.Failures.Add((candidate.SourceRowNumber, candidate.ErrorResourceKey!));
                continue;
            }

            if (item.Resolution == DuplicateResolution.Skip && item.ExistingEntryId.HasValue)
            {
                summary.SkippedDuplicates++;
                continue;
            }

            var account = PasswordImportService.ToAccountEntry(candidate);
            var validation = EntryValidator.ValidateAccount(account);
            if (!validation.IsValid)
            {
                summary.Failed++;
                summary.Failures.Add((
                    candidate.SourceRowNumber,
                    validation.ErrorResourceKey ?? "Import_FailReason_Validation"));
                continue;
            }

            if (item.Resolution == DuplicateResolution.Overwrite && item.ExistingEntryId.HasValue)
            {
                var existing = vault.Entries.FirstOrDefault(e => e.Id == item.ExistingEntryId.Value) as AccountEntry;
                if (existing == null)
                {
                    // Fall back to add if the duplicate disappeared.
                    account.CreatedAt = now;
                    account.UpdatedAt = now;
                    vault.Entries.Add(account);
                    summary.Imported++;
                    importedForLog++;
                }
                else
                {
                    account.Id = existing.Id;
                    account.CreatedAt = existing.CreatedAt;
                    account.UpdatedAt = now;
                    account.IsFavorite = existing.IsFavorite;
                    account.Category = existing.Category;
                    account.AccountType = string.IsNullOrWhiteSpace(existing.AccountType)
                        ? "Website"
                        : existing.AccountType;

                    int index = vault.Entries.IndexOf(existing);
                    vault.Entries[index] = account;
                    summary.Overwritten++;
                    importedForLog++;
                }
            }
            else
            {
                account.CreatedAt = now;
                account.UpdatedAt = now;
                vault.Entries.Add(account);
                summary.Imported++;
                importedForLog++;
            }
        }

        if (importedForLog > 0)
        {
            LogActivity(
                EntryMetadata.ActionImported,
                EntryMetadata.TypeAccount,
                importedForLog.ToString(),
                entryId: null,
                save: false);
        }

        _repo.Save(vault);
        return summary;
    }

    public void UpdateEntry(VaultEntry entry, bool logActivity = true)
    {
        var vault = EnsureLoaded();
        var existing = vault.Entries.FirstOrDefault(e => e.Id == entry.Id);
        if (existing == null)
            throw new InvalidOperationException("Entry not found.");

        entry.UpdatedAt = DateTime.UtcNow;
        if (entry.CreatedAt == default)
            entry.CreatedAt = existing.CreatedAt;

        int index = vault.Entries.IndexOf(existing);
        vault.Entries[index] = entry;

        if (logActivity)
        {
            LogActivity(
                EntryMetadata.ActionEdited,
                EntryMetadata.GetTypeKey(entry),
                EntryMetadata.GetDisplayName(entry),
                entry.Id,
                save: false);
        }

        _repo.Save(vault);
    }

    public bool RemoveEntry(Guid id, bool logActivity = true)
    {
        var vault = EnsureLoaded();
        var existing = vault.Entries.FirstOrDefault(e => e.Id == id);
        if (existing == null)
            return false;

        string type = EntryMetadata.GetTypeKey(existing);
        string name = EntryMetadata.GetDisplayName(existing);

        vault.Entries.Remove(existing);

        if (logActivity)
            LogActivity(EntryMetadata.ActionDeleted, type, name, id, save: false);

        _repo.Save(vault);
        return true;
    }

    public void Save()
    {
        var vault = EnsureLoaded();
        _repo.Save(vault);
    }

    public void TouchLastAccessed(Guid id)
    {
        var vault = EnsureLoaded();
        var existing = vault.Entries.FirstOrDefault(e => e.Id == id);
        if (existing == null)
            return;

        existing.LastAccessedAt = DateTime.UtcNow;
        _repo.Save(vault);
    }

    public void SortEntriesAlphabetically()
    {
        var vault = EnsureLoaded();
        vault.Entries = VaultExportService
            .OrderEntries(vault.Entries, alphabetical: true)
            .ToList();
        _repo.Save(vault);
    }

    public static DateTime GetRecency(VaultEntry entry)
        => entry.LastAccessedAt == default ? entry.UpdatedAt : entry.LastAccessedAt;

    public void LogActivity(string action, string entryType, string entryName, Guid? entryId = null, bool save = true)
    {
        var vault = EnsureLoaded();
        vault.ActivityLog.Insert(0, new ActivityLogEntry
        {
            Action = action,
            EntryType = entryType,
            EntryName = entryName,
            EntryId = entryId,
            Timestamp = DateTime.UtcNow
        });

        if (vault.ActivityLog.Count > MaxActivityEntries)
            vault.ActivityLog.RemoveRange(MaxActivityEntries, vault.ActivityLog.Count - MaxActivityEntries);

        if (save)
            _repo.Save(vault);
    }

    public IReadOnlyList<ActivityLogEntry> GetRecentActivity(int count = 20)
    {
        var vault = EnsureLoaded();
        return vault.ActivityLog.Take(count).ToList();
    }

    public string GetVaultPath() => _repo.GetVaultPath();

    public string GetVaultDirectory() => _repo.GetVaultDirectory();

    public void ClearSession()
    {
        _vault = new Vault();
        VaultManager.Lock();
        FileHelper.CleanupTempDocumentImages();
    }
}
