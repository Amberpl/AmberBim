#region Copyright
//
//  Copyright (C) 2021 by Mateusz Amber Ambrożewicz
//
#endregion

#region Namespace
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Media;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Windows;
using Xceed.Wpf.AvalonDock.Controls;
#endregion

#region Description
// 
// Some command usefull when you working with structural rebar
//
#endregion

namespace AmberBim.Revit
{
    [Transaction(TransactionMode.Manual)]
    class ViewTabGroup
    {
        Application _app;
        Document _doc;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Acces to app and document objects.
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            _app = uiApp.Application;
            _doc = uiDoc.Document;

            IntPtr wndHndle = IntPtr.Zero;

            wndHndle = uiApp.MainWindowHandle;

            if (wndHndle != IntPtr.Zero)
            {
                var wndSource = HwndSource.FromHwnd(wndHndle).RootVisual;
                
            }

            

            return Result.Succeeded;
        }
    }
}
