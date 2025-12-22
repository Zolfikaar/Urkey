using System;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.IO;
using Urkey.Core.Models;
using Urkey.Core.Services;
using System.Windows.Input;
using Microsoft.Win32;

namespace Urkey.WPF.Views.Windows
{
    public partial class AddAccount : Window
    {
        private readonly VaultService _repo = new ();
        private readonly AccountEntry _entry = new ();
        private bool _passwordShown = false;
        private readonly List<string> _passwordHistory = new();
        private string _lastPasswordSnapshot = string.Empty;
        private string? _selectedApplicationPath;
        private string? _selectedApplicationName;

        public AccountEntry ResultEntry => _entry;

        public AddAccount()
        {
            InitializeComponent();
            
            DataContext = _entry;

            // Default selection
            AccountTypeCombo.SelectedIndex = 0; // Website
            TogglePanels("Website");
            _lastPasswordSnapshot = string.Empty;
        }

        public AddAccount(string defaultType)
        {
            InitializeComponent();
            DataContext = _entry;

            string type = string.IsNullOrWhiteSpace(defaultType) ? "Website" : defaultType;
            if (string.Equals(type, "Application", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(type, "Other", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(type, "Accounts", StringComparison.OrdinalIgnoreCase))
            {
                type = "Application";
            }
            // Try select the matching item by content
            foreach (var item in AccountTypeCombo.Items)
            {
                if (item is ComboBoxItem cbi && string.Equals(cbi.Content as string, type, StringComparison.OrdinalIgnoreCase))
                {
                    AccountTypeCombo.SelectedItem = cbi;
                    TogglePanels((cbi.Content as string) ?? "Website");
                    return;
                }
            }

            // Fallback to Website
            AccountTypeCombo.SelectedIndex = 0;
            TogglePanels("Website");
            _lastPasswordSnapshot = string.Empty;
        }

        private void OnAccountTypeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AccountTypeCombo.SelectedItem is ComboBoxItem item && item.Content is string type)
            {
                TogglePanels(type);
            }
            else if (AccountTypeCombo.SelectedItem is string raw)
            {
                TogglePanels(raw);
            }
        }

        private void TogglePanels(string type)
        {
            var normalized = NormalizeType(type);
            var isWebsite = string.Equals(normalized, "Website", StringComparison.OrdinalIgnoreCase);
            var isApp = string.Equals(normalized, "Application", StringComparison.OrdinalIgnoreCase);
            var isOther = string.Equals(normalized, "Other", StringComparison.OrdinalIgnoreCase);

            WebsitePanel.Visibility = isWebsite ? Visibility.Visible : Visibility.Collapsed;
            ApplicationPanel.Visibility = isApp ? Visibility.Visible : Visibility.Collapsed;
            OtherPanel.Visibility = isOther ? Visibility.Visible : Visibility.Collapsed;
            UpdateCredentialsHeader(normalized);
            UpdateApplicationSelectionDisplay();
        }

        private void OnToggleShowPassword_Click(object sender, RoutedEventArgs e)
        {
            _passwordShown = !_passwordShown;
            if (_passwordShown)
            {
                PasswordRevealBox.Text = PasswordBox.Password;
                PasswordRevealBox.Visibility = Visibility.Visible;
                PasswordBox.Visibility = Visibility.Collapsed;
                if (sender is Button btn) btn.Content = "Hide";
            }
            else
            {
                // If user edited reveal box, sync back
                PasswordBox.Password = PasswordRevealBox.Text;
                PasswordRevealBox.Visibility = Visibility.Collapsed;
                PasswordBox.Visibility = Visibility.Visible;
                if (sender is Button btn) btn.Content = "Show";
            }
        }

        private void OnCopyPassword_Click(object sender, RoutedEventArgs e)
        {
            var pwd = _passwordShown ? PasswordRevealBox.Text : PasswordBox.Password;
            if (!string.IsNullOrEmpty(pwd))
            {
                Clipboard.SetText(pwd);
            }
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            string type = (AccountTypeCombo.SelectedItem as ComboBoxItem)?.Content as string ?? "Website";
            var normalizedType = NormalizeType(type);

            _entry.AccountType = normalizedType;
            _entry.Password = PasswordBox.Password;
            _entry.ServiceName = AccountNameTextBox.Text?.Trim() ?? string.Empty;
            _entry.Username = UsernameTextBox.Text?.Trim() ?? string.Empty;
            _entry.Email = EmailTextBox.Text?.Trim() ?? string.Empty;
            _entry.Url = string.IsNullOrWhiteSpace(WebsiteTextBox.Text) ? null : WebsiteTextBox.Text.Trim();
            _entry.LicenseKey = string.IsNullOrWhiteSpace(LicenseKeyTextBox.Text) ? null : LicenseKeyTextBox.Text.Trim();
            _entry.Category = string.IsNullOrWhiteSpace(CategoryTextBox.Text) ? null : CategoryTextBox.Text.Trim();

            // Website specific
            if (string.Equals(normalizedType, "Website", StringComparison.OrdinalIgnoreCase))
            {
                _entry.ApplicationPath = null;
            }
            else if (string.Equals(normalizedType, "Application", StringComparison.OrdinalIgnoreCase))
            {
                _entry.ApplicationPath = _selectedApplicationPath;
            }
            else if (string.Equals(normalizedType, "Other", StringComparison.OrdinalIgnoreCase))
            {
                _entry.ApplicationPath = null;
            }

            // Title mirrors ServiceName for now to be shown in lists
            if (string.IsNullOrWhiteSpace(_entry.ServiceName))
                _entry.ServiceName = _entry.ServiceName;

            // Append password history if any
            if (_passwordHistory.Count > 0)
            {
                var prefix = string.IsNullOrWhiteSpace(_entry.Notes) ? string.Empty : _entry.Notes + Environment.NewLine;
                _entry.Notes = prefix + "Password History:" + Environment.NewLine + string.Join(Environment.NewLine, _passwordHistory);
            }

            // Append comment if provided
            if (CommentTextBox?.Visibility == Visibility.Visible && !string.IsNullOrWhiteSpace(CommentTextBox.Text))
            {
                var prefix = string.IsNullOrWhiteSpace(_entry.Notes) ? string.Empty : _entry.Notes + Environment.NewLine;
                _entry.Notes = prefix + "Comment: " + CommentTextBox.Text.Trim();
            }

            if (!string.IsNullOrEmpty(_lastPasswordSnapshot) && PasswordHasChanged && !string.IsNullOrEmpty(_entry.Password))
            {
                var stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                _passwordHistory.Add($"- {stamp}: password changed");
                _lastPasswordSnapshot = _entry.Password;
            }

            _repo.AddEntry(_entry);
            DialogResult = true;
            Close();
        }

