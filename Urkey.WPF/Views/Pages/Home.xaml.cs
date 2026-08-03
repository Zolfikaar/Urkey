using System.Windows;
using System.Windows.Controls;
using Urkey.WPF.ViewModels;

namespace Urkey.WPF.Views.Pages
{
    public partial class Home : Page
    {
        private readonly HomeViewModel _vm;

        public Home()
        {
            InitializeComponent();
            _vm = new HomeViewModel(App.VaultService);
            DataContext = _vm;

            if (AddDropdown != null)
                AddDropdown.EntryAdded += (_, _) => _vm.Reload();
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            _vm.Reload();
        }
    }
}
