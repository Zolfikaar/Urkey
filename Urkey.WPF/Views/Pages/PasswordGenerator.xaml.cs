using System.Windows;
using System.Windows.Controls;
using Urkey.Core.Services;
using Urkey.WPF.Helpers;
using Urkey.WPF.Views.Windows;

namespace Urkey.WPF.Views.Pages
{
    public partial class PasswordGenerator : Page
    {
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
            try
            {
                var options = BuildOptions();
                if (!options.UseLowercase && !options.UseUppercase && !options.UseDigits && !options.UseSymbols)
                {
                    ToastService.Warning(Loc.Get("PasswordGenerator_SelectCharset"));
                    return;
                }

                GeneratedPasswordBox.Text = PasswordGeneratorService.Generate(options);
                UpdateStrengthLabel();
            }
            catch (Exception)
            {
                ToastService.Error(Loc.Get("PasswordGenerator_Error"));
            }
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            var text = GeneratedPasswordBox.Text;
            if (string.IsNullOrWhiteSpace(text))
                return;

            ClipboardHelper.CopyText(text, App.Settings.ClipboardClearSeconds);
            ToastService.Success(Loc.Get("PasswordGenerator_Copied"));
        }

        private void SaveAsEntryButton_Click(object sender, RoutedEventArgs e)
        {
            var password = GeneratedPasswordBox.Text;
            if (string.IsNullOrWhiteSpace(password))
            {
                ToastService.Warning(Loc.Get("PasswordGenerator_GenerateFirst"));
                return;
            }

            var win = new AddAccount("Website", password)
            {
                Owner = Application.Current.MainWindow
            };

            if (win.ShowDialog() == true)
            {
                var validation = EntryValidator.ValidateAccount(win.ResultEntry);
                if (!validation.IsValid)
                {
                    ToastService.Warning(Loc.Get(validation.ErrorResourceKey!));
                    return;
                }

                App.VaultService.AddEntry(win.ResultEntry);
                ToastService.Success(Loc.Get("PasswordGenerator_Saved"));
            }
        }

        private PasswordGeneratorOptions BuildOptions() => new()
        {
            Length = (int)LengthSlider.Value,
            UseLowercase = LowercaseCheck.IsChecked == true,
            UseUppercase = UppercaseCheck.IsChecked == true,
            UseDigits = DigitsCheck.IsChecked == true,
            UseSymbols = SymbolsCheck.IsChecked == true
        };

        private void UpdateLengthLabel()
        {
            if (LengthValueText != null && LengthSlider != null)
                LengthValueText.Text = ((int)LengthSlider.Value).ToString();
        }

        private void UpdateStrengthLabel()
        {
            if (StrengthText == null) return;
            var analysis = PasswordStrengthEvaluator.Analyze(GeneratedPasswordBox.Text);
            string levelKey = analysis.Level switch
            {
                PasswordStrengthLevel.Strong => "PasswordCheck_Strength_Strong",
                PasswordStrengthLevel.Medium => "PasswordCheck_Strength_Medium",
                PasswordStrengthLevel.Weak => "PasswordCheck_Strength_Weak",
                _ => "PasswordCheck_Strength_Empty"
            };

            StrengthText.Text = Loc.Format(
                "PasswordGenerator_StrengthFormat",
                Loc.Get(levelKey),
                analysis.EntropyBits);
        }
    }
}
