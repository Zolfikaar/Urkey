using System.Windows;
using Urkey.Core.Models;
using Urkey.Core.Services;
using Urkey.WPF.Helpers;

namespace Urkey.WPF.Views.Windows
{
    public partial class AddAddress : Window
    {
        public AddressEntry? Result { get; private set; }

        public AddAddress()
        {
            InitializeComponent();
        }

        public new string Name => NameTextBox.Text;
        public string Street => StreetTextBox.Text;
        public string City => CityTextBox.Text;
        public string Governorate => GovernorateTextBox.Text;
        public string Zip => ZipTextBox.Text;
        public string Country => CountryTextBox.Text;
        public string Notes => NotesTextBox.Text;

        public void SetValues(string name = "", string street = "", string city = "", string governorate = "", string zip = "", string country = "", string notes = "")
        {
            NameTextBox.Text = name;
            StreetTextBox.Text = street;
            CityTextBox.Text = city;
            GovernorateTextBox.Text = governorate;
            ZipTextBox.Text = zip;
            CountryTextBox.Text = country;
            NotesTextBox.Text = notes;
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            var entry = new AddressEntry
            {
                Name = NameTextBox.Text?.Trim() ?? string.Empty,
                Street = StreetTextBox.Text?.Trim() ?? string.Empty,
                City = CityTextBox.Text?.Trim() ?? string.Empty,
                Governorate = GovernorateTextBox.Text?.Trim() ?? string.Empty,
                ZipCode = string.IsNullOrWhiteSpace(ZipTextBox.Text) ? null : ZipTextBox.Text.Trim(),
                Country = CountryTextBox.Text?.Trim() ?? string.Empty,
                Notes = NotesTextBox.Text?.Trim() ?? string.Empty
            };

            var validation = EntryValidator.ValidateAddress(entry);
            if (!validation.IsValid)
            {
                ToastService.Warning(Loc.Get(validation.ErrorResourceKey!));
                return;
            }

            Result = entry;
            DialogResult = true;
            Close();
        }
    }
}
