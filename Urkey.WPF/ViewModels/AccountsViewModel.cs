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
using Urkey.Core.Services;
using Urkey.WPF.Commands;
using Urkey.WPF.Views.Windows;

namespace Urkey.WPF.ViewModels
{
    public class AccountsViewModel : ViewModelBase
    {
        private const string AllCategoriesLabel = "All categories";

        private readonly VaultService _vaultService;
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
                _searchText = value ?? string.Empty;
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
                _selectedCategory = string.IsNullOrWhiteSpace(value) ? AllCategoriesLabel : value;
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

        public AccountsViewModel(VaultService vaultService)
        {
            _vaultService = vaultService;

            _vault = _vaultService.LoadVault();

            Accounts = new ObservableCollection<AccountEntry>(
                _vault.Entries.OfType<AccountEntry>()
            );

            Accounts.CollectionChanged += OnAccountsCollectionChanged;

            FilteredAccounts = CollectionViewSource.GetDefaultView(Accounts);
            FilteredAccounts.Filter = OnFilterAccount;

            RebuildCategories();

            ReloadCommand = new RelayCommand<object>(_ => Reload());
            ToggleFavoriteCommand = new RelayCommand<object>(_ => ToggleFavorite(), _ => SelectedAccount != null);
            EditAccountCommand = new RelayCommand<object>(_ => EditSelectedAccount(), _ => SelectedAccount != null);
            DeleteAccountCommand = new RelayCommand<object>(_ => DeleteSelectedAccount(), _ => SelectedAccount != null);
            OpenAccountCommand = new RelayCommand<AccountEntry>(OpenAccount, CanOpenAccount);
        }

        private void Reload()
        {
            _vault = _vaultService.LoadVault();

            Accounts.CollectionChanged -= OnAccountsCollectionChanged;
            Accounts.Clear();

            foreach (var acc in _vault.Entries.OfType<AccountEntry>())
                Accounts.Add(acc);

            Accounts.CollectionChanged += OnAccountsCollectionChanged;

            RebuildCategories();
            FilteredAccounts.Refresh();
        }

        private void ToggleFavorite()
        {
            if (SelectedAccount == null) return;

            SelectedAccount.IsFavorite = !SelectedAccount.IsFavorite;
            _vaultService.Save();

            FilteredAccounts.Refresh();
            EnsureSelectionVisible();
        }

        private void EditSelectedAccount()
        {
            if (SelectedAccount == null) return;

            var editor = new EditAccount(SelectedAccount)
            {
                Owner = Application.Current.MainWindow
            };

            if (editor.ShowDialog() == true)
            {
                _vaultService.Save();
                Reload();
            }
        }

        private void DeleteSelectedAccount()
        {
            if (SelectedAccount == null) return;

            var result = MessageBox.Show(
                $"Delete account \"{SelectedAccount.ServiceName}\"?",
                "Confirm delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            var target = _vault.Entries.FirstOrDefault(e => e.Id == SelectedAccount.Id);
            if (target != null)
            {
                _vault.Entries.Remove(target);
                _vaultService.Save();
            }

            Accounts.Remove(SelectedAccount);
            SelectedAccount = Accounts.FirstOrDefault();
            RebuildCategories();
            FilteredAccounts.Refresh();
        }

        private bool OnFilterAccount(object obj)
        {
            if (obj is not AccountEntry entry) return false;

            if (_selectedCategory != AllCategoriesLabel &&
                !NormalizeCategory(entry.AccountType)
                    .Equals(_selectedCategory, StringComparison.OrdinalIgnoreCase))
                return false;

            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                var text = _searchText.Trim();
                return
                    entry.ServiceName?.Contains(text, StringComparison.OrdinalIgnoreCase) == true ||
                    entry.Username?.Contains(text, StringComparison.OrdinalIgnoreCase) == true ||
                    entry.Email?.Contains(text, StringComparison.OrdinalIgnoreCase) == true ||
                    entry.Url?.Contains(text, StringComparison.OrdinalIgnoreCase) == true ||
                    entry.ApplicationPath?.Contains(text, StringComparison.OrdinalIgnoreCase) == true;
            }

            return true;
        }

        private void RebuildCategories()
        {
            Categories.Clear();
            Categories.Add(AllCategoriesLabel);

            foreach (var cat in Accounts
                .Select(a => NormalizeCategory(a.AccountType))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c))
            {
                Categories.Add(cat);
            }
        }

        private static string NormalizeCategory(string? category)
            => string.IsNullOrWhiteSpace(category) ? "Website" : category;

        private bool CanOpenAccount(AccountEntry? entry)
        {
            if (entry == null) return false;

            var type = NormalizeCategory(entry.AccountType);
            return type == "Website"
                ? !string.IsNullOrWhiteSpace(entry.Url)
                : !string.IsNullOrWhiteSpace(entry.ApplicationPath);
        }

        private void OpenAccount(AccountEntry? entry)
        {
            if (!CanOpenAccount(entry) || entry == null) return;

            try
            {
                if (!string.IsNullOrWhiteSpace(entry.Url))
                    Process.Start(new ProcessStartInfo(entry.Url) { UseShellExecute = true });
                else if (!string.IsNullOrWhiteSpace(entry.ApplicationPath))
                    Process.Start(new ProcessStartInfo(entry.ApplicationPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Open failed");
            }
        }

        private void OnAccountsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RebuildCategories();
            FilteredAccounts.Refresh();
        }

        private void EnsureSelectionVisible()
        {
            var visible = FilteredAccounts.OfType<AccountEntry>().ToList();
            if (!visible.Contains(SelectedAccount))
                SelectedAccount = visible.FirstOrDefault();
        }
    }
}
