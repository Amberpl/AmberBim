using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using FluentWindow = Wpf.Ui.Controls.FluentWindow;

namespace AmberBim.Views
{
    public partial class AboutWindow : FluentWindow
    {
        public AboutWindow()
        {
            InitializeComponent();
            var version = typeof(AboutWindow).Assembly.GetName().Version;
            VersionText.Text = $"Wersja {version.Major}.{version.Minor}";
            CopyrightText.Text = $"Copyright © 2020–{DateTime.Now.Year}";
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void HandlePreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                Close();
            }
        }

        private void OpenWebsite(object sender, RequestNavigateEventArgs e)
        {
            e.Handled = true;
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            }
            catch (Exception exception) when (exception is Win32Exception || exception is InvalidOperationException)
            {
                System.Windows.MessageBox.Show(this, "Nie można otworzyć strony w przeglądarce.\n" + exception.Message,
                    "AmberBIM", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
