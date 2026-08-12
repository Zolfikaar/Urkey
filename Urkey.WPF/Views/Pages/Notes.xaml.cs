using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Urkey.Core.Models;
using Urkey.WPF.ViewModels;

namespace Urkey.WPF.Views.Pages
{
    public partial class Notes : Page
    {
        private readonly NotesViewModel _vm;

        public Notes()
        {
            InitializeComponent();
            _vm = new NotesViewModel(App.VaultService);
            DataContext = _vm;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e) => _vm.Reload();

        private void OnTileClicked(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: NoteEntry note })
                _vm.SelectedItem = note;
        }
    }
}
