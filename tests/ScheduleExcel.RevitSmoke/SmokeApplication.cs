using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using AmberBim.ScheduleExcel;

namespace AmberBim.RevitSmoke
{
    public sealed class SmokeApplication : IExternalApplication
    {
        private static string root;
        private static string output;
        private IExternalApplication ribbon;
        private static readonly XNamespace S = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        public Result OnStartup(UIControlledApplication app)
        {
            root = Environment.GetEnvironmentVariable("AMBERBIM_SMOKE_ROOT");
            if (string.IsNullOrEmpty(root)) return Result.Cancelled;
            output = Environment.GetEnvironmentVariable("AMBERBIM_SMOKE_OUTPUT");
            Directory.CreateDirectory(output);
            try
            {
                Log("Loading local Debug2024 add-in");
                var expectedAssembly = Path.GetFullPath(Path.Combine(root, "bin/Revit2024/Debug2024/net48/AmberBim.dll"));
                var loadedAssembly = Assembly.LoadFrom(expectedAssembly);
                Log("Loaded assembly: " + loadedAssembly.Location);
                if (!string.Equals(loadedAssembly.Location, expectedAssembly, StringComparison.OrdinalIgnoreCase))
                    throw new Exception("Another AmberBim assembly was loaded; the test was not started.");
                app.Idling += OnIdle;
                Log("Waiting for Idling; testing model operations only");
                return Result.Succeeded;
            }
            catch (Exception exception) { Finish(false, exception.ToString()); return Result.Failed; }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IExternalApplication StartRibbon(UIControlledApplication app)
        {
            var instance = new AmberBimRibbon();
            if (instance.OnStartup(app) != Result.Succeeded) throw new Exception("Ribbon startup failed");
            return instance;
        }

        public Result OnShutdown(UIControlledApplication app) { if (ribbon != null) ribbon.OnShutdown(app); return Result.Succeeded; }

        private void OnIdle(object sender, IdlingEventArgs args)
        {
            var ui = (UIApplication)sender;
            ui.Idling -= OnIdle;
            try { Run(ui); Finish(true, "All native Revit 2024 scenarios passed."); }
            catch (Exception exception) { Finish(false, exception.ToString()); }
            try { if (ui.Application.Documents.Size == 0) ui.PostCommand(RevitCommandId.LookupPostableCommandId(PostableCommand.ExitRevit)); }
            catch (Exception exception) { Log("Exit: " + exception.Message); }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Run(UIApplication ui)
        {
            Document document = null;
            try
            {
                Log("Creating isolated metric test project");
                document = ui.Application.NewProjectDocument(UnitSystem.Metric);
                Wall wall1, wall2;
                ViewSchedule schedule, grouped;
                WallType type;
                using (var setup = new Transaction(document, "AmberBIM test model"))
                {
                    setup.Start();
                    var units = document.GetUnits();
                    units.SetFormatOptions(SpecTypeId.Length, new FormatOptions(UnitTypeId.Meters) { Accuracy = 0.001 });
                    document.SetUnits(units);
                    var level = Level.Create(document, 0);
                    type = new FilteredElementCollector(document).OfClass(typeof(WallType)).Cast<WallType>().First(t => t.Kind == WallKind.Basic);
                    wall1 = Wall.Create(document, Line.CreateBound(new XYZ(0, 0, 0), new XYZ(10, 0, 0)), type.Id, level.Id, 10, 0, false, false);
                    wall2 = Wall.Create(document, Line.CreateBound(new XYZ(0, 5, 0), new XYZ(15, 5, 0)), type.Id, level.Id, 12, 0, false, false);
                    wall1.get_Parameter(BuiltInParameter.ALL_MODEL_MARK).Set("AB-001");
                    wall2.get_Parameter(BuiltInParameter.ALL_MODEL_MARK).Set("AB-002");
                    wall1.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).Set("Żółć — pierwszy");
                    wall2.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).Set("Drugi element");
                    type.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_COMMENTS).Set("Typ oryginalny");
                    schedule = ViewSchedule.CreateSchedule(document, new ElementId(BuiltInCategory.OST_Walls));
                    schedule.Name = "AmberBIM — ściany testowe";
                    schedule.Definition.IsItemized = true;
                    var parameters = new[] { BuiltInParameter.ALL_MODEL_MARK, BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS, BuiltInParameter.WALL_USER_HEIGHT_PARAM,
                        BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT, BuiltInParameter.ALL_MODEL_TYPE_COMMENTS };
                    var headings = new[] { "Mark", "Comments", "Height", "Structural", "Type Comments" };
                    for (var i = 0; i < parameters.Length; i++)
                    {
                        var available = schedule.Definition.GetSchedulableFields().First(f => f.ParameterId == new ElementId(parameters[i]));
                        var field = schedule.Definition.AddField(available);
                        field.ColumnHeading = headings[i];
                    }
                    schedule.Definition.AddSortGroupField(new ScheduleSortGroupField(schedule.Definition.GetFieldOrder()[0]));
                    grouped = (ViewSchedule)document.GetElement(schedule.Duplicate(ViewDuplicateOption.Duplicate));
                    grouped.Name = "AmberBIM — grupowane";
                    grouped.Definition.IsItemized = false;
                    setup.Commit();
                }
                Log("Model created: " + wall1.UniqueId + ", " + wall2.UniqueId);
                var service = new RevitScheduleExchange(document);
                Assert(service.ListSchedules().Any(s => s.UniqueId == schedule.UniqueId && !s.ReadOnly), "schedule selection");
                var path = Path.Combine(output, "revit-roundtrip.xlsx");
                service.Export(new[] { schedule.UniqueId, grouped.UniqueId }, path);
                var imported = XlsxExchange.Read(path);
                Log("Exported editable bindings: " + imported.Edits.Count + "; skipped sheets: " + imported.SkippedSheets.Count);
                Assert(imported.Errors.Count == 0 && imported.Edits.Count >= 8 && imported.SkippedSheets.Count == 1, "editable schedule extraction");
                Edit(path, wall1.UniqueId, new[] { "Comments", "Height", "Structural", "Type Comments" }, new[] { "Zaimportowany opis", "4.25", "Tak", "Nowy opis typu" });
                var report = service.Import(path);
                LogReport("import", report);
                Assert(report.Errors.Count == 0 && report.ChangedValues == 4, "native four-parameter import");
                Assert(wall1.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsString() == "Zaimportowany opis", "text parameter");
                Assert(Math.Abs(wall1.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM).AsDouble() - 4.25 / 0.3048) < 1e-8, "length unit conversion");
                Assert(wall1.get_Parameter(BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT).AsInteger() == 1, "boolean");
                Assert(type.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_COMMENTS).AsString() == "Nowy opis typu", "type parameter");
                Assert(wall2.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsString() == "Drugi element", "unmodified element");
                var repeat = service.Import(path);
                Assert(repeat.Errors.Count == 0 && repeat.ChangedValues == 0, "idempotent native import");
                Log("PASS native export/import, type, units, boolean, grouping, idempotency");

