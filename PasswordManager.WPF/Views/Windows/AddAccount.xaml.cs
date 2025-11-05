using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PasswordManager.WPF.Views.Windows
{
    /// <summary>
    /// Interaction logic for AddAccount.xaml
    /// </summary>
    public partial class AddAccount : Window
    {
        public AddAccount()
        {
            InitializeComponent();
            this.FlowDirection = App.Settings.Language == "ar"
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            var currentTheme = SystemParameters.HighContrast ? "Dark" : "Light";
        }

        // Exposed properties for easy access
        public string AccountName => AccountNameTextBox.Text;
        public string Username => UsernameTextBox.Text;
        public string Password => PasswordBox.Password;
        public string Email => EmailTextBox.Text;
        public string Website => WebsiteTextBox.Text;
        public string Notes => NotesTextBox.Text;

        // Method to set initial values
        public bool SaveValues(string accountName = "", string username = "", string password = "", string email = "", string website = "", string notes = "")
        {
            if(string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(accountName))
            {
                return false;
            } else
            {
                return true;
            }
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            var _isSaved = false;

            if (string.IsNullOrWhiteSpace(this.Username) || string.IsNullOrWhiteSpace(this.Email))
            {
                MessageBox.Show("(User Name, Email) Fields can't be empty", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            else
            {
                _isSaved = SaveValues(AccountName, Username, Email, Password, Website, Notes);
            }

            if (_isSaved)
            {
                MessageBox.Show("(User Name, Email) Fields can't be empty", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
                return;

            }
            else
            {

                MessageBox.Show("Couldn't save the info, something went wrong", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            }
        }
    }
}
