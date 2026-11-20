using System;
using System.Runtime.CompilerServices;
using System.Windows.Interop;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using AmberBim.Views;
using Microsoft.Win32;

namespace AmberBim.ScheduleExcel
{
    [Transaction(TransactionMode.Manual)]
    public sealed class ExportSchedulesCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                if (commandData.Application.ActiveUIDocument == null) { TaskDialog.Show("AmberBIM", "Otwórz dokument Revita."); return Result.Cancelled; }
                DependencyLoader.Initialize();
                return Run(commandData.Application);
            }
            catch (Exception exception) { message = exception.Message; return Result.Failed; }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Result Run(UIApplication application)
        {
            var service = new RevitScheduleExchange(application.ActiveUIDocument.Document);
            var schedules = service.ListSchedules();
            if (schedules.Count == 0) { TaskDialog.Show("AmberBIM", "W dokumencie nie ma zestawień do eksportu."); return Result.Cancelled; }
            var window = new ScheduleExportWindow(schedules);
            new WindowInteropHelper(window).Owner = application.MainWindowHandle;
            if (window.ShowDialog() != true) return Result.Cancelled;
            var dialog = new SaveFileDialog { Filter = "Skoroszyt Excel (*.xlsx)|*.xlsx", DefaultExt = ".xlsx", AddExtension = true, FileName = ScheduleSelection.DefaultFileName(schedules), Title = "Eksport zestawień AmberBIM" };
            if (dialog.ShowDialog() != true) return Result.Cancelled;
            service.Export(window.SelectedIds, dialog.FileName);
            return Result.Succeeded;
        }
    }

    [Transaction(TransactionMode.Manual)]
    public sealed class ImportSchedulesCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var document = commandData.Application.ActiveUIDocument?.Document;
                if (document == null) { TaskDialog.Show("AmberBIM", "Otwórz dokument Revita."); return Result.Cancelled; }
                DependencyLoader.Initialize();
                return Run(commandData.Application);
            }
            catch (Exception exception) { message = exception.Message; return Result.Failed; }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Result Run(UIApplication application)
        {
            var document = application.ActiveUIDocument.Document;
            var dialog = new OpenFileDialog { Filter = "Skoroszyt Excel (*.xlsx)|*.xlsx", CheckFileExists = true, Multiselect = false, Title = "Import zestawień AmberBIM" };
            if (dialog.ShowDialog() != true) return Result.Cancelled;
            var report = new RevitScheduleExchange(document).Import(dialog.FileName);
            var window = new ScheduleImportReportWindow(report);
            new WindowInteropHelper(window).Owner = application.MainWindowHandle;
            window.ShowDialog();
            return report.Errors.Count == 0 ? Result.Succeeded : Result.Cancelled;
        }
    }
}
