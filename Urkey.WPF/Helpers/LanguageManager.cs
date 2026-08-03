using System.Globalization;
using System.Windows;

public static class LanguageManager
{
    public static void ApplyLanguage(string langCode)
    {
        // Map app language codes to full cultures for date/number formatting.
        string cultureName = langCode switch
        {
            "ar" => "ar-IQ",
            "en" => "en-US",
            _ => langCode
        };

        var culture = CultureInfo.GetCultureInfo(cultureName);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        string languageFile = $"Resources/Languages/Strings.{langCode}.xaml";

        var existing = Application.Current.Resources.MergedDictionaries
            .FirstOrDefault(d =>
                d.Source != null &&
                d.Source.OriginalString.Contains("Strings."));

        if (existing != null)
            Application.Current.Resources.MergedDictionaries.Remove(existing);

        Application.Current.Resources.MergedDictionaries.Add(
            new ResourceDictionary
            {
                Source = new Uri(languageFile, UriKind.Relative)
            });
    }
}
