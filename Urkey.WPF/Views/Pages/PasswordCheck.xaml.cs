using System.Windows;
using System.Windows.Controls;
using Urkey.WPF.ViewModels;

namespace Urkey.WPF.Views.Pages
{
    public partial class PasswordCheck : Page
    {
        private readonly PasswordCheckViewModel _vm;

        public PasswordCheck()
        {
            InitializeComponent();
            _vm = new PasswordCheckViewModel(App.VaultService);
            DataContext = _vm;
        }

        private async void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            await _vm.ReloadAsync();
        }
    }
}
