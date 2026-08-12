using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Urkey.WPF.Converters
{
    /// <summary>
    /// Maps FlowDirection to a trailing-corner HorizontalAlignment when the target
    /// does <c>not</c> inherit RTL (FlowDirection forced to LTR).
    /// Prefer inheriting FlowDirection and using HorizontalAlignment=Right instead —
    /// WPF mirrors "Right" to the visual left in RTL automatically.
    /// </summary>
    public class FlowDirectionToHorizontalAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FlowDirection direction)
                return direction == FlowDirection.RightToLeft
                    ? HorizontalAlignment.Left
                    : HorizontalAlignment.Right;

            return HorizontalAlignment.Right;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
