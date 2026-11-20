using System.Collections.Generic;
using System.IO;
using Autodesk.Revit.UI;
#if REVIT_THEME_SUPPORT
using Autodesk.Revit.UI.Events;
#endif

namespace AmberBim
{
    public class AmberBimRibbon : IExternalApplication
    {
        internal static AmberBimRibbon thisApp;
        private readonly Dictionary<PushButton, string> ribbonIcons = new Dictionary<PushButton, string>();
        private string assemblyPath;
        private bool? darkIcons;
#if REVIT_THEME_SUPPORT
        private UIControlledApplication themeApplication;
#endif

        public Result OnStartup(UIControlledApplication app)
        {
            DependencyLoader.Initialize();
            assemblyPath = typeof(AmberBimRibbon).Assembly.Location;
            if (!File.Exists(assemblyPath))
            {
                TaskDialog.Show("AmberBIM", "External command assembly not found at directory: " + assemblyPath);
                return Result.Failed;
            }

            thisApp = this;
            AddRibbon(app);
#if REVIT_THEME_SUPPORT
            ApplyIcons(UIThemeManager.CurrentTheme == UITheme.Dark);
            themeApplication = app;
            themeApplication.ThemeChanged += OnThemeChanged;
#else
            ApplyIcons(false);
#endif
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication app)
        {
#if REVIT_THEME_SUPPORT
            if (themeApplication != null)
            {
                themeApplication.ThemeChanged -= OnThemeChanged;
                themeApplication = null;
            }
#endif
            ribbonIcons.Clear();
            darkIcons = null;
            thisApp = null;
            DependencyLoader.Shutdown();
            return Result.Succeeded;
        }

        public void AddRibbon(UIControlledApplication app)
        {
            app.CreateRibbonTab("AmberBIM");
            var rebarPanel = app.CreateRibbonPanel("AmberBIM", "Rebar");
            var cadPanel = app.CreateRibbonPanel("AmberBIM", "CAD");
            var excelPanel = app.CreateRibbonPanel("AmberBIM", "Excel");
            var infoPanel = app.CreateRibbonPanel("AmberBIM", "Info");
            AddPushButton(rebarPanel);
            AddCadButton(cadPanel);
            AddExcelButtons(excelPanel);
            AddInfoButton(infoPanel);
        }

        public void AddPushButton(RibbonPanel panel)
        {
#if REVIT_LEGACY_REBAR_SOLID
            var solid = new PushButtonData("SetSolidInView", "Solid", assemblyPath, "AmberBim.RebarSolidInView")
            {
                ToolTip = "Set all rebar visible in view as a solid.\nWorks only in 3D view."
            };
            var notSolid = new PushButtonData("SetNotSolid", "Not Solid", assemblyPath, "AmberBim.RebarNotSolidInView")
            {
                ToolTip = "Set all rebar visible in view as a not solid"
            };
            var solidButtons = panel.AddStackedItems(solid, notSolid);
            Track((PushButton)solidButtons[0], "Solid");
            Track((PushButton)solidButtons[1], "NotSolid");
#endif
            var unobscured = new PushButtonData("SetUnobscured", "Unobscured", assemblyPath, "AmberBim.RebarUnobscuredInView")
            {
                ToolTip = "Set all rebar visible in view as a unobscured"
            };
            var obscured = new PushButtonData("SetObscured", "Obscured", assemblyPath, "AmberBim.RebarObscuredInView")
            {
                ToolTip = "Set all rebar visible in view as a obscured"
            };
            var visibilityButtons = panel.AddStackedItems(unobscured, obscured);
            Track((PushButton)visibilityButtons[0], "Unobscured");
            Track((PushButton)visibilityButtons[1], "Obscured");
        }

        public void AddCadButton(RibbonPanel panel)
        {
            var open = new PushButtonData("OpenCadFile", "Open DWG file", assemblyPath, "AmberBim.OpenCadFile")
            {
                ToolTip = "Open selected DWG file in external program"
            };
            Track((PushButton)panel.AddItem(open), "DwgOpen");
            var reload = new PushButtonData("ReloadCadFile", "Reload DWG file", assemblyPath, "AmberBim.ReloadCadFile")
            {
                ToolTip = "Reload selected DWG file"
            };
            Track((PushButton)panel.AddItem(reload), "DwgReload");
        }

        public void AddInfoButton(RibbonPanel panel)
        {
            var info = new PushButtonData("Info", "About", assemblyPath, "AmberBim.Info")
            {
                ToolTip = "About AmberBIM"
            };
            Track((PushButton)panel.AddItem(info), "About");
        }

        private void Track(PushButton button, string icon)
        {
            ribbonIcons.Add(button, icon);
            RibbonIcons.Apply(button, icon, darkIcons ?? false);
        }

        private void AddExcelButtons(RibbonPanel panel)
        {
            var export = new PushButtonData("ExportSchedulesExcel", "Eksport do\nExcela", assemblyPath, "AmberBim.ScheduleExcel.ExportSchedulesCommand")
            { ToolTip = "Eksport wybranych zestawień do skoroszytu XLSX. Każde zestawienie w osobnym arkuszu." };
            Track((PushButton)panel.AddItem(export), "ExcelExport");
            var import = new PushButtonData("ImportSchedulesExcel", "Import z\nExcela", assemblyPath, "AmberBim.ScheduleExcel.ImportSchedulesCommand")
            { ToolTip = "Aktualizacja parametrów z pliku AmberBIM: elementy po UniqueId w kolumnie A, parametry po ID w ukrytym wierszu 1. Zapis po walidacji bez podglądu." };
            Track((PushButton)panel.AddItem(import), "ExcelImport");
        }

        private void ApplyIcons(bool dark)
        {
            if (darkIcons == dark)
                return;
            foreach (var item in ribbonIcons)
                RibbonIcons.Apply(item.Key, item.Value, dark);
            darkIcons = dark;
        }

#if REVIT_THEME_SUPPORT
        private void OnThemeChanged(object sender, ThemeChangedEventArgs args)
        {
            ApplyIcons(UIThemeManager.CurrentTheme == UITheme.Dark);
        }
#endif
    }
}
