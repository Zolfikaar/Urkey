using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Urkey.Core.Models;
using Urkey.Core.Repository;

namespace Urkey.WPF.ViewModels
{
    public class VaultViewModel : INotifyPropertyChanged
    {
        private readonly VaultRepository _repo;
        private Vault _vault;

        public ObservableCollection<AccountEntry> Accounts { get; }

        // حقول الإدخال لنموذج "حساب"
        public string NewServiceName { get; set; } = string.Empty;
        public string NewUsername { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string? NewUrl { get; set; }

        public ICommand AddAccountCommand { get; }
        public ICommand SaveCommand { get; }

        public VaultViewModel(string vaultFilePath)
        {
            _repo = new VaultRepository(vaultFilePath);
            _vault = _repo.LoadVault();

            // فلترة الإدخالات على الحساب فقط
            var accounts = _vault.Entries.OfType<AccountEntry>().ToList();
            Accounts = new ObservableCollection<AccountEntry>(accounts);

            AddAccountCommand = new RelayCommand(_ => AddAccount());
            SaveCommand = new RelayCommand(_ => Save());
        }

        private void AddAccount()
        {
            var acc = new AccountEntry
            {
                Title = NewServiceName, // للعرض العام
                ServiceName = NewServiceName,
                Username = NewUsername,
                Password = NewPassword,
                Url = NewUrl
            };

            _vault.Entries.Add(acc);
            Accounts.Add(acc);

            NewServiceName = NewUsername = NewPassword = string.Empty;
            NewUrl = null;
            OnPropertyChanged(nameof(NewServiceName));
            OnPropertyChanged(nameof(NewUsername));
            OnPropertyChanged(nameof(NewPassword));
            OnPropertyChanged(nameof(NewUrl));
        }

        private void Save() => _repo.SaveVault(_vault);

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class RelayCommand : ICommand
    {
        private readonly System.Action<object?> _exec;
        private readonly System.Func<object?, bool>? _can;
        public RelayCommand(System.Action<object?> exec, System.Func<object?, bool>? can = null) { _exec = exec; _can = can; }
        public bool CanExecute(object? p) => _can?.Invoke(p) ?? true;
        public void Execute(object? p) => _exec(p);
        public event System.EventHandler? CanExecuteChanged { add { } remove { } }
    }
}
