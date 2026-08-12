using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Urkey.Core.Models;
using Urkey.WPF.ViewModels;

namespace Urkey.WPF.Views.Pages
{
    public partial class Documents : Page
    {
        private readonly DocumentsViewModel _DocVM;

        public Documents()
        {
            InitializeComponent();

            _DocVM = new DocumentsViewModel(App.VaultService);
            DataContext = _DocVM;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            _DocVM.Reload();
        }

        private void OnDocumentTileClicked(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: DocumentEntry doc })
                _DocVM.SelectedDocument = doc;
        }

        private void ToggleMoreOptionsMenu(object sender, RoutedEventArgs e)
        {
            MoreOptionsPopup.IsOpen = !MoreOptionsPopup.IsOpen;
        }

        private void ShowDetails_Click(object sender, RoutedEventArgs e)
        {
            if (_DocVM.SelectedDocument == null) return;

            MoreOptionsPopup.IsOpen = false;

            var detailsWindow = new Windows.DocumentDetails(_DocVM.SelectedDocument)
            {
                Owner = Application.Current.MainWindow
            };
            detailsWindow.ShowDialog();
        }

        private void PreviewImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_DocVM.SelectedDocument == null) return;

            _DocVM.PreviewCommand.Execute(_DocVM.SelectedDocument);
        }
    }
}
