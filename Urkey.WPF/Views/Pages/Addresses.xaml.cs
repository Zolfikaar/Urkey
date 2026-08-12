using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Urkey.Core.Models;
using Urkey.WPF.ViewModels;

namespace Urkey.WPF.Views.Pages
{
    public partial class Addresses : Page
    {
        private readonly AddressesViewModel _vm;

        public Addresses()
        {
            InitializeComponent();
            _vm = new AddressesViewModel(App.VaultService);
            DataContext = _vm;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e) => _vm.Reload();

        private void OnTileClicked(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: AddressEntry address })
                _vm.SelectedItem = address;
        }
    }
}
