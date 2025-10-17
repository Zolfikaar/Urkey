using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using PasswordManager.Core.Models;
using PasswordManager.Core.Repository;

namespace PasswordManager.WPF.ViewModels
{
    public class VaultViewModel : INotifyPropertyChanged
    {
        //private readonly _MasterPassword = VaultRepository.MasterPassword();
        //public ObservableCollection<Credential> Credentials { get; }


        //private readonly VaultRepository _repository;

        //private Vault _vault;

        public VaultViewModel()
        {
            //    _repository = new VaultRepository("vault.json", _repository.MasterPassword());
            //    _vault = _repository.LoadVault();
            //    Credentials = new ObservableCollection<Credential>(_vault.Credentials);
            //}

            //public void AddCredential(Credential credential)
            //{
            //    _vault.Credentials.Add(credential);
            //    Credentials.Add(credential);
            //    _repository.SaveVault(_vault);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
