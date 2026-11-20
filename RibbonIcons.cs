using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace AmberBim
{
    internal static class RibbonIcons
    {
        private static readonly Dictionary<string, BitmapSource> Cache = new Dictionary<string, BitmapSource>();

        internal static BitmapSource GetImage(string name, bool dark, int size)
        {
            var path = $"Ico/{(dark ? "Dark" : "Light")}/{name}{size}.png";
            lock (Cache)
            {
                if (!Cache.TryGetValue(path, out var image))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri("pack://application:,,,/AmberBim;component/" + path);
                    bitmap.EndInit();
                    bitmap.Freeze();
                    image = bitmap;
                    Cache.Add(path, image);
                }
                return image;
            }
        }

        internal static void Apply(PushButton button, string name, bool dark)
        {
            button.Image = GetImage(name, dark, 16);
            button.LargeImage = GetImage(name, dark, 32);
        }
    }
}
