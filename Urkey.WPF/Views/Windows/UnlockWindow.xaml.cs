using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Urkey.Core.Services;
using Urkey.WPF.Helpers;
using Urkey.WPF.ViewModels;

namespace Urkey.WPF.Views.Windows
{
    /// <summary>
    /// Interaction logic for UnlockWindow.xaml
    /// </summary>
    /// 

    public partial class UnlockWindow
    {
        private bool _isPasswordVisible ;
        private string _enteredMasterPassword = string.Empty;
        // private readonly Vault _vault;
        private UserViewModel _userVM;

        public UnlockWindow()
        {
            InitializeComponent();
            Loaded += UnlockWindow_Loaded;


            // Bind password visibility
            // // The '_' parameters are required for the lambda to match the event signature,
            // // but they are intentionally ignored because they are not needed.
            MasterPasswordBox.PasswordChanged += (_, _) =>
            {
                if (!_isPasswordVisible)
                    MasterPasswordTextBox.Text = MasterPasswordBox.Password;
            };

            MasterPasswordTextBox.TextChanged += (_, _) =>
            {
                if (_isPasswordVisible)
                    MasterPasswordBox.Password = MasterPasswordTextBox.Text;
            };

            _enteredMasterPassword = MasterPasswordTextBox.Text;
            
            _userVM = new UserViewModel();
            
            
            
        }

        private void UnlockWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Apply language and set FlowDirection
            var langCode = App.Settings.Language;
            LanguageManager.ApplyLanguage(langCode);

            FlowDirection = langCode == "ar" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

            // Position settings button based on language
            // In RTL, button should be on left; in LTR, on right
            SettingsButton.HorizontalAlignment = langCode == "ar"
                ? HorizontalAlignment.Left
                : HorizontalAlignment.Right;

            // Apply font based on language
            var fontFamily = langCode == "ar" ? new FontFamily("Cairo") : new FontFamily("LeagueSpartan");
            ApplyFontToWindow(this, fontFamily);

            // Set focus on password field
            MasterPasswordBox.Focus();
        }

        private void ApplyFontToWindow(DependencyObject parent, FontFamily fontFamily)
        {
            // if (parent == null) return; // "Expression is always false according to nullable reference types' annotations" // so we don't need it at all

            // Apply font to current element based on type
            if (parent is TextBlock textBlock)
            {
                textBlock.FontFamily = fontFamily;
            }
            else if (parent is TextBox textBox)
            {
                textBox.FontFamily = fontFamily;
            }
            else if (parent is PasswordBox passwordBox)
            {
                passwordBox.FontFamily = fontFamily;
            }
            else if (parent is Button button)
            {
                button.FontFamily = fontFamily;
            }
            else if (parent is Label label)
            {
                label.FontFamily = fontFamily;
            }
            else if (parent is Control control)
            {
                control.FontFamily = fontFamily;
            }
            else if (parent is Window window)
            {
                window.FontFamily = fontFamily;
            }

            // Recursively apply to children
            var childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                ApplyFontToWindow(child, fontFamily);
            }
        }
        
        private void TogglePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            if (_isPasswordVisible)
            {
                MasterPasswordTextBox.Text = MasterPasswordBox.Password;
                MasterPasswordTextBox.Visibility = Visibility.Visible;
                MasterPasswordBox.Visibility = Visibility.Collapsed;
                MasterPasswordTextBox.Focus();
            }
            else
            {
                MasterPasswordBox.Password = MasterPasswordTextBox.Text;
                MasterPasswordBox.Visibility = Visibility.Visible;
                MasterPasswordTextBox.Visibility = Visibility.Collapsed;
                MasterPasswordBox.Focus();
            }
        }

        private void TogglePasswordArea_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            TogglePasswordButton_Click(TogglePasswordButton, new RoutedEventArgs());
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                UnlockButton_Click(sender, e);
            }
        }

        private void UnlockButton_Click(object sender, RoutedEventArgs e)
        {
            var isPasswordCorrect = _userVM.Unlock(_enteredMasterPassword);

            if (!isPasswordCorrect)
            {
                MessageBox.Show(
                    "Master Password is incorrect.",
                    "Info",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            if (VaultService.VaultExists())
                VaultService.LoadVault();
            else
                VaultService.CreateVault();
            
            var mainWindow = new MainWindow();
            mainWindow.Show();

            this.Close();
        }

        private void ForgotPasswordLink_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // TODO: Implement forgot password logic
            MessageBox.Show("Forgot password functionality will be implemented here.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsPopup.IsOpen = !SettingsPopup.IsOpen;
        }
    }
}
