using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace PasswordManager.Core.Services
{
    public static class ImageService
    {
        /// <summary>
        /// Converts an image file to a Base64 string.
        /// </summary>
        public static string ToBase64(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
                throw new FileNotFoundException("Image file not found.", imagePath);

            byte[] bytes = File.ReadAllBytes(imagePath);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Converts a Base64 string back to an image and saves it to a file.
        /// </summary>
        public static string FromBase64(string base64, string outputPath)
        {
            byte[] bytes = Convert.FromBase64String(base64);
            File.WriteAllBytes(outputPath, bytes);
            return outputPath;
        }

        /// <summary>
        /// Converts a Base64 string to a BitmapImage (for preview in WPF).
        /// </summary>
        public static BitmapImage Base64ToBitmap(string base64)
        {
            byte[] bytes = Convert.FromBase64String(base64);
            using var ms = new MemoryStream(bytes);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = ms;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
    }
}
