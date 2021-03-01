﻿#region Copyright
//
//  Copyright (C) 2020 by Mateusz Amber Ambrożewicz
//
#endregion

#region Namespace
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
#endregion

#region Description
// 
// Some command usefull when you working with structural rebar
//
#endregion

namespace AmberBim
{
    [Transaction(TransactionMode.Manual)]
    public class RebarNotSolidInView : IExternalCommand
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

            // Getting all instance of rebar in active view
            FilteredElementCollector rebar = new FilteredElementCollector(_doc, _doc.ActiveView.Id).OfCategory(BuiltInCategory.OST_Rebar).WhereElementIsNotElementType();

            // Throw exeption when list of rebars is empty
            if (rebar == null)
            {
                throw new ArgumentNullException("No rebars in view");
            }

            // Getting active view from _doc
            View activeView = _doc.ActiveView;
            View3D activeView3D = null;


            // Active View as View3D for SetSolidInView method
            if (activeView is View3D)
            {
                activeView3D = _doc.ActiveView as View3D;
            }
            else
            {
                return Result.Cancelled;
            }

            // Set solid in view
            Transaction T = new Transaction(_doc);
            T.Start("Start setting rebar as not a solid");

            foreach (Autodesk.Revit.DB.Structure.Rebar rebElem in rebar)
            {
                rebElem.SetSolidInView(activeView3D, false);
            }

            T.Commit();

            return Result.Succeeded;
        }
    }
}
