#region Copyright
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
using Autodesk.Revit.UI;
#endregion

#region Description
// 
// Info about AmberBIM add-in.
//
#endregion

namespace AmberBim
{
    [Transaction(TransactionMode.Manual)]
    public class Info : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            TaskDialog infoDialog = new TaskDialog("About");
            infoDialog.Title = "AmberBim";
            infoDialog.MainContent = 
                "AmberBIM tools v. 0.2 \n" +
                "Copyright (C) 2020-2021 \n" +
                "Mateusz Amber Ambrożewicz\n";
            infoDialog.FooterText =
                "<a href=\"http://www.amberbim.wordpress.com \">"
                + "www.amberbim.wordpress.com</a>";

            infoDialog.Show();
            return Result.Succeeded;
        }
    }
}