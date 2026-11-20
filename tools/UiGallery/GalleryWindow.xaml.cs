using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AmberBim.Views;
using FluentWindow = Wpf.Ui.Controls.FluentWindow;

namespace AmberBim.UiGallery
{
    public partial class GalleryWindow : FluentWindow
    {
        public GalleryWindow()
        {
            InitializeComponent();
            UpdateSelection();
        }

        private void SelectVariant(object sender, SelectionChangedEventArgs e)
        {
            if (VariantTitle != null)
                UpdateSelection();
        }

        private void UpdateSelection()
        {
            var compact = Variants.SelectedIndex == 1;
            VariantTitle.Text = compact ? "Info · kompaktowe" : "Info · obecne";
            VariantDescription.Text = compact
                ? "Ikona obok nazwy i wersji. Dane autora zebrane w jednym miejscu, mniejsze odstępy."
                : "Aktualne okno informacji o dodatku AmberBIM.";
        }

        private void OpenPreview(object sender, RoutedEventArgs e)
        {
            ShowPreview();
        }

        private void OpenSelectedVariant(object sender, MouseButtonEventArgs e)
        {
            if (ItemsControl.ContainerFromElement(Variants, e.OriginalSource as DependencyObject) != null)
                ShowPreview();
        }

        private void ShowPreview()
        {
            Window preview = Variants.SelectedIndex == 1
                ? (Window)new CompactAboutWindow()
                : new AboutWindow();
            preview.Owner = this;
            preview.ShowDialog();
        }
    }
}
