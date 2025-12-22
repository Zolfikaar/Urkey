using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Urkey.Core.Services;
using Urkey.Core.Models;

namespace Urkey.WPF.ViewModels
{
    public class VaultViewModel : ViewModelBase
    {
        private VaultService _vaultService ;
        //private Vault _vault;

        public VaultViewModel()
        {
            _vaultService = new VaultService();
            //_vault = _vaultService.Load();
        }

        public void SaveVault()
        {
            _vaultService.Save();
        }

        public bool VaultExists()
        {
            return _vaultService.VaultExists();
        }

        public string GetVaultPath()
        {
           return _vaultService.GetVaultPath();
        }

        public string GetVaultDirectory()
        {
           return _vaultService.GetVaultDirectory();
        }

        public Vault LoadVault() => _vaultService.LoadVault();
        
    }
}

