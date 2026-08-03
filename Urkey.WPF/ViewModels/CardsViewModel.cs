using System.Collections.ObjectModel;
using System.Windows.Input;
using Urkey.Core.Models;
using Urkey.Core.Services;
using Urkey.WPF.Commands;
using Urkey.WPF.Helpers;

namespace Urkey.WPF.ViewModels
{
    public class CardsViewModel : ViewModelBase
    {
        private readonly VaultService _vaultService;

        public ObservableCollection<CardEntry> Items { get; } = new();

        private CardEntry? _selectedItem;
        public CardEntry? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (!SetProperty(ref _selectedItem, value)) return;
                OnPropertyChanged(nameof(SelectedMaskedNumber));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string SelectedMaskedNumber
            => SelectedItem == null ? string.Empty : EntryMetadata.MaskCard(SelectedItem.Number);

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

        public CardsViewModel(VaultService vaultService)
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
            foreach (var card in _vaultService.GetEntries().OfType<CardEntry>()
                         .OrderByDescending(c => c.UpdatedAt))
            {
                if (!string.IsNullOrEmpty(query) &&
                    !Contains(card.HolderName, query) &&
                    !Contains(card.Number, query) &&
                    !Contains(card.Notes, query))
                    continue;

                Items.Add(card);
            }

            SelectedItem = Items.FirstOrDefault();
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(SelectedMaskedNumber));
        }

        private void Add()
        {
            if (EntryDialogHelper.AddOrEditCard() != null)
                Reload();
        }

        private void Edit()
        {
            if (SelectedItem == null) return;
            if (EntryDialogHelper.AddOrEditCard(SelectedItem) != null)
                Reload();
        }

        private void Delete()
        {
            if (SelectedItem == null) return;
            if (!EntryDialogHelper.ConfirmDelete(SelectedItem.HolderName))
                return;

            _vaultService.RemoveEntry(SelectedItem.Id);
            Reload();
        }

        private static bool Contains(string? text, string q)
            => !string.IsNullOrEmpty(text) && text.Contains(q, StringComparison.OrdinalIgnoreCase);
    }
}
