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
    /// Interaction logic for AddAddress.xaml
    /// </summary>
    public partial class AddAddress : Window
    {
        public AddAddress()
        {
            InitializeComponent();
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
        public string Name => NameTextBox.Text;
        public string Street => StreetTextBox.Text;
        public string City => CityTextBox.Text;
        public string State => StateTextBox.Text;
        public string Zip => ZipTextBox.Text;
        public string Country => CountryTextBox.Text;
        public string Notes => NotesTextBox.Text;

        // Method to set initial values
        public void SetValues(string name = "", string street = "", string city = "", string state = "", string zip = "", string country = "", string notes = "")
        {
            NameTextBox.Text = name;
            StreetTextBox.Text = street;
            CityTextBox.Text = city;
            StateTextBox.Text = state;
            ZipTextBox.Text = zip;
            CountryTextBox.Text = country;
            NotesTextBox.Text = notes;
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // جمع القيم من الحقول
            string name = NameTextBox.Text;
            string street = StreetTextBox.Text;
            string city = CityTextBox.Text;
            string state = StateTextBox.Text;
            string zip = ZipTextBox.Text;
            string country = CountryTextBox.Text;
            string notes = NotesTextBox.Text;

            Close();
        }
    }
}
