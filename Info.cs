using System;
using System.Windows.Interop;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using AmberBim.Views;

namespace AmberBim
{
    [Transaction(TransactionMode.Manual)]
    public class Info : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var window = new AboutWindow();
                new WindowInteropHelper(window).Owner = commandData.Application.MainWindowHandle;
                window.ShowDialog();
                return Result.Succeeded;
            }
            catch (Exception exception)
            {
                message = exception.Message;
                return Result.Failed;
            }
        }
    }
}
