using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Urkey.WPF.Converters
{
    public class FlowDirectionToHorizontalAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FlowDirection direction)
                return direction == FlowDirection.RightToLeft
                    ? HorizontalAlignment.Right
                    : HorizontalAlignment.Left;

            return HorizontalAlignment.Left;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

}
