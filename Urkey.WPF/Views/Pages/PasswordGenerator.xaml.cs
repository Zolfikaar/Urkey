using System.Windows;
using System.Windows.Controls;
using System.Security.Cryptography;
using System.Text;
using Urkey.WPF.Helpers;

namespace Urkey.WPF.Views.Pages
{
    /// <summary>
    /// Interaction logic for PasswordGenerator.xaml
    /// </summary>
    public partial class PasswordGenerator : Page
    {
        private const string Charset = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()-_=+[]{};:,.?";

        public PasswordGenerator()
        {
            InitializeComponent();
            UpdateLengthLabel();
        }

        private void LengthSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateLengthLabel();
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            var length = (int)LengthSlider.Value;
            GeneratedPasswordBox.Text = GeneratePassword(length);
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            var text = GeneratedPasswordBox.Text;
            ClipboardHelper.CopyText(text, App.Settings.ClipboardClearSeconds);
        }

        private void UpdateLengthLabel()
        {
            if (LengthValueText != null && LengthSlider != null)
                LengthValueText.Text = ((int)LengthSlider.Value).ToString();
        }

        private static string GeneratePassword(int length)
        {
            if (length <= 0)
                return string.Empty;

            var chars = new char[length];
            for (var i = 0; i < length; i++)
            {
                var index = RandomNumberGenerator.GetInt32(Charset.Length);
                chars[i] = Charset[index];
            }

            return new string(chars);
        }
    }
}
