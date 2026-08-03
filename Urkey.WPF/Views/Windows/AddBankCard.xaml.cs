using System.Windows;
using Urkey.Core.Models;
using Urkey.Core.Services;
using Urkey.WPF.Helpers;

namespace Urkey.WPF.Views.Windows
{
    public partial class AddBankCard : Window
    {
        public CardEntry? Result { get; private set; }

        public AddBankCard()
        {
            InitializeComponent();
        }

        public string CardName => CardNameTextBox.Text;
        public string CardNumber => CardNumberBox.Password;
        public string Expiry => ExpiryTextBox.Text;
        public string Cvv => CvvBox.Password;
        public string Pin => PinBox.Password;
        public string Notes => NotesTextBox.Text;

        public void SetValues(
            string cardName = "",
            string cardNumber = "",
            string expiry = "",
            string cvv = "",
            string pin = "",
            string notes = "")
        {
            CardNameTextBox.Text = cardName;
            CardNumberBox.Password = cardNumber;
            ExpiryTextBox.Text = expiry;
            CvvBox.Password = cvv;
            PinBox.Password = pin;
            NotesTextBox.Text = notes;
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            var entry = new CardEntry
            {
                HolderName = CardNameTextBox.Text?.Trim() ?? string.Empty,
                Number = new string((CardNumberBox.Password ?? string.Empty).Where(char.IsDigit).ToArray()),
                Cvv = string.IsNullOrWhiteSpace(CvvBox.Password) ? null : CvvBox.Password.Trim(),
                Pin = string.IsNullOrWhiteSpace(PinBox.Password) ? null : PinBox.Password.Trim(),
                Notes = NotesTextBox.Text?.Trim() ?? string.Empty
            };

            if (!string.IsNullOrWhiteSpace(ExpiryTextBox.Text))
            {
                if (!EntryValidator.TryParseCardExpiry(ExpiryTextBox.Text, out var expiry))
                {
                    ToastService.Warning(Loc.Get("Validation_InvalidExpiry"));
                    return;
                }
                entry.ExpiryDate = expiry;
            }

            var validation = EntryValidator.ValidateCard(entry);
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
