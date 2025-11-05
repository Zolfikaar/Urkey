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
    /// Interaction logic for AddDocument.xaml
    /// </summary>
    public partial class AddDocument : Window
    {
        public AddDocument()
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
        public string DocumentName => DocumentNameTextBox.Text;
        public string Type => TypeTextBox.Text;
        public string Number => NumberTextBox.Text;
        public string Issuer => IssuerTextBox.Text;
        public string Expiry => ExpiryTextBox.Text;
        public string Notes => NotesTextBox.Text;

        // Method to set initial values
        public void SetValues(string documentName = "", string type = "", string number = "", string issuer = "", string expiry = "", string notes = "")
        {
            DocumentNameTextBox.Text = documentName;
            TypeTextBox.Text = type;
            NumberTextBox.Text = number;
            IssuerTextBox.Text = issuer;
            ExpiryTextBox.Text = expiry;
            NotesTextBox.Text = notes;
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DocumentNameTextBox.Text))
            {
                MessageBox.Show("Document Name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // جمع القيم من الحقول
            string documentName = DocumentNameTextBox.Text;
            string type = TypeTextBox.Text;
            string number = NumberTextBox.Text;
            string issuer = IssuerTextBox.Text;
            string expiry = ExpiryTextBox.Text;
            string notes = NotesTextBox.Text;

            Close();
        }
    }
}
