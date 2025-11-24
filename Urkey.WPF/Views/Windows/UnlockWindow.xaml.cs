using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Urkey.WPF.Helpers;

namespace Urkey.WPF.Views.Windows
{
    /// <summary>
    /// Interaction logic for UnlockWindow.xaml
    /// </summary>
    public partial class UnlockWindow : Window
    {
        private bool _isPasswordVisible = false;

        public UnlockWindow()
        {
            InitializeComponent();
            Loaded += UnlockWindow_Loaded;

            // Bind password visibility
            MasterPasswordBox.PasswordChanged += (s, e) =>
            {
                if (!_isPasswordVisible)
                    MasterPasswordTextBox.Text = MasterPasswordBox.Password;
            };

            MasterPasswordTextBox.TextChanged += (s, e) =>
            {
                if (_isPasswordVisible)
                    MasterPasswordBox.Password = MasterPasswordTextBox.Text;
            };
        }

        private void UnlockWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Apply language and set FlowDirection
            var langCode = App.Settings?.Language ?? "en";
            LanguageManager.ApplyLanguage(langCode);

            FlowDirection = langCode == "ar" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

            // Position settings button based on language
            // In RTL, button should be on left; in LTR, on right
            SettingsButton.HorizontalAlignment = langCode == "ar"
                ? HorizontalAlignment.Left
                : HorizontalAlignment.Right;

            // Set focus on password field
            MasterPasswordBox.Focus();
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
            // TODO: Implement unlock logic
            string password = _isPasswordVisible ? MasterPasswordTextBox.Text : MasterPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Please enter your master password.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Here you would verify the password and unlock the vault
            // For now, just show a message
            MessageBox.Show("Unlock functionality will be implemented here.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
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
