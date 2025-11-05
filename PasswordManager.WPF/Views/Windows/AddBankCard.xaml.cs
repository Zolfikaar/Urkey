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
    /// Interaction logic for AddBankCard.xaml
    /// </summary>
    public partial class AddBankCard : Window
    {
        public AddBankCard()
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
        public string CardName => CardNameTextBox.Text;
        public string CardNumber => CardNumberBox.Password;
        public string Expiry => ExpiryTextBox.Text;
        public string Cvv => CvvBox.Password;
        public string Bank => BankTextBox.Text;
        public string Notes => NotesTextBox.Text;

        // Method to set initial values
        public void SetValues(string cardName = "", string cardNumber = "", string expiry = "", string cvv = "", string bank = "", string notes = "")
        {
            CardNameTextBox.Text = cardName;
            CardNumberBox.Password = cardNumber;
            ExpiryTextBox.Text = expiry;
            CvvBox.Password = cvv;
            BankTextBox.Text = bank;
            NotesTextBox.Text = notes;
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CardNameTextBox.Text))
            {
                MessageBox.Show("Card Name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // جمع القيم من الحقول
            string cardName = CardNameTextBox.Text;
            string cardNumber = CardNumberBox.Password;
            string expiry = ExpiryTextBox.Text;
            string cvv = CvvBox.Password;
            string bank = BankTextBox.Text;
            string notes = NotesTextBox.Text;

            Close();
        }
    }
}
