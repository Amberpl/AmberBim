using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using AmberBim.ScheduleExcel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;

internal static class Program
{
    private static readonly XNamespace S = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static string output;
    private static int count;

    private static int Main(string[] args)
    {
        output = Path.GetFullPath(args[0]);
        Directory.CreateDirectory(output);
        try
        {
            Test("schedule filenames and combined category/name filtering", () =>
            {
                var schedules = new[] {
                    new ScheduleInfo { Name = "Ściany: A/B", CategoryId = "walls", CategoryName = "Ściany", IsSelected = true },
                    new ScheduleInfo { Name = "Drzwi", CategoryId = "doors", CategoryName = "Drzwi", IsSelected = true },
                    new ScheduleInfo { Name = "Wszystkie", CategoryId = "-1", CategoryName = "Wiele kategorii" },
                    new ScheduleInfo { Name = "Inne", CategoryId = "unknown", CategoryName = "Nieustalona" }
                };
                Assert(ScheduleSelection.DefaultFileName(schedules) == "Ściany_ A_B.xlsx", "first selected name and invalid characters");
                schedules[0].IsSelected = false;
                Assert(ScheduleSelection.DefaultFileName(schedules) == "Drzwi.xlsx", "single selected name");
                Assert(schedules.Count(s => ScheduleSelection.Matches(s, "DRZ", "doors")) == 1, "combined search");
                Assert(schedules.Count(s => ScheduleSelection.Matches(s, "DRZ", "walls")) == 0, "filters intersect");
                Assert(schedules.Count(s => ScheduleSelection.Matches(s, "", null)) == 4, "all categories");
                Assert(schedules.Count(s => ScheduleSelection.Matches(s, "", "-1")) == 1 && schedules.Count(s => ScheduleSelection.Matches(s, "", "unknown")) == 1, "category fallbacks filterable");
                foreach (var schedule in schedules.Where(s => ScheduleSelection.Matches(s, "", "walls"))) schedule.IsSelected = true;
                Assert(schedules[0].IsSelected && schedules[1].IsSelected && !schedules[2].IsSelected, "visible selection preserves other selections");
            });
            Test("valid XLSX, names, Info, hidden IDs, Unicode and numbers", () =>
            {
                var path = Write("roundtrip");
                using (var doc = SpreadsheetDocument.Open(path, false))
                {
                    var errors = new OpenXmlValidator(FileFormatVersions.Office2019).Validate(doc).ToList();
                    Assert(errors.Count == 0, string.Join("\n", errors.Take(5).Select(e => e.Description + " " + e.Path?.XPath)));
                }
                var workbook = ReadXml(path, "xl/workbook.xml");
                var names = workbook.Descendants(S + "sheet").Select(e => (string)e.Attribute("name")).ToList();
                Assert(names[names.Count - 2] == "Info" && names[0] != "Info" && names.All(n => n.Length <= 31) && names.Distinct(StringComparer.OrdinalIgnoreCase).Count() == names.Count, "sheet names");
                Assert((string)workbook.Descendants(S + "workbookView").Single().Attribute("activeTab") == "0", "first schedule active");
                var xml = ReadXml(path, "xl/worksheets/sheet1.xml");
                Assert((string)xml.Descendants(S + "col").First().Attribute("hidden") == "1", "A hidden");
                Assert(Text(xml, "A3") == "wall-1", "A contains element UniqueId");
                Assert((string)xml.Descendants(S + "row").Single(r => (int)r.Attribute("r") == 1).Attribute("hidden") == "1", "parameter ID row hidden");
                Assert(Text(xml, "B1") == "1001" && Text(xml, "B2") == "Komentarze", "parameter ID above name");
                Assert((string)xml.Descendants(S + "c").Single(c => (string)c.Attribute("r") == "B3").Attribute("s") == "0", "editable instance style");
                Assert((string)xml.Descendants(S + "c").Single(c => (string)c.Attribute("r") == "E3").Attribute("s") == "3", "editable type style");
                var read = XlsxExchange.Read(path);
                Assert(read.Errors.Count == 0 && read.SkippedSheets.Count == 1 && read.Edits.Count == 8, "roundtrip metadata");
                Assert(ImportEngine.Apply(read, new FakeModel()).ChangedValues == 0, "unchanged workbook");
            });
            Test("readonly type and instance styles and Info legend", () =>
            {
                var document = Fixture();
                document.Sheets[0].Columns[3].IsType = true;
                document.Sheets[0].Rows[0].Bindings.RemoveAt(3);
                document.Sheets[0].Rows[0].Bindings.RemoveAt(0);
                var path = Path.Combine(output, "readonly-type.xlsx");
                XlsxExchange.Write(path, document);
                var xml = ReadXml(path, "xl/worksheets/sheet1.xml");
                Assert((string)xml.Descendants(S + "c").Single(c => (string)c.Attribute("r") == "B3").Attribute("s") == "2", "readonly instance style");
                Assert((string)xml.Descendants(S + "c").Single(c => (string)c.Attribute("r") == "E3").Attribute("s") == "4", "readonly type style");
                var styles = ReadXml(path, "xl/styles.xml");
                Assert(styles.Descendants(S + "fgColor").Any(c => (string)c.Attribute("rgb") == "FFDDEBF7"), "blue fill");
                var info = ReadXml(path, "xl/worksheets/sheet3.xml").ToString();
                Assert(info.Contains("Jasnoniebieskie") && info.Contains("Szary tekst") && info.Contains("wszystkie wystąpienia"), "legend and type warning");
                using (var doc = SpreadsheetDocument.Open(path, false))
                    Assert(!new OpenXmlValidator(FileFormatVersions.Office2019).Validate(doc).Any(), "new readonly styles schema valid");
            });
            Test("ID rows and header columns survive reordering", () =>
            {
                var path = Write("reordered");
                Mutate(path, "xl/worksheets/sheet1.xml", xml =>
                {
                    var rows = xml.Descendants(S + "row").Where(r => (int)r.Attribute("r") >= 3).ToList();
                    foreach (var row in rows)
                    {
                        var old = (int)row.Attribute("r");
                        var next = old == 3 ? 4 : 3;
                        row.SetAttributeValue("r", next);
                        foreach (var cell in row.Elements(S + "c")) cell.SetAttributeValue("r", new string(((string)cell.Attribute("r")).TakeWhile(char.IsLetter).ToArray()) + next);
                    }
                    foreach (var cell in xml.Descendants(S + "c"))
                    {
                        var reference = (string)cell.Attribute("r");
                        if (reference.StartsWith("B")) cell.SetAttributeValue("r", "C" + reference.Substring(1));
                        else if (reference.StartsWith("C")) cell.SetAttributeValue("r", "B" + reference.Substring(1));
                    }
                    SetText(xml, "C4", "Żółć — zmiana po ID");
                });
                var model = new FakeModel();
                var report = ImportEngine.Apply(XlsxExchange.Read(path), model);
                Assert(report.Errors.Count == 0 && report.ChangedValues == 1 && model.Values["wall-1|comments"] == "Żółć — zmiana po ID" && model.Values["wall-2|comments"] == "Drugi", "ID/header mapping");
            });
            Test("text, units, boolean and shared type parameter", () =>
            {
                var path = Write("edited");
                Mutate(path, "xl/worksheets/sheet1.xml", xml => { SetText(xml, "B3", "Nowy opis"); SetNumber(xml, "C3", "12.5"); SetText(xml, "D3", "Nie"); SetText(xml, "E3", "Nowy typ"); });
                var model = new FakeModel();
                var report = ImportEngine.Apply(XlsxExchange.Read(path), model);
                Assert(report.Errors.Count == 0 && report.ChangedValues == 4 && report.ChangedElements == 2, "four updates including type");
                Assert(Math.Abs(double.Parse(model.Values["wall-1|height"], CultureInfo.InvariantCulture) - 12.5 / 0.3048) < 1e-8, "unit conversion");
                Assert(model.Values["wall-1|boolean"] == "0" && model.Values["type-1|type-comments"] == "Nowy typ", "boolean and type");
                Assert(ImportEngine.Apply(XlsxExchange.Read(path), model).ChangedValues == 0, "idempotent repeated import");
            });
            Test("renamed and duplicated visible names do not control parameter binding", () =>
            {
                var path = Write("renamed-headers");
                Mutate(path, "xl/worksheets/sheet1.xml", xml => { SetText(xml, "B2", "Wysokość"); SetText(xml, "C2", "Wysokość"); SetText(xml, "B3", "Zmieniony komentarz"); });
                var model = new FakeModel();
                var report = ImportEngine.Apply(XlsxExchange.Read(path), model);
                Assert(report.Errors.Count == 0 && model.Values["wall-1|comments"] == "Zmieniony komentarz", "caption independence");
                Assert(model.Values["wall-1|height"] == "10", "unchanged numeric parameter");
            });
            Test("missing, unknown and duplicate parameter IDs reject all writes", () =>
            {
                foreach (var id in new[] { "", "999999", "1002" })
                {
                    var path = Write("bad-parameter-id-" + id.Length);
                    Mutate(path, "xl/worksheets/sheet1.xml", xml => { SetText(xml, "B1", id); SetText(xml, "B3", "Zmieniony"); });
                    var model = new FakeModel();
                    Assert(ImportEngine.Apply(XlsxExchange.Read(path), model).Errors.Count > 0 && model.ApplyCalls == 0, "parameter ID rejected");
                }
            });
            Test("identical displayed values retain distinct element IDs", () =>
            {
                var document = Fixture();
                var sheet = document.Sheets[0];
                for (var c = 0; c < sheet.Columns.Count; c++)
                {
                    sheet.Rows[1].Cells[c] = sheet.Rows[0].Cells[c];
                    sheet.Rows[1].Bindings[c].OriginalExcel = sheet.Rows[0].Bindings[c].OriginalExcel;
                    sheet.Rows[1].Bindings[c].OriginalRaw = sheet.Rows[0].Bindings[c].OriginalRaw;
                }
                var path = Path.Combine(output, "identical-values.xlsx");
                XlsxExchange.Write(path, document);
                var xml = ReadXml(path, "xl/worksheets/sheet1.xml");
                Assert(Text(xml, "A3") == "wall-1" && Text(xml, "A4") == "wall-2", "distinct IDs for repeated values");
                Mutate(path, "xl/worksheets/sheet1.xml", x => SetText(x, "B4", "Zmiana tylko drugiego"));
                var model = new FakeModel();
                model.Values["wall-2|comments"] = "Pierwszy";
                model.Values["wall-2|height"] = "10";
                var report = ImportEngine.Apply(XlsxExchange.Read(path), model);
                Assert(report.Errors.Count == 0 && report.ChangedValues == 1 && model.Values["wall-2|comments"] == "Zmiana tylko drugiego" && model.Values["wall-1|comments"] == "Pierwszy", "ID selection for identical rows");
            });
            Test("wrong model, unknown element and invalid number", () =>
            {
                var path = Write("bad-model");
                var model = new FakeModel { ModelId = "other-model" };
                Assert(ImportEngine.Apply(XlsxExchange.Read(path), model).Errors.Count > 0 && model.ApplyCalls == 0, "model rejected");
                path = Write("bad-id");
                Mutate(path, "xl/worksheets/sheet1.xml", xml => SetText(xml, "A3", "unknown-id"));
                Assert(XlsxExchange.Read(path).Errors.Count > 0, "unknown ID rejected");
                path = Write("bad-number");
                Mutate(path, "xl/worksheets/sheet1.xml", xml => { SetText(xml, "B3", "Good edit"); SetText(xml, "C3", "nieliczba"); });
                model = new FakeModel();
                Assert(ImportEngine.Apply(XlsxExchange.Read(path), model).Errors.Count > 0 && model.ApplyCalls == 0, "no partial import");
            });
            Test("conflicting type changes and stale model values", () =>
            {
                var path = Write("type-conflict");
                Mutate(path, "xl/worksheets/sheet1.xml", xml => { SetText(xml, "E3", "A"); SetText(xml, "E4", "B"); });
                var model = new FakeModel();
                Assert(ImportEngine.Apply(XlsxExchange.Read(path), model).Errors.Count > 0 && model.ApplyCalls == 0, "type conflict");
                path = Write("stale");
                Mutate(path, "xl/worksheets/sheet1.xml", xml => SetText(xml, "B3", "Z Excela"));
                model = new FakeModel(); model.Values["wall-1|comments"] = "Zmienione w modelu";
                Assert(ImportEngine.Apply(XlsxExchange.Read(path), model).Errors.Count > 0 && model.ApplyCalls == 0, "stale change");
            });
            Test("atomic apply rollback on write failure", () =>
            {
                var path = Write("rollback");
                Mutate(path, "xl/worksheets/sheet1.xml", xml => { SetText(xml, "B3", "Pierwszy zapis"); SetText(xml, "B4", "Drugi zapis"); });
                var model = new FakeModel { FailSecondWrite = true };
                var report = ImportEngine.Apply(XlsxExchange.Read(path), model);
                Assert(report.Errors.Count > 0 && report.ChangedValues == 0 && model.Values["wall-1|comments"] == "Pierwszy" && model.Values["wall-2|comments"] == "Drugi", "rollback");
            });
            Test("Info edits ignored, readonly sheets skipped and deleted rows do not delete", () =>
            {
                var path = Write("readonly");
                Mutate(path, "xl/worksheets/sheet3.xml", xml => SetText(xml, "A2", "Zmieniona instrukcja"));
                Mutate(path, "xl/worksheets/sheet2.xml", xml => SetText(xml, "B3", "Nie importuj"));
                Mutate(path, "xl/worksheets/sheet1.xml", xml => xml.Descendants(S + "row").Single(r => (int)r.Attribute("r") == 4).Remove());
                var model = new FakeModel();
                var report = ImportEngine.Apply(XlsxExchange.Read(path), model);
                Assert(report.Errors.Count == 0 && report.ChangedValues == 0 && report.SkippedSheets.Count == 1 && model.Values.ContainsKey("wall-2|comments"), "Info/readonly/delete");
            });
            Console.WriteLine("PASS " + count + " scenario groups. XLSX fixtures: " + output);
            return 0;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }

    private static void Test(string name, Action action) { action(); count++; Console.WriteLine("PASS " + name); }
    private static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static string Write(string name) { var path = Path.Combine(output, name + ".xlsx"); XlsxExchange.Write(path, Fixture()); return path; }
    private static ExchangeDocument Fixture()
    {
        var sheet = new ExchangeSheet { Name = "Ściany / długa nazwa: test zgodności arkuszy", ScheduleUniqueId = "schedule-1" };
        foreach (var heading in new[] { "Komentarze", "Wysokość", "Nośna", "Opis typu" })
            sheet.Columns.Add(new ExchangeColumn { Heading = heading, ParameterId = (1001 + sheet.Columns.Count).ToString(CultureInfo.InvariantCulture) });
        foreach (var number in new[] { 1, 2 })
        {
            var id = "wall-" + number;
            var row = new ExchangeRow { ElementUniqueId = id };
            row.Cells.AddRange(new[] { CellValue.Text(number == 1 ? "Pierwszy" : "Drugi"), CellValue.Number(number * 10 * 0.3048), CellValue.Boolean(true), CellValue.Text("Typ oryginalny") });
            for (var c = 0; c < 4; c++) row.Bindings.Add(new CellBinding
            {
                ColumnKey = sheet.Columns[c].Key, ScheduleUniqueId = sheet.ScheduleUniqueId, FieldId = c, SourceUniqueId = id,
                OwnerUniqueId = c == 3 ? "type-1" : id, ParameterKey = new[] { "comments", "height", "boolean", "type-comments" }[c], ParameterId = sheet.Columns[c].ParameterId,
                Kind = new[] { ValueKind.Text, ValueKind.Number, ValueKind.Boolean, ValueKind.Text }[c], UnitId = c == 1 ? "m" : "",
                OriginalRaw = c == 0 ? row.Cells[c].Value : c == 1 ? (number * 10).ToString(CultureInfo.InvariantCulture) : c == 2 ? "1" : "Typ oryginalny",
                OriginalExcel = row.Cells[c]
            });
            sheet.Rows.Add(row);
        }
        var grouped = new ExchangeSheet { Name = sheet.Name, ScheduleUniqueId = "grouped", ReadOnlyReason = "Zestawienie grupowane" };
        grouped.Columns.Add(new ExchangeColumn { Heading = "Liczba" }); grouped.Rows.Add(new ExchangeRow { Cells = new List<CellValue> { CellValue.Number(2) } });
        return new ExchangeDocument { ModelId = "model-A", Sheets = new List<ExchangeSheet> { sheet, grouped } };
    }

    private sealed class FakeModel : IModelGateway
    {
        public string ModelId { get; set; } = "model-A";
        public Dictionary<string, string> Values { get; private set; } = new Dictionary<string, string>();
        public bool FailSecondWrite;
        public int ApplyCalls;
        public FakeModel() { foreach (var row in Fixture().Sheets[0].Rows) foreach (var b in row.Bindings) Values[b.OwnerUniqueId + "|" + b.ParameterKey] = b.OriginalRaw; }
        public ParameterState Read(CellBinding b) { var key = b.OwnerUniqueId + "|" + b.ParameterKey; if (!Values.ContainsKey(key)) throw new Exception("unknown parameter"); return new ParameterState { Kind = b.Kind, Raw = Values[key] }; }
        public double ToInternal(CellBinding b, double number) { if (b.UnitId != "m") throw new Exception("bad unit"); return number / 0.3048; }
        public void Apply(IReadOnlyList<ParameterChange> changes)
        {
            ApplyCalls++;
            var before = new Dictionary<string, string>(Values);
            try { for (var i = 0; i < changes.Count; i++) { if (i == 1 && FailSecondWrite) throw new Exception("write failure"); var c = changes[i]; Values[c.Binding.OwnerUniqueId + "|" + c.Binding.ParameterKey] = c.NewRaw; } }
            catch { Values = before; throw; }
        }
    }

    private static XDocument ReadXml(string path, string part) { using (var zip = ZipFile.OpenRead(path)) using (var stream = zip.GetEntry(part).Open()) return XDocument.Load(stream); }
    private static void Mutate(string path, string part, Action<XDocument> action)
    {
        using (var zip = ZipFile.Open(path, ZipArchiveMode.Update))
        {
            var entry = zip.GetEntry(part); XDocument xml; using (var stream = entry.Open()) xml = XDocument.Load(stream);
            action(xml); entry.Delete(); using (var stream = zip.CreateEntry(part).Open()) xml.Save(stream);
        }
    }
    private static string Text(XDocument xml, string reference) => string.Concat(xml.Descendants(S + "c").Single(c => (string)c.Attribute("r") == reference).Descendants(S + "t").Select(e => e.Value));
    private static void SetText(XDocument xml, string reference, string text) { var cell = xml.Descendants(S + "c").Single(c => (string)c.Attribute("r") == reference); cell.SetAttributeValue("t", "inlineStr"); cell.RemoveNodes(); cell.Add(new XElement(S + "is", new XElement(S + "t", text))); }
    private static void SetNumber(XDocument xml, string reference, string number) { var cell = xml.Descendants(S + "c").Single(c => (string)c.Attribute("r") == reference); cell.Attribute("t")?.Remove(); cell.RemoveNodes(); cell.Add(new XElement(S + "v", number)); }
}
