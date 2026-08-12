using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Urkey.Core.Models;
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

        private void OnCardTileClicked(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: CardEntry card })
            {
                _vm.SelectedItem = card;
                _vm.IsListView = true;
            }
        }
    }
}
