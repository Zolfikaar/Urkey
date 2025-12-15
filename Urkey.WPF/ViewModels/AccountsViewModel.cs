using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Urkey.Core.Models;
using Urkey.Core.Repository;
using Urkey.WPF.Commands;
using Urkey.WPF.Views.Windows;

namespace Urkey.WPF.ViewModels
{
    public class AccountsViewModel : INotifyPropertyChanged
    {
        private const string AllCategoriesLabel = "All categories";

        private VaultRepository _repo;
        private Vault _vault;

        public ObservableCollection<AccountEntry> Accounts { get; }
        public ICollectionView FilteredAccounts { get; }
        public ObservableCollection<string> Categories { get; } = new();

        private AccountEntry? _selectedAccount;
        public AccountEntry? SelectedAccount
        {
            get => _selectedAccount;
            set
            {
                if (_selectedAccount == value) return;
                _selectedAccount = value;
                OnPropertyChanged(nameof(SelectedAccount));
                _toggleFavoriteRelay?.RaiseCanExecuteChanged();
                _editAccountRelay?.RaiseCanExecuteChanged();
                _deleteAccountRelay?.RaiseCanExecuteChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private bool _isListView = true;
        public bool IsListView
        {
            get => _isListView;
            set
            {
                if (_isListView == value) return;
                _isListView = value;
                OnPropertyChanged(nameof(IsListView));
                OnPropertyChanged(nameof(IsGridView));
            }
        }
        public bool IsGridView => !_isListView;

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                var newValue = value ?? string.Empty;
                if (string.Equals(_searchText, newValue, StringComparison.Ordinal)) return;
                _searchText = newValue;
                OnPropertyChanged(nameof(SearchText));
                FilteredAccounts.Refresh();
                EnsureSelectionVisible();
            }
        }

