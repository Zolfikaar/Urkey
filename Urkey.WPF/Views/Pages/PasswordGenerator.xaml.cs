using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Urkey.Core.Services;
using Urkey.WPF.Helpers;
using Urkey.WPF.Views.Windows;

namespace Urkey.WPF.Views.Pages
{
    public partial class PasswordGenerator : Page
    {
        private string _plainPassword = string.Empty;
        private bool _isPasswordVisible = true;

        public PasswordGenerator()
        {
            InitializeComponent();
            UpdateLengthLabel();
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_plainPassword))
                GeneratePassword();
        }

        private void LengthSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateLengthLabel();
            if (IsLoaded)
                GeneratePassword();
        }

        private void Charset_Changed(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
                GeneratePassword();
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
            => GeneratePassword();

        private void GeneratePassword()
        {
            try
            {
                var options = BuildOptions();
                if (!options.UseLowercase && !options.UseUppercase && !options.UseDigits && !options.UseSymbols)
                {
                    ToastService.Warning(Loc.Get("PasswordGenerator_SelectCharset"));
                    return;
                }

                _plainPassword = PasswordGeneratorService.Generate(options);
                RefreshPasswordDisplay();
                UpdateStrengthLabel();
            }
            catch (Exception)
            {
                ToastService.Error(Loc.Get("PasswordGenerator_Error"));
            }
        }

        private void ToggleVisibility_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;
            RefreshPasswordDisplay();

            if (VisibilityIcon != null)
                VisibilityIcon.Data = (Geometry)FindResource(_isPasswordVisible ? "bx_hide" : "bx_eye");

            if (ToggleVisibilityButton != null)
                ToggleVisibilityButton.ToolTip = Loc.Get(_isPasswordVisible
                    ? "PasswordGenerator_Hide"
                    : "PasswordGenerator_Show");
        }

        private void RefreshPasswordDisplay()
        {
            if (PasswordDisplay == null) return;
            if (string.IsNullOrEmpty(_plainPassword))
            {
                PasswordDisplay.Text = string.Empty;
                return;
            }

            PasswordDisplay.Text = _isPasswordVisible
                ? _plainPassword
                : new string('•', Math.Min(_plainPassword.Length, 32));
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_plainPassword))
                return;

            ClipboardHelper.CopyText(_plainPassword, App.Settings.ClipboardClearSeconds);
            ToastService.Success(Loc.Get("PasswordGenerator_Copied"));
        }

        private void SaveAsEntryButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_plainPassword))
            {
                ToastService.Warning(Loc.Get("PasswordGenerator_GenerateFirst"));
                return;
            }

            var win = new AddAccount("Website", _plainPassword)
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
            var analysis = PasswordStrengthEvaluator.Analyze(_plainPassword);
            string levelKey = analysis.Level switch
            {
                PasswordStrengthLevel.Strong => "PasswordCheck_Strength_Strong",
                PasswordStrengthLevel.Medium => "PasswordCheck_Strength_Medium",
                PasswordStrengthLevel.Weak => "PasswordCheck_Strength_Weak",
                _ => "PasswordCheck_Strength_Empty"
            };

            var level = Loc.Get(levelKey);
            StrengthText.Text = Loc.Format("PasswordGenerator_StrengthFormat", level);
            StrengthText.Foreground = analysis.Level switch
            {
                PasswordStrengthLevel.Strong => (Brush)FindResource("SuccessColor"),
                PasswordStrengthLevel.Medium => (Brush)FindResource("AccentGold"),
                PasswordStrengthLevel.Weak => (Brush)FindResource("ErrorColor"),
                _ => (Brush)FindResource("TextMutedColor")
            };
        }
    }
}
