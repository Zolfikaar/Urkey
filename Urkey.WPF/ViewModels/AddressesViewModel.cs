using System.Collections.ObjectModel;
using System.Windows.Input;
using Urkey.Core.Models;
using Urkey.Core.Services;
using Urkey.WPF.Commands;
using Urkey.WPF.Helpers;

namespace Urkey.WPF.ViewModels
{
    public class AddressesViewModel : ViewModelBase
    {
        private readonly VaultService _vaultService;

        public ObservableCollection<AddressEntry> Items { get; } = new();

        private AddressEntry? _selectedItem;
        public AddressEntry? SelectedItem
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

        public AddressesViewModel(VaultService vaultService)
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
            foreach (var address in _vaultService.GetEntries().OfType<AddressEntry>()
                         .OrderByDescending(a => a.UpdatedAt))
            {
                if (!string.IsNullOrEmpty(query) &&
                    !Contains(address.Name, query) &&
                    !Contains(address.City, query) &&
                    !Contains(address.Street, query) &&
                    !Contains(address.Country, query))
                    continue;

                Items.Add(address);
            }

            SelectedItem = Items.FirstOrDefault();
            OnPropertyChanged(nameof(IsEmpty));
        }

        private void Add()
        {
            if (EntryDialogHelper.AddOrEditAddress() != null)
                Reload();
        }

        private void Edit()
        {
            if (SelectedItem == null) return;
            if (EntryDialogHelper.AddOrEditAddress(SelectedItem) != null)
                Reload();
        }

        private void Delete()
        {
            if (SelectedItem == null) return;
            if (!EntryDialogHelper.ConfirmDelete(SelectedItem.Name))
                return;

            _vaultService.RemoveEntry(SelectedItem.Id);
            Reload();
        }

        private static bool Contains(string? text, string q)
            => !string.IsNullOrEmpty(text) && text.Contains(q, StringComparison.OrdinalIgnoreCase);
    }
}
