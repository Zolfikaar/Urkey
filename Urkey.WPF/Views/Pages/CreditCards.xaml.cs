using System.Windows;
using System.Windows.Controls;
using Urkey.WPF.ViewModels;

namespace Urkey.WPF.Views.Pages
{
    public partial class CreditCards : Page
    {
        private readonly CardsViewModel _vm;

        public CreditCards()
        {
            InitializeComponent();
            _vm = new CardsViewModel(App.VaultService);
            DataContext = _vm;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e) => _vm.Reload();
    }
}
