using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Urkey.Core.Models;
using Urkey.Core.Services;

namespace Urkey.WPF.Converters
{
    public class DocumentThumbnailConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DocumentEntry doc && !string.IsNullOrEmpty(doc.ExternalImagePath) && File.Exists(doc.ExternalImagePath))
            {
                try
                {
                    string tempPath = FileHelper.ExtractDocumentImage(doc.ExternalImagePath);
                    if (File.Exists(tempPath))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(tempPath);
                        bitmap.DecodePixelWidth = 80; // Small thumbnail size
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        return bitmap;
                    }
                }
                catch
                {
                    // Return null if image loading fails
                }
            }
            return null!;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

