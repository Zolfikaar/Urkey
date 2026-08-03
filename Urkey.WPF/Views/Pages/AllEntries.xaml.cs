using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Urkey.WPF.UserControls;
using Urkey.WPF.ViewModels;

namespace Urkey.WPF.Views.Pages
{
    public partial class AllEntries : Page
    {
        private readonly AllEntriesViewModel _vm;

        public AllEntries()
        {
            InitializeComponent();
            _vm = new AllEntriesViewModel(App.VaultService);
            DataContext = _vm;

            if (AddDropdown != null)
                AddDropdown.EntryAdded += (_, _) => _vm.Reload();
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            _vm.Reload();
        }

        private void OnRowDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_vm.EditCommand.CanExecute(null))
                _vm.EditCommand.Execute(null);
        }
    }
}