        private string _selectedCategory = AllCategoriesLabel;
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                var newValue = string.IsNullOrWhiteSpace(value) ? AllCategoriesLabel : value;
                if (string.Equals(_selectedCategory, newValue, StringComparison.OrdinalIgnoreCase)) return;
                _selectedCategory = newValue;
                OnPropertyChanged(nameof(SelectedCategory));
                FilteredAccounts.Refresh();
                EnsureSelectionVisible();
            }
        }

        public ICommand ReloadCommand { get; }
        public ICommand ToggleFavoriteCommand { get; }
        public ICommand EditAccountCommand { get; }
        public ICommand DeleteAccountCommand { get; }
        public ICommand OpenAccountCommand { get; }

        private readonly RelayCommand<object> _toggleFavoriteRelay;
        private readonly RelayCommand<object> _editAccountRelay;
        private readonly RelayCommand<object> _deleteAccountRelay;

        public AccountsViewModel()
        {
            _repo = new VaultRepository(            // إذا تم تمرير مسار يدوي نستخدمه، وإلا نحفظ في AppData\Urkey
            _vaultDirectory);
            _vault = _repo.ReloadFromDisk();

            Accounts = new ObservableCollection<AccountEntry>(_vault.Entries.OfType<AccountEntry>());
            Accounts.CollectionChanged += OnAccountsCollectionChanged;

            FilteredAccounts = CollectionViewSource.GetDefaultView(Accounts);
            FilteredAccounts.Filter = OnFilterAccount;

            RebuildCategories();

            ReloadCommand = new RelayCommand<object>(_ => Reload());
            _toggleFavoriteRelay = new RelayCommand<object>(_ => ToggleFavorite(), _ => SelectedAccount != null);
            ToggleFavoriteCommand = _toggleFavoriteRelay;
            _editAccountRelay = new RelayCommand<object>(_ => EditSelectedAccount(), _ => SelectedAccount != null);
            EditAccountCommand = _editAccountRelay;
            _deleteAccountRelay = new RelayCommand<object>(_ => DeleteSelectedAccount(), _ => SelectedAccount != null);
            DeleteAccountCommand = _deleteAccountRelay;
            OpenAccountCommand = new RelayCommand<AccountEntry>(OpenAccount, CanOpenAccount);
        }

        private void OnAccountsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RebuildCategories();
            FilteredAccounts.Refresh();
            EnsureSelectionVisible();
        }

        private bool OnFilterAccount(object obj)
        {
            if (obj is not AccountEntry entry)
                return false;

            if (_selectedCategory != AllCategoriesLabel)
            {
                var cat = NormalizeCategory(entry.AccountType);
                if (!string.Equals(cat, _selectedCategory, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                var text = _searchText.Trim();
                bool matches =
                    (!string.IsNullOrWhiteSpace(entry.ServiceName) && entry.ServiceName.Contains(text, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(entry.Username) && entry.Username.Contains(text, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(entry.Email) && entry.Email.Contains(text, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(entry.Url) && entry.Url.Contains(text, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(entry.ApplicationPath) && entry.ApplicationPath.Contains(text, StringComparison.OrdinalIgnoreCase));

                if (!matches)
                    return false;
            }

            return true;
        }

        public void Reload()
        {
            _repo = new VaultRepository(            // إذا تم تمرير مسار يدوي نستخدمه، وإلا نحفظ في AppData\Urkey
            _vaultDirectory);
            _vault = _repo.ReloadFromDisk();
            Accounts.CollectionChanged -= OnAccountsCollectionChanged;
            Accounts.Clear();
            foreach (var acc in _vault.Entries.OfType<AccountEntry>())
                Accounts.Add(acc);
            Accounts.CollectionChanged += OnAccountsCollectionChanged;

            RebuildCategories();
            FilteredAccounts.Refresh();
        }

        public void AddOrRefreshAccount(AccountEntry? entry)
        {
            _repo = new VaultRepository(            // إذا تم تمرير مسار يدوي نستخدمه، وإلا نحفظ في AppData\Urkey
            _vaultDirectory);
            _vault = _repo.ReloadFromDisk();

            if (entry == null)
            {
                Reload();
                return;
            }

            var refreshed = _vault.Entries.OfType<AccountEntry>().FirstOrDefault(a => a.Id == entry.Id);
            var target = refreshed ?? entry;

            var existing = Accounts.FirstOrDefault(a => a.Id == target.Id);
            if (existing == null)
            {
                Accounts.Add(target);
            }
            else
            {
                var index = Accounts.IndexOf(existing);
                Accounts[index] = target;
            }

            SelectedAccount = target;
            RebuildCategories();
            FilteredAccounts.Refresh();
            CommandManager.InvalidateRequerySuggested();
        }

        private void RebuildCategories()
        {
            var previous = _selectedCategory;
            Categories.Clear();
            Categories.Add(AllCategoriesLabel);

            var distinct = Accounts
                .Select(a => NormalizeCategory(a.AccountType))
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c, StringComparer.OrdinalIgnoreCase);

            foreach (var cat in distinct)
                Categories.Add(cat);

            if (!Categories.Any(c => c.Equals(previous, StringComparison.OrdinalIgnoreCase)))
            {
                _selectedCategory = AllCategoriesLabel;
                OnPropertyChanged(nameof(SelectedCategory));
            }
        }

        private static string NormalizeCategory(string? category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return "Website";

            if (category.Equals("Accounts", StringComparison.OrdinalIgnoreCase))
                return "Application";

            return category;
        }

        private void ToggleFavorite()
        {
            if (SelectedAccount == null) return;
            SelectedAccount.IsFavorite = !SelectedAccount.IsFavorite;
            _repo.SaveVault(_vault);
            FilteredAccounts.Refresh();
            EnsureSelectionVisible();
            CommandManager.InvalidateRequerySuggested();
        }

        private void EditSelectedAccount()
        {
            if (SelectedAccount == null) return;

            var currentId = SelectedAccount.Id;
            var editor = new EditAccount(SelectedAccount) { Owner = Application.Current.MainWindow };
            var result = editor.ShowDialog();
            if (result == true)
            {
                _repo.SaveVault(_vault);
                Reload();
                SelectedAccount = Accounts.FirstOrDefault(a => a.Id == currentId);
            }
        }

        private void DeleteSelectedAccount()
        {
            if (SelectedAccount == null) return;

            var response = MessageBox.Show($"Delete account \"{SelectedAccount.ServiceName}\"?", "Confirm delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (response != MessageBoxResult.Yes) return;

            var existing = _vault.Entries.FirstOrDefault(e => e.Id == SelectedAccount.Id);
            if (existing != null)
            {
                _vault.Entries.Remove(existing);
                _repo.SaveVault(_vault);
            }

            Accounts.Remove(SelectedAccount);
            SelectedAccount = Accounts.FirstOrDefault();
            RebuildCategories();
            FilteredAccounts.Refresh();
            CommandManager.InvalidateRequerySuggested();
        }

        private bool CanOpenAccount(AccountEntry? entry)
        {
            if (entry == null) return false;
            var type = NormalizeCategory(entry.AccountType);
            if (type.Equals("Website", StringComparison.OrdinalIgnoreCase))
                return !string.IsNullOrWhiteSpace(entry.Url);
            if (type.Equals("Application", StringComparison.OrdinalIgnoreCase))
                return !string.IsNullOrWhiteSpace(entry.ApplicationPath);
            return false;
        }

        private void OpenAccount(AccountEntry? entry)
        {
            if (!CanOpenAccount(entry) || entry == null) return;

            try
            {
                var type = NormalizeCategory(entry.AccountType);
                if (type.Equals("Website", StringComparison.OrdinalIgnoreCase) && entry.Url != null)
                {
                    Process.Start(new ProcessStartInfo(entry.Url) { UseShellExecute = true });
                }
                else if (type.Equals("Application", StringComparison.OrdinalIgnoreCase) && entry.ApplicationPath != null)
                {
                    if (!File.Exists(entry.ApplicationPath))
                    {
                        MessageBox.Show("Application path not found.", "Launch failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    Process.Start(new ProcessStartInfo(entry.ApplicationPath) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open entry: {ex.Message}", "Open failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EnsureSelectionVisible()
        {
            var visible = FilteredAccounts.OfType<AccountEntry>().ToList();
            if (SelectedAccount != null && visible.Contains(SelectedAccount))
                return;
            SelectedAccount = visible.FirstOrDefault();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
