using System.Windows;

namespace Urkey.WPF.Helpers
{
    public static class Loc
    {
        public static string Get(string key, string? fallback = null)
        {
            var value = Application.Current?.TryFindResource(key) as string;
            if (!string.IsNullOrWhiteSpace(value))
                return value;
            return fallback ?? key;
        }

        public static string Format(string key, params object[] args)
        {
            var template = Get(key);
            try
            {
                return string.Format(template, args);
            }
            catch
            {
                return template;
            }
        }
    }
}
