using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using Urkey.Core.Models;
using Urkey.Core.Services;
using Urkey.WPF.Commands;
using Urkey.WPF.Helpers;
using Urkey.WPF.Views.Windows;

namespace Urkey.WPF.ViewModels
{
    public class AllEntriesViewModel : ViewModelBase
    {
        public const string FilterAll = "all";

        private readonly VaultService _vaultService;

        public ObservableCollection<EntryListItem> Entries { get; } = new();
        public ICollectionView FilteredEntries { get; }

        public ObservableCollection<string> TypeFilters { get; } = new()
        {
            FilterAll,
            EntryMetadata.TypeAccount,
            EntryMetadata.TypeCard,
            EntryMetadata.TypeAddress,
            EntryMetadata.TypeDocument,
            EntryMetadata.TypeNote
        };

        private EntryListItem? _selectedEntry;
        public EntryListItem? SelectedEntry
        {
            get => _selectedEntry;
            set
            {
                if (!SetProperty(ref _selectedEntry, value)) return;
                OnPropertyChanged(nameof(HasSelection));
                OnPropertyChanged(nameof(PreviewText));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool HasSelection => SelectedEntry != null;
        public bool IsEmpty => !FilteredEntries.Cast<object>().Any();

        public string PreviewText => SelectedEntry?.Detail ?? Loc.Get("SelectEntryToPreview", Loc.Get("SelectDocumentToPreview"));

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (!SetProperty(ref _searchText, value ?? string.Empty)) return;
                FilteredEntries.Refresh();
                NotifyEmpty();
            }
        }

        private string _selectedTypeFilter = FilterAll;
        public string SelectedTypeFilter
        {
            get => _selectedTypeFilter;
            set
            {
                if (!SetProperty(ref _selectedTypeFilter, string.IsNullOrWhiteSpace(value) ? FilterAll : value)) return;
                FilteredEntries.Refresh();
                NotifyEmpty();
            }
        }

        /// <summary>ComboBox SelectedIndex bridge (0=all,1=account,2=card,3=address,4=document,5=note).</summary>
        public int SelectedTypeFilterIndex
        {
            get => TypeFilters.IndexOf(SelectedTypeFilter);
            set
            {
                if (value < 0 || value >= TypeFilters.Count) return;
                SelectedTypeFilter = TypeFilters[value];
                OnPropertyChanged(nameof(SelectedTypeFilterIndex));
            }
        }

        public ICommand ReloadCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public AllEntriesViewModel(VaultService vaultService)
        {
            _vaultService = vaultService;
            FilteredEntries = CollectionViewSource.GetDefaultView(Entries);
            FilteredEntries.Filter = FilterEntry;

            ReloadCommand = new RelayCommand<object>(_ => Reload());
            EditCommand = new RelayCommand<object>(_ => EditSelected(), _ => SelectedEntry != null);
            DeleteCommand = new RelayCommand<object>(_ => DeleteSelected(), _ => SelectedEntry != null);

            Reload();
        }

        public void Reload()
        {
            _vaultService.EnsureLoaded();
            Entries.Clear();

            foreach (var entry in _vaultService.GetEntries().OrderByDescending(e => e.UpdatedAt))
                Entries.Add(new EntryListItem(entry));

            FilteredEntries.Refresh();
            SelectedEntry = FilteredEntries.Cast<EntryListItem>().FirstOrDefault();
            NotifyEmpty();
        }

        private bool FilterEntry(object obj)
        {
            if (obj is not EntryListItem item) return false;

            if (SelectedTypeFilter != FilterAll &&
                !string.Equals(item.TypeKey, SelectedTypeFilter, StringComparison.OrdinalIgnoreCase))
                return false;

            if (string.IsNullOrWhiteSpace(SearchText))
                return true;

            var q = SearchText.Trim();
            return Contains(item.Title, q)
                   || Contains(item.Subtitle, q)
                   || Contains(item.Email, q)
                   || Contains(item.Username, q)
                   || Contains(item.UrlOrPath, q)
                   || Contains(item.TypeDisplay, q);
        }

        private static bool Contains(string? haystack, string needle)
            => !string.IsNullOrEmpty(haystack) &&
               haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);

        private void EditSelected()
        {
            if (SelectedEntry == null) return;

            bool changed = SelectedEntry.Entry switch
            {
                AccountEntry account => EditAccount(account),
                CardEntry card => EntryDialogHelper.AddOrEditCard(card) != null,
                AddressEntry address => EntryDialogHelper.AddOrEditAddress(address) != null,
                NoteEntry note => EntryDialogHelper.AddOrEditNote(note) != null,
                DocumentEntry doc => EditDocument(doc),
                _ => false
            };

            if (changed)
                Reload();
        }

        private bool EditAccount(AccountEntry account)
        {
            var editor = new EditAccount(account) { Owner = System.Windows.Application.Current.MainWindow };
            if (editor.ShowDialog() != true)
                return false;

            var validation = EntryValidator.ValidateAccount(account);
            if (!validation.IsValid)
            {
                ToastService.Warning(Loc.Get(validation.ErrorResourceKey!));
                return false;
            }

            _vaultService.UpdateEntry(account);
            return true;
        }

        private bool EditDocument(DocumentEntry doc)
        {
            var win = new AddDocument(doc) { Owner = System.Windows.Application.Current.MainWindow };
            if (win.ShowDialog() != true || win.Document == null)
                return false;

            doc.Name = win.Document.Name;
            doc.Type = win.Document.Type;
            doc.Number = win.Document.Number;
            doc.Issuer = win.Document.Issuer;
            doc.Notes = win.Document.Notes;
            doc.ExpiryDate = win.Document.ExpiryDate;

            if (!string.IsNullOrWhiteSpace(win.Document.ExternalImagePath) &&
                win.Document.ExternalImagePath != doc.ExternalImagePath &&
                System.IO.File.Exists(win.Document.ExternalImagePath))
            {
                string encrypted = FileHelper.SaveDocumentImage(
                    win.Document.ExternalImagePath,
                    _vaultService.GetVaultDirectory());
                doc.ExternalImagePath = encrypted;
            }

            _vaultService.UpdateEntry(doc);
            return true;
        }

        private void DeleteSelected()
        {
            if (SelectedEntry == null) return;
            if (!EntryDialogHelper.ConfirmDelete(SelectedEntry.Title))
                return;

            if (SelectedEntry.Entry is DocumentEntry doc &&
                !string.IsNullOrEmpty(doc.ExternalImagePath) &&
                System.IO.File.Exists(doc.ExternalImagePath))
            {
                try { System.IO.File.Delete(doc.ExternalImagePath); }
                catch { /* ignore */ }
            }

            _vaultService.RemoveEntry(SelectedEntry.Id);
            Reload();
        }

        private void NotifyEmpty()
        {
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(PreviewText));
        }
    }
}
