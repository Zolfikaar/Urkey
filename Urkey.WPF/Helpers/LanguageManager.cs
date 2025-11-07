using System.Globalization;
using System.Windows;

namespace Urkey.WPF.Helpers
{
    public static class LanguageManager
    {
        public static void ApplyLanguage(string langCode)
        {
            var culture = new CultureInfo(langCode);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            string languageFile = $"Resources/Languages/Strings.{langCode}.xaml";
            var existing = Application.Current.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Strings."));
            if (existing != null)
                Application.Current.Resources.MergedDictionaries.Remove(existing);

            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri(languageFile, UriKind.Relative)
            });

            if (Application.Current.MainWindow != null)
            {
                Application.Current.MainWindow.FlowDirection =
                    langCode == "ar" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            }
        }
    }

}