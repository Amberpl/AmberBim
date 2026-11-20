using System;
using System.Runtime.CompilerServices;
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
                DependencyLoader.Initialize();
                ShowWindow(commandData.Application);
                return Result.Succeeded;
            }
            catch (Exception exception)
            {
                message = exception.Message;
                return Result.Failed;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ShowWindow(UIApplication application)
        {
            var window = new AboutWindow();
            new WindowInteropHelper(window).Owner = application.MainWindowHandle;
            window.ShowDialog();
        }
    }
}
