using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Urkey.WPF.Converters
{
    /// <summary>
    /// Formats DateTime/DateOnly with the active UI culture (short date by default).
    /// ConverterParameter: "d" (default), "g", "MM/yy", etc.
    /// </summary>
    public sealed class LocaleDateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var format = parameter as string;
            if (string.IsNullOrWhiteSpace(format))
                format = "d";

            var uiCulture = CultureInfo.CurrentCulture;

            return value switch
            {
                DateTime dt => dt.ToString(format, uiCulture),
                DateOnly d => d.ToString(format, uiCulture),
                DateTimeOffset dto => dto.ToString(format, uiCulture),
                _ => string.Empty
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    /// <summary>
    /// Formats numbers/counts with the active culture.
    /// </summary>
    public sealed class LocaleNumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var uiCulture = CultureInfo.CurrentCulture;
            return value switch
            {
                IFormattable f => f.ToString(parameter as string, uiCulture) ?? string.Empty,
                null => string.Empty,
                _ => System.Convert.ToString(value, uiCulture) ?? string.Empty
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
