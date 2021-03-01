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
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion

namespace AmberBim
{
    public class AmberBimRibbon : IExternalApplication
    {
        const string _dllExtension = ".dll";
        const string _addinName = "AmberBim";

        // class instance
        internal static AmberBimRibbon thisApp = null;

        /// <summary>
        /// Location of managed dll
        /// </summary>
        string _manDllPath;

        /// <summary>
        /// OnShoutdown() - Called when Revit ends.
        /// </summary>
        public Result OnShutdown(UIControlledApplication app)
        {
            return Result.Succeeded;
        }

        /// <summary>
        /// OnStartup() - called when Revit starts.
        /// </summary>
        public Result OnStartup(UIControlledApplication app)
        {
            // Externail application directory
            string dir = Path.GetDirectoryName(
                System.Reflection.Assembly
                .GetExecutingAssembly().Location);

            // External command path:
            _manDllPath = Path.Combine(dir, _addinName + _dllExtension);
            if (!File.Exists(_manDllPath))
            {
                TaskDialog.Show("AmberBIM", "External command assembly not found at directory: " + _manDllPath);
                return Result.Failed;
            }

            thisApp = this;

            AddRibbon(app);

            return Result.Succeeded;
        }

        /// <summary>
        /// Criate ribbon panel
        /// </summary>
        /// 
        public void AddRibbon( UIControlledApplication app)
        {
            app.CreateRibbonTab("AmberBIM");

            RibbonPanel rebarPanel = app.CreateRibbonPanel("AmberBIM","Rebar");
            RibbonPanel cadPanel = app.CreateRibbonPanel("AmberBIM", "CAD");
            RibbonPanel infoPanel = app.CreateRibbonPanel("AmberBIM","Info");

            AddPushButton(rebarPanel);
            AddCadButton(cadPanel);
            AddInfoButton(infoPanel);
        }

        public void AddPushButton(RibbonPanel panel)
        {
            PushButtonData pushButtonSolid = new PushButtonData("SetSolidInView", "Solid", _manDllPath, _addinName + ".RebarSolidInView");
            //PushButton pushButton1 = panel.AddItem(pushButtonSolid) as PushButton;

            pushButtonSolid.Image = new BitmapImage(new Uri("pack://application:,,,/AmberBim;component/Ico/Solid16.png"));
            pushButtonSolid.ToolTip = "Set all rebar visible in view as a solid.\n" +
                "Works only in 3D view.";


            PushButtonData pushButtonNotSolid = new PushButtonData("SetNotSolid", "Not Solid", _manDllPath, _addinName + ".RebarNotSolidInView");
            //PushButton pushButton2 = panel.AddItem(pushButtonNotSolid) as PushButton;

            pushButtonNotSolid.Image = new BitmapImage(new Uri("pack://application:,,,/AmberBim;component/Ico/NotSolid16.png"));
            pushButtonNotSolid.ToolTip = "Set all rebar visible in view as a not solid";

            IList<RibbonItem> stackedButtons1 = panel.AddStackedItems(pushButtonSolid, pushButtonNotSolid);

            PushButtonData pushButtonUnobscured = new PushButtonData("SetUnobscured", "Unobscured", _manDllPath, _addinName + ".RebarUnobscuredInView");
            //PushButton pushButton3 = panel.AddItem(pushButtonUnobscured) as PushButton;

            pushButtonUnobscured.Image = new BitmapImage(new Uri("pack://application:,,,/AmberBim;component/Ico/Unobscured16.png"));
            pushButtonUnobscured.ToolTip = "Set all rebar visible in view as a unobscured";

            PushButtonData pushButtonObscured = new PushButtonData("SetObscured", "Obscured", _manDllPath, _addinName + ".RebarObscuredInView");
            //PushButton pushButton4 = panel.AddItem(pushButtonObscured) as PushButton;

            pushButtonObscured.Image = new BitmapImage(new Uri("pack://application:,,,/AmberBim;component/Ico/Obscured16.png"));
            pushButtonObscured.ToolTip = "Set all rebar visible in view as a obscured";

            IList<RibbonItem> stackedButtons2 = panel.AddStackedItems(pushButtonUnobscured, pushButtonObscured);
        }
        public void AddCadButton(RibbonPanel panel)
        {
            PushButtonData pushButtonCadOpen = new PushButtonData("OpenCadFile", "Open DWG file", _manDllPath, _addinName + ".OpenCadFile");
            PushButton pushButton1 = panel.AddItem(pushButtonCadOpen) as PushButton;

            pushButton1.LargeImage = new BitmapImage(new Uri("pack://application:,,,/AmberBim;component/Ico/DwgOpen32.png"));
            pushButton1.ToolTip = "Open selected DWG file in external program";

            PushButtonData pushButtonCadReload = new PushButtonData("ReloadCadFile", "Reload DWG file", _manDllPath, _addinName + ".ReloadCadFile");
            PushButton pushButton2 = panel.AddItem(pushButtonCadReload) as PushButton;

            pushButton2.LargeImage = new BitmapImage(new Uri("pack://application:,,,/AmberBim;component/Ico/DwgReload32.png"));
            pushButton2.ToolTip = "Reload selected DWG file";
        }
        public void AddInfoButton(RibbonPanel panel)
        {
            PushButtonData pushButtonInfo = new PushButtonData("Info", "About", _manDllPath, _addinName + ".Info");
            PushButton pushButton1 = panel.AddItem(pushButtonInfo) as PushButton;

            pushButton1.LargeImage = new BitmapImage(new Uri("pack://application:,,,/AmberBim;component/Ico/info.png"));
            pushButton1.ToolTip = "About AmberBIM";
        }
    }
}
