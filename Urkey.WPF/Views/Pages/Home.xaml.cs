using System.Windows;
using System.Windows.Controls;
using Urkey.WPF.Helpers;
using Urkey.WPF.ViewModels;
using Urkey.WPF.Views.Windows;

namespace Urkey.WPF.Views.Pages
{
    public partial class Home : Page
    {
        private readonly HomeViewModel _vm;

        public Home()
        {
            InitializeComponent();
            _vm = new HomeViewModel(App.VaultService);
            DataContext = _vm;

            if (AddDropdown != null)
                AddDropdown.EntryAdded += (_, _) => _vm.Reload();
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            _vm.Reload();
        }

        private void OnLearnMoreClick(object sender, RoutedEventArgs e)
        {
            Navigate("PasswordCheck", new PasswordCheck());
        }

        private void OnUploadClick(object sender, RoutedEventArgs e)
        {
            EntryDialogHelper.AddDocument();
            _vm.Reload();
        }

        private void OnSetupTaskClick(object sender, RoutedEventArgs e)
        {
            string action = (sender as FrameworkElement)?.Tag as string ?? string.Empty;
            switch (action)
            {
                case "add-account":
                    if (EntryDialogHelper.AddAccount() != null)
                        _vm.Reload();
                    break;
                case "import":
                    OpenImport();
                    break;
                case "autolock":
                case "clipboard":
                    OpenSettings();
                    break;
                case "password-check":
                    Navigate("PasswordCheck", new PasswordCheck());
                    break;
            }
        }

        private static void OpenImport()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = Loc.Get("Import_SelectFileTitle"),
                Filter = Loc.Get("Import_FileFilter"),
                CheckFileExists = true
            };

            if (dialog.ShowDialog(Application.Current.MainWindow) != true)
                return;

            var win = new ImportPasswordsWindow(dialog.FileName)
            {
                Owner = Application.Current.MainWindow
            };
            win.ShowDialog();

            if (Application.Current.MainWindow is MainWindow main &&
                main.CurrentContent is Home home)
            {
                home._vm.Reload();
            }
        }

        private static void OpenSettings()
        {
            Navigate("Settings", new Settings());
        }

        private static void Navigate(string sidebarButton, Page page)
        {
            if (Application.Current.MainWindow is MainWindow main)
                main.NavigateToPage(page, sidebarButton);
        }
    }
}
