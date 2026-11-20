using System;
using System.Windows;

namespace AmberBim.UiGallery
{
    public partial class App : Application
    {
        private void StartGallery(object sender, StartupEventArgs e)
        {
            var gallery = new GalleryWindow();
            MainWindow = gallery;
            gallery.Show();
            if (Array.IndexOf(e.Args, "--compact-info") >= 0)
                new CompactAboutWindow { Owner = gallery }.ShowDialog();
        }
    }
}
