using Urkey.Core.Models;
using Urkey.Core.Services;

namespace Urkey.WPF.Helpers
{
    public static class EntryListSort
    {
        public static IEnumerable<T> Apply<T>(IEnumerable<T> items) where T : VaultEntry
            => VaultExportService.OrderEntries(items, App.Settings.SortEntriesAlphabetically);
    }
}
