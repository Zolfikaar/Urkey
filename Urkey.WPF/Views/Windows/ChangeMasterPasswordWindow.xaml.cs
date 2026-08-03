using System.Windows;
using Urkey.Core.Services;
using Urkey.WPF.Helpers;

namespace Urkey.WPF.Views.Windows
{
    public partial class ChangeMasterPasswordWindow : Window
    {
        public ChangeMasterPasswordWindow()
        {
            InitializeComponent();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            string current = CurrentPasswordBox.Password;
            string next = NewPasswordBox.Password;
            string confirm = ConfirmPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(current) || string.IsNullOrWhiteSpace(next))
            {
                ToastService.Warning(Loc.Get("ChangeMasterPassword_Required"));
                return;
            }

            if (next.Length < 4)
            {
                ToastService.Warning(Loc.Get("Setup_Error_InvalidPassword"));
                return;
            }

            if (!string.Equals(next, confirm, StringComparison.Ordinal))
            {
                ToastService.Warning(Loc.Get("Setup_Error_PasswordMismatch"));
                return;
            }

            try
            {
                var userService = new UserService();
                bool ok = userService.ChangeMasterPassword(current, next);
                if (!ok)
                {
                    ToastService.Error(Loc.Get("ChangeMasterPassword_Failed"));
                    return;
                }

                // Keep app vault in sync with re-encrypted on-disk vault.
                App.VaultService.Load();

                CurrentPasswordBox.Password = string.Empty;
                NewPasswordBox.Password = string.Empty;
                ConfirmPasswordBox.Password = string.Empty;

                ToastService.Success(Loc.Get("ChangeMasterPassword_Success"));

                DialogResult = true;
                Close();
            }
            catch (Exception)
            {
                ToastService.Error(Loc.Get("ChangeMasterPassword_Failed"));
            }
        }
    }
}
