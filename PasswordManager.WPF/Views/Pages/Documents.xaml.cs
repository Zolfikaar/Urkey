using System.Windows;
using System.Windows.Controls;
using PasswordManager.WPF.ViewModels;
using PasswordManager.WPF.Views.Windows;

namespace PasswordManager.WPF.Views.Pages
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
    }
}