                path = Path.Combine(output, "revit-conflict.xlsx");
                service.Export(new[] { schedule.UniqueId }, path);
                Edit(path, wall1.UniqueId, new[] { "Comments" }, new[] { "Z Excela" });
                using (var change = new Transaction(document, "Change after export")) { change.Start(); wall1.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).Set("Zmieniono w modelu"); change.Commit(); }
                var conflict = service.Import(path);
                Assert(conflict.Errors.Count > 0 && wall1.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsString() == "Zmieniono w modelu", "native conflict");
                Log("PASS native stale-value conflict");

                path = Path.Combine(output, "revit-rollback.xlsx");
                service.Export(new[] { schedule.UniqueId }, path);
                Edit(path, wall1.UniqueId, new[] { "Comments", "Height" }, new[] { "Nie wolno zapisać", "-4" });
                var rollback = service.Import(path);
                LogReport("rollback", rollback);
                Assert(rollback.Errors.Count > 0 && rollback.ChangedValues == 0 && wall1.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsString() == "Zmieniono w modelu", "native atomic rollback");
                Log("PASS native transaction rollback");
                document.SaveAs(Path.Combine(output, "test-model.rvt"), new SaveAsOptions { OverwriteExistingFile = true });
            }
            finally { if (document != null && document.IsValidObject) document.Close(false); }
        }

        private static void Edit(string path, string uniqueId, string[] headings, string[] values)
        {
            using (var zip = ZipFile.Open(path, ZipArchiveMode.Update))
            {
                var entry = zip.GetEntry("xl/worksheets/sheet1.xml");
                XDocument xml;
                using (var stream = entry.Open()) xml = XDocument.Load(stream);
                var row = xml.Descendants(S + "row").Single(r => r.Elements(S + "c").Any(c => ((string)c.Attribute("r")).StartsWith("A") && Text(c) == uniqueId));
                var rowNumber = (string)row.Attribute("r");
                var parameterIds = xml.Descendants(S + "row").Single(r => (string)r.Attribute("r") == "1");
                var parameterByLabel = new System.Collections.Generic.Dictionary<string, BuiltInParameter>
                {
                    { "Comments", BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS },
                    { "Height", BuiltInParameter.WALL_USER_HEIGHT_PARAM },
                    { "Structural", BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT },
                    { "Type Comments", BuiltInParameter.ALL_MODEL_TYPE_COMMENTS }
                };
                for (var i = 0; i < headings.Length; i++)
                {
                    var parameterId = ((long)parameterByLabel[headings[i]]).ToString(CultureInfo.InvariantCulture);
                    var column = new string(((string)parameterIds.Elements(S + "c").Single(c => Text(c) == parameterId).Attribute("r")).TakeWhile(char.IsLetter).ToArray());
                    var cell = row.Elements(S + "c").Single(c => (string)c.Attribute("r") == column + rowNumber);
                    cell.SetAttributeValue("t", "inlineStr"); cell.RemoveNodes(); cell.Add(new XElement(S + "is", new XElement(S + "t", values[i])));
                }
                entry.Delete();
                using (var stream = zip.CreateEntry("xl/worksheets/sheet1.xml").Open()) xml.Save(stream);
            }
        }

        private static string Text(XElement cell) => string.Concat(cell.Descendants(S + "t").Select(e => e.Value));
        private static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
        private static void LogReport(string name, ImportReport report) => Log(name + ": changed=" + report.ChangedValues + "; errors=" + string.Join(" | ", report.Errors));
        private static void Log(string message) => File.AppendAllText(Path.Combine(output, "trace.txt"), DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + " " + message + Environment.NewLine);
        private static void Finish(bool passed, string message) { Log((passed ? "PASS " : "FAIL ") + message); File.WriteAllText(Path.Combine(output, "result.txt"), (passed ? "PASS" : "FAIL") + Environment.NewLine + message); }
    }
}
