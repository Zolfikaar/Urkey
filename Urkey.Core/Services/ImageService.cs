using System;
using System.IO;
using System.Windows.Media.Imaging;

public static class ImageService
{
    public static string ToBase64(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        return Convert.ToBase64String(bytes);
    }

    public static string FromBase64(string base64, string outputPath)
    {
        byte[] bytes = Convert.FromBase64String(base64);
        File.WriteAllBytes(outputPath, bytes);
        return outputPath;
    }
}
