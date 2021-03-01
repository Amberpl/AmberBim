#region Copyright
//
//  Copyright (C) 2020 by Mateusz Amber Ambrożewicz
//
#endregion

#region Namespace
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
#endregion

#region Description
// 
// This comand reload selected Cad File
//
#endregion

namespace AmberBim
{
    [Transaction(TransactionMode.Manual)]
    public class ReloadCadFile : IExternalCommand
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

            try
            {
                // Select some elements in Revit before invoking this command


                // Get the element selection of current document.
                Selection selection = uiDoc.Selection;
                ICollection<ElementId> selectedIds = uiDoc.Selection.GetElementIds();

                if (0 == selectedIds.Count)
                {
                    // If no elements selected.
                    TaskDialog.Show("AmberBIM", "You haven't selected any elements.");
                }
                else
                {
                    foreach (ElementId id in selectedIds)
                    {
                        Element element = GetSelectedElementTypeId(id);
                        if (TestIdAsAExternalFile(element) && TestIdAsCADLinkType(element))
                        {
                            Transaction T = new Transaction(_doc);
                            T.Start("Reload Cad file");
                                CadLinkReloadDialog(element);
                            T.Commit();
                        }
                        else if (!TestIdAsAExternalFile(element))
                        {
                            TaskDialog.Show("AmberBIM", "This is not a external file.");
                        }
                        else
                        {
                            TaskDialog.Show("AmberBIM", "Something went wrong.");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                message = e.Message;
                return Result.Failed;
            }

            return Result.Succeeded;
        }
        private void ReloadSelectedCadFile(Element element)
        {
            try
            {
                CADLinkType cadLinkElem = element as CADLinkType;
                cadLinkElem.Reload();
            }
            catch (Exception e)
            {
            }
        }

        private Element GetSelectedElementTypeId(ElementId id)
        {
            Element element = _doc.GetElement(id);
            Element cadFileTypeId = _doc.GetElement(element.GetTypeId());

            return cadFileTypeId;
        }

        private bool TestIdAsAExternalFile(Element element)
        {
            if (element.IsExternalFileReference())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool TestIdAsCADLinkType(Element element)
        {
            if (element.GetType() == typeof(CADLinkType))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void CadLinkReloadDialog(Element element)
        {
            TaskDialog cadLinkDialog = new TaskDialog("AmberBIM");
            cadLinkDialog.MainInstruction = "Do you want to reload this file:";
            cadLinkDialog.MainContent =
                element.Name.ToString();

            // Add commmandLink options to task dialog
            cadLinkDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1,
                                        "Continue");
            cadLinkDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2,
                                        "Cancel");

            TaskDialogResult tResult = cadLinkDialog.Show();

            // Set common buttons and default button. If no CommonButton or CommandLink is added,
            // task dialog will show a Close button by default
            cadLinkDialog.CommonButtons = TaskDialogCommonButtons.Close;
            cadLinkDialog.DefaultButton = TaskDialogResult.Close;

            // If the user clicks the first command link, the CadLink will be open
            if (TaskDialogResult.CommandLink1 == tResult)
            {
                ReloadSelectedCadFile(element);
            }

            // If the user clicks the second command link, it stop a operation
            else if (TaskDialogResult.CommandLink2 == tResult)
            {

            }
        }
    }
}