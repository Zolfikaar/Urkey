using System.Windows;
using System.Windows.Controls;
using Urkey.WPF.ViewModels;
using Urkey.WPF.Views.Windows;

namespace Urkey.WPF.Views.Pages
{
    public partial class Documents : Page
    {
        private readonly DocumentsViewModel _DocVM;

        public Documents()
        {
            InitializeComponent();

            _DocVM = new DocumentsViewModel();
            DataContext = _DocVM;

            // Apply language direction
            this.FlowDirection = App.Settings.Language == "ar"
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            // Reload documents when the page is shown to get the latest data
            var currentSelection = _DocVM.SelectedDocument;
            _DocVM.Reload();
            // Restore selection if it still exists
            if (currentSelection != null && _DocVM.Documents.Contains(currentSelection))
            {
                _DocVM.SelectedDocument = currentSelection;
            }
        }

        private void OnAddDocument_Click(object sender, RoutedEventArgs e)
        {
            var win = new AddDocument
            {
                Owner = Application.Current.MainWindow
            };

            // ✅ افتح نافذة الإضافة كـ Dialog
            bool? result = win.ShowDialog();

            // ✅ إذا تمت الإضافة بنجاح
            if (result == true && win.Document is not null)
            {
                // نحفظ الوثيقة الجديدة مباشرة عبر ViewModel الحالي
                _DocVM.SaveNewDocument(win.Document);

                // وبعد الحفظ نعيد تحميل الوثائق
                _DocVM.Reload();
            }
        }

        private void ToggleMoreOptionsMenu(object sender, RoutedEventArgs e)
        {
            MoreOptionsPopup.IsOpen = !MoreOptionsPopup.IsOpen;
        }

        private void ShowDetails_Click(object sender, RoutedEventArgs e)
        {
            if (_DocVM.SelectedDocument == null) return;

            MoreOptionsPopup.IsOpen = false;
            
            var detailsWindow = new DocumentDetails(_DocVM.SelectedDocument)
            {
                Owner = Application.Current.MainWindow
            };
            detailsWindow.ShowDialog();
        }

        private void PreviewImage_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_DocVM.SelectedDocument == null) return;
            
            _DocVM.PreviewCommand.Execute(_DocVM.SelectedDocument);
        }
    }
}
