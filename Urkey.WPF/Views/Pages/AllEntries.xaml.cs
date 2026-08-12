using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

        private void OnTileClicked(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: EntryListItem item })
                _vm.SelectedEntry = item;
        }

        private void OnTileMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: EntryListItem item })
                _vm.SelectedEntry = item;
        }

        private void OnTileActionClick(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: EntryListItem item })
                _vm.SelectedEntry = item;
        }
    }
}
