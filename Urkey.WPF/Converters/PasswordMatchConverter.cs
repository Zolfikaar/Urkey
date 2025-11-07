using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Urkey.WPF.Converters
{
  public class PasswordMatchConverter : IMultiValueConverter
  {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      if (values.Length >= 2 && values[0] is string password && values[1] is string confirmPassword)
      {
        if (string.IsNullOrEmpty(password))
          return Brushes.Gray;

        if (password == confirmPassword)
          return Brushes.Green;
        else
          return Brushes.Red;
      }
      return Brushes.Gray;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  public class PasswordMatchTextConverter : IMultiValueConverter
  {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      if (values.Length >= 2 && values[0] is string password && values[1] is string confirmPassword)
      {
        if (string.IsNullOrEmpty(password))
          return "Enter a password";

        if (password == confirmPassword)
          return "Passwords match";
        else
          return "Passwords do not match";
      }
      return "Enter a password";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
