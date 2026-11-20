using System;
using System.Windows;
using AmberBim.ScheduleExcel;
using FluentWindow = Wpf.Ui.Controls.FluentWindow;

namespace AmberBim.Views
{
    public partial class ScheduleImportReportWindow : FluentWindow
    {
        public ScheduleImportReportWindow(ImportReport report)
        {
            InitializeComponent();
            StatusText.Text = report.Errors.Count == 0 ? "Import zakończony" : "Import zablokowany — nie zapisano zmian";
            CountsText.Text = "Zmienione elementy lub typy: " + report.ChangedElements +
                "\nZmienione wartości: " + report.ChangedValues + "\nPominięte arkusze: " + report.SkippedSheets.Count;
            var details = "";
            if (report.Errors.Count != 0) details = "Błędy" + Environment.NewLine + string.Join(Environment.NewLine, report.Errors);
            if (report.SkippedSheets.Count != 0) details += (details.Length == 0 ? "" : "\n\n") +
                "Pominięte arkusze" + Environment.NewLine + string.Join(Environment.NewLine, report.SkippedSheets);
            DetailsText.Text = details.Length == 0 ? "Brak błędów i pominiętych arkuszy." : details;
        }

        private void CloseReport(object sender, RoutedEventArgs e) { Close(); }
    }
}
