using System.Windows;
using System.Windows.Controls;
using Urkey.Core.Models;
using Urkey.Core.Services;
using Urkey.WPF.Helpers;

namespace Urkey.WPF.Views.Windows
{
    public partial class EditAccount : Window
    {
        private readonly VaultService _vaultService;
        private AccountEntry _entry = new();
        private bool _passwordVisible;

        public EditAccount()
        {
            InitializeComponent();
            _vaultService = App.VaultService;
            DataContext = _entry;
            InitializeUiFromEntry();
        }

        public EditAccount(AccountEntry entry)
        {
            InitializeComponent();
            _vaultService = App.VaultService;
            _entry = entry;
            DataContext = _entry;
            InitializeUiFromEntry();
        }

        private void InitializeUiFromEntry()
        {
            AccountNameTextBox.Text = _entry.ServiceName;
            UsernameTextBox.Text = _entry.Username;
            EmailTextBox.Text = _entry.Email;
            NotesTextBox.Text = _entry.Notes;
            PasswordBox.Password = _entry.Password ?? string.Empty;
            PasswordRevealBox.Text = _entry.Password ?? string.Empty;

            string type = string.IsNullOrWhiteSpace(_entry.AccountType) ? "Website" : _entry.AccountType;
            var match = AccountTypeCombo.Items.OfType<ComboBoxItem>()
                .FirstOrDefault(i => string.Equals(i.Tag as string, type, StringComparison.OrdinalIgnoreCase)
                                     || string.Equals(i.Content as string, type, StringComparison.OrdinalIgnoreCase));
            AccountTypeCombo.SelectedItem = match ?? AccountTypeCombo.Items[0];
            type = (AccountTypeCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "Website";

            if (type == "Website")
                WebsiteTextBox.Text = _entry.Url ?? string.Empty;
            else if (type == "Application")
            {
                AppNameTextBox.Text = _entry.ServiceName;
                if (!string.IsNullOrWhiteSpace(_entry.Notes))
                {
                    const string prefix = "License: ";
                    var line = _entry.Notes.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                        .FirstOrDefault(l => l.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
                    if (line != null)
                        LicenseKeyTextBox.Text = line[prefix.Length..].Trim();
                }
            }
            else if (type == "Other" && !string.IsNullOrWhiteSpace(_entry.Notes))
            {
                const string prefix = "Category: ";
                var line = _entry.Notes.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                    .FirstOrDefault(l => l.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
                if (line != null)
                    CategoryTextBox.Text = line[prefix.Length..].Trim();
            }

            TogglePanels(type);
        }

        private void OnAccountTypeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AccountTypeCombo.SelectedItem is ComboBoxItem item)
            {
                var type = item.Tag as string
                           ?? item.Content as string
                           ?? "Website";
                TogglePanels(type);
            }
        }

        private void TogglePanels(string type)
        {
            WebsitePanel.Visibility = type == "Website" ? Visibility.Visible : Visibility.Collapsed;
            ApplicationPanel.Visibility = type is "Application" or "Accounts" ? Visibility.Visible : Visibility.Collapsed;
            OtherPanel.Visibility = type == "Other" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnToggleShowPassword_Click(object sender, RoutedEventArgs e)
        {
            _passwordVisible = !_passwordVisible;
            if (_passwordVisible)
            {
                PasswordRevealBox.Text = PasswordBox.Password;
                PasswordBox.Visibility = Visibility.Collapsed;
                PasswordRevealBox.Visibility = Visibility.Visible;
            }
            else
            {
                PasswordBox.Password = PasswordRevealBox.Text;
                PasswordRevealBox.Visibility = Visibility.Collapsed;
                PasswordBox.Visibility = Visibility.Visible;
            }
        }

        private void OnCopyPassword_Click(object sender, RoutedEventArgs e)
        {
            var password = _passwordVisible ? PasswordRevealBox.Text : PasswordBox.Password;
            if (string.IsNullOrWhiteSpace(password))
                return;

            ClipboardHelper.CopyText(password, App.Settings.ClipboardClearSeconds);
            ToastService.Success(Loc.Get("Toast_CopiedPassword"));
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            string type = (AccountTypeCombo.SelectedItem as ComboBoxItem)?.Tag as string
                          ?? (AccountTypeCombo.SelectedItem as ComboBoxItem)?.Content as string
                          ?? "Website";

            _entry.ServiceName = AccountNameTextBox.Text;
            _entry.Username = UsernameTextBox.Text;
            _entry.Password = _passwordVisible ? PasswordRevealBox.Text : PasswordBox.Password;
            _entry.Email = EmailTextBox.Text;
            _entry.Notes = NotesTextBox.Text;
            _entry.AccountType = type;

            if (type == "Website")
            {
                _entry.Url = WebsiteTextBox.Text;
            }
            else if (type is "Application" or "Accounts")
            {
                if (string.IsNullOrWhiteSpace(_entry.ServiceName) && !string.IsNullOrWhiteSpace(AppNameTextBox.Text))
                    _entry.ServiceName = AppNameTextBox.Text;
                if (!string.IsNullOrWhiteSpace(LicenseKeyTextBox.Text))
                {
                    var prefix = string.IsNullOrWhiteSpace(_entry.Notes) ? string.Empty : _entry.Notes + Environment.NewLine;
                    _entry.Notes = prefix + $"License: {LicenseKeyTextBox.Text}";
                }
            }
            else if (type == "Other" && !string.IsNullOrWhiteSpace(CategoryTextBox.Text))
            {
                var prefix = string.IsNullOrWhiteSpace(_entry.Notes) ? string.Empty : _entry.Notes + Environment.NewLine;
                _entry.Notes = prefix + $"Category: {CategoryTextBox.Text}";
            }

            _vaultService.Save();
            DialogResult = true;
            Close();
        }
    }
}
