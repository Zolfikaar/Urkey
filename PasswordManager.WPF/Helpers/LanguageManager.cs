using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;

namespace PasswordManager.WPF.Helpers
{
    public enum LangCode
    {
        en,
        ar
    }
    public static class LanguageManager
    {
        private static readonly string _defaultLanguage = "en";
        private static LangCode _currentLanguage = LangCode.en;

        public static string DefaultLanguage => _defaultLanguage;
        public static LangCode CurrentLanguage => _currentLanguage;

        public static void ChangeLanguage(LangCode lang)
        {
            string cultureCode = lang.ToString();
            _currentLanguage = lang;

            // Save language preference
            SaveLanguagePreference(lang);

            // Set culture
            var culture = new CultureInfo(cultureCode);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            // Determine the appropriate language file
            string languageFile = $"Resources/Languages/Strings.{cultureCode}.xaml";

            // Remove current language resource (Strings.*)
            var existingLang = Application.Current.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Strings."));
            if (existingLang != null)
                Application.Current.Resources.MergedDictionaries.Remove(existingLang);

            // Load new language resource
            var newDict = new ResourceDictionary
            {
                Source = new Uri(languageFile, UriKind.Relative)
            };
            Application.Current.Resources.MergedDictionaries.Add(newDict);

            // Change text direction (Right-to-Left for Arabic)
            if (Application.Current.MainWindow != null)
            {
                Application.Current.MainWindow.FlowDirection =
                    lang == LangCode.ar ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            }
        }

        // Load saved language preference on startup
        public static void ApplyDefaultLanguage()
        {
            var savedLanguage = LoadLanguagePreference();
            ChangeLanguage(savedLanguage);
        }

        // Save language preference to a simple text file
        private static void SaveLanguagePreference(LangCode lang)
        {
            try
            {
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "language.txt");
                File.WriteAllText(settingsPath, lang.ToString());
                Console.WriteLine($"Successfully saved language preference: {lang} to {settingsPath}");
                System.Diagnostics.Debug.WriteLine($"Successfully saved language preference: {lang} to {settingsPath}");
            }
            catch (Exception ex)
            {
                // Log error if needed, but don't crash the app
                Console.WriteLine($"Failed to save language preference: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Failed to save language preference: {ex.Message}");
            }
        }

        // Load language preference from a simple text file
        private static LangCode LoadLanguagePreference()
        {
            try
            {
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "language.txt");
                Console.WriteLine($"Looking for language file at: {settingsPath}");

                if (File.Exists(settingsPath))
                {
                    var savedLanguage = File.ReadAllText(settingsPath).Trim();
                    Console.WriteLine($"Loading language preference from file: '{savedLanguage}'");
                    System.Diagnostics.Debug.WriteLine($"Loading language preference from file: '{savedLanguage}'");

                    if (!string.IsNullOrEmpty(savedLanguage) && Enum.TryParse<LangCode>(savedLanguage, out var lang))
                    {
                        Console.WriteLine($"Successfully loaded language: {lang}");
                        System.Diagnostics.Debug.WriteLine($"Successfully loaded language: {lang}");
                        return lang;
                    }
                    else
                    {
                        Console.WriteLine($"Failed to parse language from file: '{savedLanguage}'");
                        System.Diagnostics.Debug.WriteLine($"Failed to parse language from file: '{savedLanguage}'");
                    }
                }
                else
                {
                    Console.WriteLine($"Language settings file not found: {settingsPath}");
                    System.Diagnostics.Debug.WriteLine($"Language settings file not found: {settingsPath}");
                }
            }
            catch (Exception ex)
            {
                // Log error if needed, but don't crash the app
                Console.WriteLine($"Failed to load language preference: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Failed to load language preference: {ex.Message}");
            }

            // Return default language if loading fails
            Console.WriteLine($"Using default language: {_defaultLanguage}");
            System.Diagnostics.Debug.WriteLine($"Using default language: {_defaultLanguage}");
            return Enum.TryParse<LangCode>(_defaultLanguage, out var defaultLang) ? defaultLang : LangCode.en;
        }
    }
}