        private bool PasswordHasChanged => !string.Equals(_lastPasswordSnapshot, PasswordBox.Password, StringComparison.Ordinal);

        private void OnTogglePasswordHistory_Click(object sender, RoutedEventArgs e)
        {
            if (PwdHistoryPanel.Visibility == Visibility.Visible)
            {
                PwdHistoryPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (_passwordHistory.Count == 0)
                {
                    PwdHistoryList.ItemsSource = null;
                    PwdHistoryList.Visibility = Visibility.Collapsed;
                    PwdHistoryEmptyMessage.Visibility = Visibility.Visible;
                }
                else
                {
                    PwdHistoryEmptyMessage.Visibility = Visibility.Collapsed;
                    PwdHistoryList.ItemsSource = null;
                    PwdHistoryList.ItemsSource = _passwordHistory;
                    PwdHistoryList.Visibility = Visibility.Visible;
                }
                PwdHistoryPanel.Visibility = Visibility.Visible;
            }
        }

        private void OnAddCommentClicked(object sender, MouseButtonEventArgs e)
        {
            CommentTextBox.Visibility = CommentTextBox.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void OnChooseApplicationClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Applications (*.exe)|*.exe|All files (*.*)|*.*",
                Title = "Select application",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
            };

            if (dialog.ShowDialog(this) != true)
                return;

            _selectedApplicationPath = dialog.FileName;

            try
            {
                var info = FileVersionInfo.GetVersionInfo(dialog.FileName);
                var displayName = !string.IsNullOrWhiteSpace(info.FileDescription)
                    ? info.FileDescription
                    : Path.GetFileNameWithoutExtension(dialog.FileName);

                _selectedApplicationName = displayName;
            }
            catch
            {
                _selectedApplicationName = Path.GetFileNameWithoutExtension(dialog.FileName);
            }

            AccountNameTextBox.Text = _selectedApplicationName ?? AccountNameTextBox.Text;
            _entry.ServiceName = AccountNameTextBox.Text;

            UpdateApplicationSelectionDisplay();
        }

        private void UpdateApplicationSelectionDisplay()
        {
            bool isApplicationVisible = ApplicationPanel.Visibility == Visibility.Visible;

            if (!isApplicationVisible || string.IsNullOrWhiteSpace(_selectedApplicationPath))
            {
                SelectedAppPathText.Visibility = Visibility.Collapsed;
            }
            else
            {
                SelectedAppPathText.Text = _selectedApplicationPath;
                SelectedAppPathText.Visibility = Visibility.Visible;
            }

            if (!isApplicationVisible || string.IsNullOrWhiteSpace(_selectedApplicationName))
            {
                SelectedAppNameText.Visibility = Visibility.Collapsed;
            }
            else
            {
                SelectedAppNameText.Text = _selectedApplicationName;
                SelectedAppNameText.Visibility = Visibility.Visible;
            }
        }

        private static string NormalizeType(string type)
        {
            if (string.Equals(type, "Accounts", StringComparison.OrdinalIgnoreCase))
                return "Application";
            if (string.Equals(type, "App", StringComparison.OrdinalIgnoreCase))
                return "Application";
            return type;
        }

        private void UpdateCredentialsHeader(string normalizedType)
        {
            string resourceKey = normalizedType switch
            {
                "Application" => "ApplicationCredentialsTitle",
                "Other" => "OtherCredentialsTitle",
                _ => "WebsiteCredentialsTitle"
            };

            var localized = TryFindResource(resourceKey) as string
                            ?? Application.Current.TryFindResource(resourceKey) as string
                            ?? "Credentials";

            if (CredentialsHeader != null)
                CredentialsHeader.Text = localized;
        }
    }
}
