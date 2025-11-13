using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Urkey.Core.Models;
using Urkey.Core.Repository;

namespace Urkey.WPF.Views.Windows
{
    public partial class EditAccount : Window
    {
        private readonly VaultRepository _repo = new();
        private AccountEntry _entry = new();

        public EditAccount()
        {
            InitializeComponent();
            DataContext = _entry;
            InitializeUiFromEntry();
        }

        public EditAccount(AccountEntry entry)
        {
            InitializeComponent();
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

            // Select type
            string type = string.IsNullOrWhiteSpace(_entry.AccountType) ? "Website" : _entry.AccountType;
            if (AccountTypeCombo.Items.OfType<ComboBoxItem>().FirstOrDefault(i => (i.Content as string) == type) is ComboBoxItem toSelect)
            {
                AccountTypeCombo.SelectedItem = toSelect;
            }
            else
            {
                AccountTypeCombo.SelectedIndex = 0;
                type = "Website";
            }

            // Fill type-specific
            if (type == "Website")
            {
                WebsiteTextBox.Text = _entry.Url ?? string.Empty;
            }
            else if (type == "Application")
            {
                // Best-effort parse from notes
                AppNameTextBox.Text = _entry.ServiceName;
                if (!string.IsNullOrWhiteSpace(_entry.Notes))
                {
                    const string prefix = "License: ";
                    var line = _entry.Notes.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).FirstOrDefault(l => l.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
                    if (line != null)
                        LicenseKeyTextBox.Text = line.Substring(prefix.Length).Trim();
                }
            }
            else if (type == "Other")
            {
                // Try parse Category: from notes
                if (!string.IsNullOrWhiteSpace(_entry.Notes))
                {
                    const string prefix = "Category: ";
                    var line = _entry.Notes.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).FirstOrDefault(l => l.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
                    if (line != null)
                        CategoryTextBox.Text = line.Substring(prefix.Length).Trim();
                }
            }

            TogglePanels(type);
        }

        private void OnAccountTypeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AccountTypeCombo.SelectedItem is ComboBoxItem item && item.Content is string type)
            {
                TogglePanels(type);
            }
        }

        private void TogglePanels(string type)
        {
            WebsitePanel.Visibility = type == "Website" ? Visibility.Visible : Visibility.Collapsed;
            ApplicationPanel.Visibility = type == "Application" ? Visibility.Visible : Visibility.Collapsed;
            OtherPanel.Visibility = type == "Other" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            string type = (AccountTypeCombo.SelectedItem as ComboBoxItem)?.Content as string ?? "Website";

            _entry.ServiceName = AccountNameTextBox.Text;
            _entry.Username = UsernameTextBox.Text;
            _entry.Password = PasswordBox.Password;
            _entry.Email = EmailTextBox.Text;
            _entry.Notes = NotesTextBox.Text;
            _entry.AccountType = type;

            if (type == "Website")
            {
                _entry.Url = WebsiteTextBox.Text;
            }
            else if (type == "Application")
            {
                if (string.IsNullOrWhiteSpace(_entry.ServiceName) && !string.IsNullOrWhiteSpace(AppNameTextBox.Text))
                    _entry.ServiceName = AppNameTextBox.Text;
                if (!string.IsNullOrWhiteSpace(LicenseKeyTextBox.Text))
                {
                    var prefix = string.IsNullOrWhiteSpace(_entry.Notes) ? string.Empty : _entry.Notes + Environment.NewLine;
                    _entry.Notes = prefix + $"License: {LicenseKeyTextBox.Text}";
                }
            }
            else if (type == "Other")
            {
                if (!string.IsNullOrWhiteSpace(CategoryTextBox.Text))
                {
                    var prefix = string.IsNullOrWhiteSpace(_entry.Notes) ? string.Empty : _entry.Notes + Environment.NewLine;
                    _entry.Notes = prefix + $"Category: {CategoryTextBox.Text}";
                }
            }

            if (string.IsNullOrWhiteSpace(_entry.Title))
                _entry.Title = _entry.ServiceName;

            // Persist changes
            _repo.SaveVault(_repo.LoadVault());
            DialogResult = true;
            Close();
        }
    }
}
