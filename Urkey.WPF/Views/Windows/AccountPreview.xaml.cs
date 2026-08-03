using System.Windows;
using Urkey.Core.Models;
using Urkey.WPF.Helpers;

namespace Urkey.WPF.Views.Windows
{
    public partial class AccountPreview : Window
    {
        private bool _passwordVisible;

        public AccountPreview()
        {
            InitializeComponent();
            DataContextChanged += (_, _) => RefreshPasswordDisplay();
            Loaded += (_, _) => RefreshPasswordDisplay();
        }

        private AccountEntry? Entry => DataContext as AccountEntry;

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void TogglePassword_Click(object sender, RoutedEventArgs e)
        {
            _passwordVisible = !_passwordVisible;
            RefreshPasswordDisplay();
        }

        private void RefreshPasswordDisplay()
        {
            var password = Entry?.Password ?? string.Empty;
            if (_passwordVisible && !string.IsNullOrEmpty(password))
            {
                PasswordMasked.Visibility = Visibility.Collapsed;
                PasswordRevealed.Visibility = Visibility.Visible;
                PasswordRevealed.Text = password;
            }
            else
            {
                PasswordMasked.Visibility = Visibility.Visible;
                PasswordRevealed.Visibility = Visibility.Collapsed;
                PasswordMasked.Text = string.IsNullOrEmpty(password) ? "—" : "••••••••••••";
            }
        }

        private void CopyPassword_Click(object sender, RoutedEventArgs e)
            => CopyField(Entry?.Password, Loc.Get("Toast_CopiedPassword"));

        private void CopyUsername_Click(object sender, RoutedEventArgs e)
            => CopyField(Entry?.Username);

        private void CopyEmail_Click(object sender, RoutedEventArgs e)
            => CopyField(Entry?.Email);

        private void CopyUrl_Click(object sender, RoutedEventArgs e)
            => CopyField(Entry?.Url);

        private void CopyField(string? value, string? successMessage = null)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            ClipboardHelper.CopyText(value, App.Settings.ClipboardClearSeconds);
            ToastService.Success(successMessage ?? Loc.Get("Toast_Copied"));
        }
    }
}
