using System.Collections.ObjectModel;
using System.Windows.Input;
using Urkey.Core.Models;
using Urkey.Core.Services;
using Urkey.WPF.Commands;
using Urkey.WPF.Helpers;

namespace Urkey.WPF.ViewModels
{
    public class NotesViewModel : ViewModelBase
    {
        private readonly VaultService _vaultService;

        public ObservableCollection<NoteEntry> Items { get; } = new();

        private NoteEntry? _selectedItem;
        public NoteEntry? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (!SetProperty(ref _selectedItem, value)) return;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (!SetProperty(ref _searchText, value ?? string.Empty)) return;
                Reload();
            }
        }

        public bool IsEmpty => Items.Count == 0;

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ReloadCommand { get; }

        public NotesViewModel(VaultService vaultService)
        {
            _vaultService = vaultService;
            AddCommand = new RelayCommand<object>(_ => Add());
            EditCommand = new RelayCommand<object>(_ => Edit(), _ => SelectedItem != null);
            DeleteCommand = new RelayCommand<object>(_ => Delete(), _ => SelectedItem != null);
            ReloadCommand = new RelayCommand<object>(_ => Reload());
            Reload();
        }

        public void Reload()
        {
            Items.Clear();
            var query = _searchText.Trim();
            foreach (var note in _vaultService.GetEntries().OfType<NoteEntry>()
                         .OrderByDescending(n => n.UpdatedAt))
            {
                if (!string.IsNullOrEmpty(query) &&
                    !Contains(note.Title, query) &&
                    !Contains(note.Content, query) &&
                    !Contains(note.Category, query) &&
                    !Contains(note.Tags, query))
                    continue;

                Items.Add(note);
            }

            SelectedItem = Items.FirstOrDefault();
            OnPropertyChanged(nameof(IsEmpty));
        }

        private void Add()
        {
            if (EntryDialogHelper.AddOrEditNote() != null)
                Reload();
        }

        private void Edit()
        {
            if (SelectedItem == null) return;
            if (EntryDialogHelper.AddOrEditNote(SelectedItem) != null)
                Reload();
        }

        private void Delete()
        {
            if (SelectedItem == null) return;
            if (!EntryDialogHelper.ConfirmDelete(SelectedItem.Title))
                return;

            _vaultService.RemoveEntry(SelectedItem.Id);
            Reload();
        }

        private static bool Contains(string? text, string q)
            => !string.IsNullOrEmpty(text) && text.Contains(q, StringComparison.OrdinalIgnoreCase);
    }
}
