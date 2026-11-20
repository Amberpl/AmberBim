using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace AmberBim.ScheduleExcel
{
    public static class XlsxExchange
    {
        private static readonly XNamespace S = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private static readonly XNamespace P = "http://schemas.openxmlformats.org/package/2006/relationships";
        private const string MetadataName = "__AmberBIM";
        private const string RowKeyMarker = "__AmberBIM_RowKey";
        private const string FormatVersion = "2";

        public static void Write(string path, ExchangeDocument document)
        {
            if (document.Sheets.Count == 0) throw new InvalidOperationException("Nie wybrano zestawień.");
            path = Path.GetFullPath(path);
            var temporary = Path.Combine(Path.GetDirectoryName(path), ".amberbim-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                using (var file = File.Create(temporary))
                using (var zip = new ZipArchive(file, ZipArchiveMode.Create))
                {
                    var names = UniqueNames(document.Sheets.Select(s => s.Name));
                    var sheets = new XElement(S + "sheets");
                    var relationships = new XElement(P + "Relationships");
                    var types = new XElement("Types",
                        new XAttribute("xmlns", "http://schemas.openxmlformats.org/package/2006/content-types"));
                    XNamespace content = types.Attribute("xmlns").Value;
                    types.Name = content + "Types";
                    types.Add(new XElement(content + "Default", new XAttribute("Extension", "rels"), new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
                        new XElement(content + "Default", new XAttribute("Extension", "xml"), new XAttribute("ContentType", "application/xml")));
                    AddType(types, content, "/xl/workbook.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml");
                    AddType(types, content, "/xl/styles.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml");
                    for (var i = 0; i < document.Sheets.Count + 2; i++)
                    {
                        var id = i + 1;
                        var metadata = i == document.Sheets.Count + 1;
                        var info = i == document.Sheets.Count;
                        var sheet = new XElement(S + "sheet", new XAttribute("name", metadata ? MetadataName : info ? "Info" : names[i]),
                            new XAttribute("sheetId", id), new XAttribute(R + "id", "rId" + id));
                        if (metadata) sheet.Add(new XAttribute("state", "veryHidden"));
                        sheets.Add(sheet);
                        relationships.Add(Relationship("rId" + id, "worksheet", "worksheets/sheet" + id + ".xml"));
                        AddType(types, content, "/xl/worksheets/sheet" + id + ".xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml");
                        Save(zip, "xl/worksheets/sheet" + id + ".xml", metadata ? MetadataSheet(document) : info ? InfoSheet() : DataSheet(document.Sheets[i]));
                    }
                    relationships.Add(Relationship("styles", "styles", "styles.xml"));
                    Save(zip, "[Content_Types].xml", types);
                    Save(zip, "_rels/.rels", new XElement(P + "Relationships", Relationship("rId1", "officeDocument", "xl/workbook.xml")));
                    Save(zip, "xl/workbook.xml", new XElement(S + "workbook", new XAttribute(XNamespace.Xmlns + "r", R),
                        new XElement(S + "bookViews", new XElement(S + "workbookView", new XAttribute("activeTab", 0))), sheets));
                    Save(zip, "xl/_rels/workbook.xml.rels", relationships);
                    Save(zip, "xl/styles.xml", Styles());
                }
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }

        public static WorkbookImport Read(string path)
        {
            using (var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var zip = new ZipArchive(file, ZipArchiveMode.Read))
            {
                var workbook = Load(zip, "xl/workbook.xml");
                var rels = Load(zip, "xl/_rels/workbook.xml.rels").Root.Elements(P + "Relationship")
                    .ToDictionary(e => (string)e.Attribute("Id"), e => (string)e.Attribute("Target"));
                var strings = zip.GetEntry("xl/sharedStrings.xml") == null ? new List<string>()
                    : Load(zip, "xl/sharedStrings.xml").Root.Elements(S + "si").Select(TextOf).ToList();
                var sheetData = workbook.Descendants(S + "sheet").Select(e => new
                {
                    Name = (string)e.Attribute("name"),
                    Rows = ReadRows(Load(zip, ResolvePart(rels[(string)e.Attribute(R + "id")])), strings)
                }).ToList();
                var metadata = sheetData.SingleOrDefault(s => s.Name == MetadataName);
                if (metadata == null || ValueAt(metadata.Rows, 1, 0).Value != "AmberBIM Schedule Exchange" ||
                    ValueAt(metadata.Rows, 1, 1).Value != FormatVersion) throw new InvalidDataException("Nieobsługiwany format pliku AmberBIM. Wyeksportuj zestawienia ponownie, aby zapisać ID parametrów.");
                var xml = string.Concat(metadata.Rows.Where(r => r.Key >= 3).OrderBy(r => r.Key).Select(r => ValueAt(metadata.Rows, r.Key, 0).Value));
                var model = ParseMetadata(ParseXml(xml));
                var result = new WorkbookImport { ModelId = model.ModelId };
                var knownSheets = model.Sheets.ToDictionary(s => s.Key, StringComparer.Ordinal);
                var seenSheets = new HashSet<string>(StringComparer.Ordinal);
                foreach (var sheet in sheetData.Where(s => s.Name != MetadataName))
                {
                    var marker = ValueAt(sheet.Rows, 1, 0).Value;
                    if (marker == "AmberBIM_INFO") continue;
                    if (!marker.StartsWith("AmberBIM:", StringComparison.Ordinal))
                    {
                        result.Errors.Add(sheet.Name + ": brak identyfikatora arkusza AmberBIM.");
                        continue;
                    }
                    var key = marker.Substring(9);
                    if (!knownSheets.TryGetValue(key, out var definition) || !seenSheets.Add(key))
                    {
                        result.Errors.Add(sheet.Name + ": nieznany lub powielony identyfikator arkusza.");
                        continue;
                    }
                    if (!string.IsNullOrEmpty(definition.ReadOnlyReason))
                    {
                        result.SkippedSheets.Add(sheet.Name + ": " + definition.ReadOnlyReason);
                        continue;
                    }
                    var columns = new Dictionary<int, string>();
                    var knownColumns = definition.Columns.Where(c => !string.IsNullOrEmpty(c.ParameterId)).GroupBy(c => c.ParameterId, StringComparer.Ordinal).ToList();
                    if (knownColumns.Any(g => g.Count() != 1))
                    {
                        result.Errors.Add(sheet.Name + ": powielone ID parametrów nie pozwalają jednoznacznie powiązać kolumn.");
                        continue;
                    }
                    var byParameterId = knownColumns.ToDictionary(g => g.Key, g => g.Single().Key, StringComparer.Ordinal);
                    var seenColumns = new HashSet<string>();
                    var keyColumn = sheet.Rows[1].Where(c => c.Value.Value == RowKeyMarker).Select(c => c.Key).Single();
                    foreach (var cell in sheet.Rows.ContainsKey(1) ? sheet.Rows[1] : new Dictionary<int, CellValue>())
                    {
                        if (cell.Key == 0 || cell.Key == keyColumn || string.IsNullOrWhiteSpace(cell.Value.Value)) continue;
                        if (!byParameterId.TryGetValue(cell.Value.Value, out var columnKey) || !seenColumns.Add(columnKey))
                            result.Errors.Add(sheet.Name + ": nieznane lub powielone ID parametru „" + cell.Value.Value + "”.");
                        else columns[cell.Key] = columnKey;
                    }
                    if (seenColumns.Count != byParameterId.Count)
                        result.Errors.Add(sheet.Name + ": brakujące ID parametrów w ukrytym wierszu 1.");
                    var rows = definition.Rows.ToDictionary(r => r.Key);
                    var seenRows = new HashSet<string>();
                    foreach (var row in sheet.Rows.Where(r => r.Key >= 3).OrderBy(r => r.Key))
                    {
                        if (row.Value.Values.All(v => string.IsNullOrEmpty(v.Value))) continue;
                        var elementId = ValueAt(sheet.Rows, row.Key, 0).Value;
                        var rowKey = ValueAt(sheet.Rows, row.Key, keyColumn).Value;
                        ExchangeRow original;
                        if (string.IsNullOrEmpty(elementId))
                        {
                            if (rows.TryGetValue(rowKey, out original) && string.IsNullOrEmpty(original.ElementUniqueId) && original.Bindings.Count == 0) continue;
                            result.Errors.Add(sheet.Name + ", wiersz " + row.Key + ": brak UniqueId elementu w kolumnie A.");
                            continue;
                        }
                        var matches = definition.Rows.Where(r => r.ElementUniqueId == elementId).ToList();
                        if (matches.Count != 1 || !seenRows.Add(elementId))
                        {
                            result.Errors.Add(sheet.Name + ", wiersz " + row.Key + ": nieznany lub powielony UniqueId elementu.");
                            continue;
                        }
                        original = matches[0];
                        foreach (var column in columns)
                        {
                            var binding = original.Bindings.SingleOrDefault(b => b.ColumnKey == column.Value);
                            if (binding == null) continue;
                            result.Edits.Add(new CellEdit { Binding = binding, Value = ValueAt(sheet.Rows, row.Key, column.Key),
                                Location = sheet.Name + "!" + ColumnName(column.Key) + row.Key });
                        }
                    }
                }
                foreach (var missing in model.Sheets.Where(s => !seenSheets.Contains(s.Key)))
                    result.SkippedSheets.Add(missing.Name + ": arkusz usunięty z pliku.");
                return result;
            }
        }

        public static List<string> UniqueNames(IEnumerable<string> source)
        {
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { MetadataName, "History", "Info" };
            var result = new List<string>();
            foreach (var name in source)
            {
                var clean = new string((name ?? "").Where(c => "[]:*?/\\".IndexOf(c) < 0 && !char.IsControl(c)).ToArray()).Trim().Trim('\'');
                if (clean.Length == 0) clean = "Zestawienie";
                var candidate = clean.Substring(0, Math.Min(31, clean.Length));
                var number = 1;
                while (!used.Add(candidate))
                {
                    var suffix = " (" + (++number) + ")";
                    candidate = clean.Substring(0, Math.Min(31 - suffix.Length, clean.Length)) + suffix;
                }
                result.Add(candidate);
            }
            return result;
        }

        private static XElement DataSheet(ExchangeSheet sheet)
        {
            if (sheet.Columns.Count > 16382 || sheet.Rows.Count > 1048574) throw new InvalidOperationException("Zestawienie przekracza limity arkusza Excel.");
            var keyColumn = sheet.Columns.Count + 1;
            var data = new XElement(S + "sheetData");
            var ids = new XElement(S + "row", new XAttribute("r", 1), new XAttribute("hidden", 1), Cell(1, 0, CellValue.Text("AmberBIM:" + sheet.Key), 2));
            var headings = new XElement(S + "row", new XAttribute("r", 2), Cell(2, 0, CellValue.Text("Revit UniqueId"), 1));
            for (var i = 0; i < sheet.Columns.Count; i++)
            {
                ids.Add(Cell(1, i + 1, CellValue.Text(sheet.Columns[i].ParameterId), 2));
                headings.Add(Cell(2, i + 1, CellValue.Text(sheet.Columns[i].Heading), 1));
            }
            ids.Add(Cell(1, keyColumn, CellValue.Text(RowKeyMarker), 2));
            headings.Add(Cell(2, keyColumn, CellValue.Text(RowKeyMarker), 2));
            data.Add(ids, headings);
            for (var i = 0; i < sheet.Rows.Count; i++)
            {
                var source = sheet.Rows[i];
                var row = new XElement(S + "row", new XAttribute("r", i + 3), Cell(i + 3, 0, CellValue.Text(source.ElementUniqueId), 2));
                for (var c = 0; c < sheet.Columns.Count; c++)
                {
                    var column = sheet.Columns[c];
                    var binding = source.Bindings.SingleOrDefault(b => b.ColumnKey == column.Key);
                    var editable = binding != null && string.IsNullOrEmpty(sheet.ReadOnlyReason);
                    var type = column.IsType || source.TypeColumnKeys.Contains(column.Key) ||
                        (binding != null && binding.OwnerUniqueId != binding.SourceUniqueId);
                    row.Add(Cell(i + 3, c + 1, c < source.Cells.Count ? source.Cells[c] : CellValue.Text(""),
                        type ? (editable ? 3 : 4) : (editable ? 0 : 2)));
                }
                row.Add(Cell(i + 3, keyColumn, CellValue.Text(source.Key), 2));
                data.Add(row);
            }
            return new XElement(S + "worksheet",
                new XElement(S + "sheetViews", new XElement(S + "sheetView", new XAttribute("workbookViewId", 0),
                    new XElement(S + "pane", new XAttribute("ySplit", 2), new XAttribute("topLeftCell", "B3"), new XAttribute("activePane", "bottomLeft"), new XAttribute("state", "frozen")))),
                new XElement(S + "cols", new XElement(S + "col", new XAttribute("min", 1), new XAttribute("max", 1), new XAttribute("hidden", 1), new XAttribute("width", 12)),
                    new XElement(S + "col", new XAttribute("min", 2), new XAttribute("max", Math.Max(2, sheet.Columns.Count + 1)), new XAttribute("width", 24), new XAttribute("customWidth", 1)),
                    new XElement(S + "col", new XAttribute("min", keyColumn + 1), new XAttribute("max", keyColumn + 1), new XAttribute("hidden", 1), new XAttribute("width", 12))),
                data, new XElement(S + "autoFilter", new XAttribute("ref", "A2:" + ColumnName(keyColumn) + Math.Max(2, sheet.Rows.Count + 2))));
        }

        private static XElement MetadataSheet(ExchangeDocument document)
        {
            var xml = SerializeMetadata(document).ToString(SaveOptions.DisableFormatting);
            var data = new XElement(S + "sheetData", new XElement(S + "row", new XAttribute("r", 1), Cell(1, 0, CellValue.Text("AmberBIM Schedule Exchange"), 2), Cell(1, 1, CellValue.Text(FormatVersion), 2)));
            var row = 3;
            for (var i = 0; i < xml.Length;)
            {
                var length = Math.Min(30000, xml.Length - i);
                if (i + length < xml.Length && char.IsHighSurrogate(xml[i + length - 1])) length--;
                data.Add(new XElement(S + "row", new XAttribute("r", row), Cell(row, 0, CellValue.Text(xml.Substring(i, length)), 2)));
                i += length;
                row++;
            }
            return new XElement(S + "worksheet", data);
        }

        private static XElement SerializeMetadata(ExchangeDocument document) => new XElement("exchange", new XAttribute("version", FormatVersion), new XAttribute("model", document.ModelId),
            document.Sheets.Select(s => new XElement("sheet", new XAttribute("key", s.Key), new XAttribute("schedule", s.ScheduleUniqueId), new XAttribute("name", s.Name), new XAttribute("reason", s.ReadOnlyReason ?? ""),
                s.Columns.Select(c => new XElement("column", new XAttribute("key", c.Key), new XAttribute("heading", c.Heading ?? ""), new XAttribute("parameterId", c.ParameterId ?? ""))),
                s.Rows.Select(r => new XElement("row", new XAttribute("key", r.Key), new XAttribute("element", r.ElementUniqueId), r.Bindings.Select(b => new XElement("binding",
                    new XAttribute("column", b.ColumnKey), new XAttribute("schedule", b.ScheduleUniqueId), new XAttribute("field", b.FieldId),
                    new XAttribute("source", b.SourceUniqueId), new XAttribute("owner", b.OwnerUniqueId), new XAttribute("parameter", b.ParameterKey), new XAttribute("parameterId", b.ParameterId),
                    new XAttribute("kind", b.Kind), new XAttribute("unit", b.UnitId ?? ""), new XAttribute("excelType", b.OriginalExcel.Type),
                    new XElement("original", b.OriginalRaw ?? ""), new XElement("excel", b.OriginalExcel.Value ?? ""))))))));

        private static ExchangeDocument ParseMetadata(XDocument xml)
        {
            var root = xml.Root;
            if (root == null || root.Name != "exchange" || (string)root.Attribute("version") != FormatVersion) throw new InvalidDataException("Nieobsługiwana wersja danych technicznych.");
            var result = new ExchangeDocument { ModelId = Required(root, "model") };
            foreach (var source in root.Elements("sheet"))
            {
                var sheet = new ExchangeSheet { Key = Required(source, "key"), ScheduleUniqueId = Required(source, "schedule"), Name = Required(source, "name"), ReadOnlyReason = (string)source.Attribute("reason") ?? "" };
                sheet.Columns = source.Elements("column").Select(c => new ExchangeColumn { Key = Required(c, "key"), Heading = (string)c.Attribute("heading") ?? "", ParameterId = (string)c.Attribute("parameterId") ?? "" }).ToList();
                foreach (var row in source.Elements("row"))
                {
                    sheet.Rows.Add(new ExchangeRow { Key = Required(row, "key"), ElementUniqueId = (string)row.Attribute("element") ?? "", Bindings = row.Elements("binding").Select(b => new CellBinding
                    {
                        ColumnKey = Required(b, "column"), ScheduleUniqueId = Required(b, "schedule"), FieldId = int.Parse(Required(b, "field"), CultureInfo.InvariantCulture),
                        SourceUniqueId = Required(b, "source"), OwnerUniqueId = Required(b, "owner"), ParameterKey = Required(b, "parameter"), ParameterId = Required(b, "parameterId"),
                        Kind = (ValueKind)Enum.Parse(typeof(ValueKind), Required(b, "kind")), UnitId = (string)b.Attribute("unit") ?? "",
                        OriginalRaw = (string)b.Element("original") ?? "", OriginalExcel = new CellValue { Type = Required(b, "excelType"), Value = (string)b.Element("excel") ?? "" }
                    }).ToList() });
                }
                if (sheet.Columns.Select(c => c.Key).Distinct().Count() != sheet.Columns.Count || sheet.Rows.Select(r => r.Key).Distinct().Count() != sheet.Rows.Count)
                    throw new InvalidDataException("Powielone identyfikatory w danych technicznych.");
                if (sheet.Rows.Any(r => r.Bindings.Select(b => b.ColumnKey).Distinct().Count() != r.Bindings.Count || r.Bindings.Any(b => !sheet.Columns.Any(c => c.Key == b.ColumnKey && c.ParameterId == b.ParameterId && !string.IsNullOrEmpty(c.ParameterId)) || b.ScheduleUniqueId != sheet.ScheduleUniqueId || b.SourceUniqueId != r.ElementUniqueId)))
                    throw new InvalidDataException("Niespójne powiązania komórek.");
                result.Sheets.Add(sheet);
            }
            return result;
        }

        private static string Required(XElement element, string name) => (string)element.Attribute(name) ?? throw new InvalidDataException("Brak danych technicznych: " + name);

        private static XElement InfoSheet()
        {
            var rules = new[]
            {
                "AmberBIM — zasady importu zestawień",
                "Plik służy do aktualizacji istniejących elementów w modelu, z którego został wyeksportowany.",
                "W każdym arkuszu zestawienia kolumna A jest ukryta i zawiera UniqueId elementu Revita. Nie usuwaj ani nie zmieniaj tych identyfikatorów.",
                "Ukryty wiersz 1 zawiera ID parametrów. Nie zmieniaj ani nie usuwaj tych ID. Wiersz 2 zawiera ich czytelne nazwy; nazwy nie sterują importem.",
                "Możesz zmieniać kolejność wierszy i kolumn danych. Sortuj całe wiersze za pomocą filtrów nagłówków. Przenoś całe kolumny wraz z ukrytym wierszem ID.",
                "Białe komórki z ciemnym tekstem to edytowalne parametry wystąpienia. Jasnoniebieskie tło oznacza parametry typu.",
                "Szary tekst oznacza pola tylko do odczytu, także na jasnoniebieskim tle. Szare tło oznacza pozostałe pola tylko do odczytu, pola obliczane i wiersze zbiorcze.",
                "Zestawienia grupowane i pozostałe arkusze tylko do odczytu są pomijane podczas importu.",
                "Liczby podawaj w jednostkach użytych przy eksporcie. Wartości Tak/Nie można wpisać jako TRUE/FALSE, Tak/Nie lub 1/0.",
                "Zmiana parametru typu obejmuje wszystkie wystąpienia tego typu, również poza eksportowanym zestawieniem.",
                "Usunięcie wiersza z Excela nie usuwa elementu. Dodawanie elementów przez dopisywanie wierszy nie jest obsługiwane.",
                "Po wybraniu pliku import waliduje dane i zapisuje zmiany bez dodatkowego potwierdzenia.",
                "Błędna wartość lub konflikt z aktualnymi danymi modelu blokuje cały zapis. Błąd zapisu wycofuje cały import.",
                "Zapisz zmiany w Excelu i zamknij plik przed importem. Nie usuwaj ukrytych danych technicznych skoroszytu.",
                "Ten arkusz Info nie jest importowany."
            };
            return new XElement(S + "worksheet", new XElement(S + "cols", new XElement(S + "col", new XAttribute("min", 1), new XAttribute("max", 1), new XAttribute("width", 110), new XAttribute("customWidth", 1))),
                new XElement(S + "sheetData", new XElement(S + "row", new XAttribute("r", 1), new XAttribute("hidden", 1), Cell(1, 0, CellValue.Text("AmberBIM_INFO"), 2)),
                    rules.Select((text, i) => new XElement(S + "row", new XAttribute("r", i + 2), Cell(i + 2, 0, CellValue.Text(text), i == 0 ? 1 : 0)))));
        }

        private static XElement Cell(int row, int column, CellValue value, int style)
        {
            var cell = new XElement(S + "c", new XAttribute("r", ColumnName(column) + row), new XAttribute("s", style));
            if (value.Type == "number" || value.Type == "boolean")
            {
                if (value.Type == "boolean") cell.Add(new XAttribute("t", "b"));
                cell.Add(new XElement(S + "v", value.Value));
            }
            else
            {
                if (value.Value.Length > 32767) throw new InvalidOperationException("Wartość przekracza limit długości komórki Excel.");
                cell.Add(new XAttribute("t", "inlineStr"), new XElement(S + "is", new XElement(S + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), value.Value)));
            }
            return cell;
        }

        private static Dictionary<int, Dictionary<int, CellValue>> ReadRows(XDocument xml, IList<string> strings)
        {
            var rows = new Dictionary<int, Dictionary<int, CellValue>>();
            foreach (var row in xml.Descendants(S + "sheetData").Elements(S + "row"))
            {
                var number = int.Parse((string)row.Attribute("r"), CultureInfo.InvariantCulture);
                var cells = new Dictionary<int, CellValue>();
                foreach (var cell in row.Elements(S + "c"))
                {
                    var reference = (string)cell.Attribute("r");
                    var column = 0;
                    foreach (var ch in reference.TakeWhile(char.IsLetter)) column = checked(column * 26 + char.ToUpperInvariant(ch) - 'A' + 1);
                    var type = (string)cell.Attribute("t");
                    var value = (string)cell.Element(S + "v") ?? "";
                    CellValue parsed;
                    if (type == "inlineStr") parsed = CellValue.Text(TextOf(cell.Element(S + "is")));
                    else if (type == "s") parsed = CellValue.Text(strings[int.Parse(value, CultureInfo.InvariantCulture)]);
                    else if (type == "b") parsed = new CellValue { Type = "boolean", Value = value };
                    else if (type == "e" || (cell.Element(S + "f") != null && cell.Element(S + "v") == null)) parsed = new CellValue { Type = "error", Value = value };
                    else parsed = new CellValue { Type = type == "str" || type == "d" || value.Length == 0 ? "text" : "number", Value = value };
                    cells.Add(column - 1, parsed);
                }
                rows.Add(number, cells);
            }
            return rows;
        }

        private static string TextOf(XElement element) => element == null ? "" : string.Concat(element.Descendants(S + "t").Select(t => t.Value));
        private static CellValue ValueAt(Dictionary<int, Dictionary<int, CellValue>> rows, int row, int column) => rows.TryGetValue(row, out var cells) && cells.TryGetValue(column, out var value) ? value : CellValue.Text("");
        private static string ResolvePart(string target) => new Uri(new Uri("https://xlsx.local/xl/workbook.xml"), target).AbsolutePath.TrimStart('/');
        private static XElement Relationship(string id, string kind, string target) => new XElement(P + "Relationship", new XAttribute("Id", id), new XAttribute("Type", R.NamespaceName + "/" + kind), new XAttribute("Target", target));
        private static void AddType(XElement root, XNamespace ns, string path, string type) => root.Add(new XElement(ns + "Override", new XAttribute("PartName", path), new XAttribute("ContentType", type)));
        private static void Save(ZipArchive zip, string path, XElement content) { using (var stream = zip.CreateEntry(path, CompressionLevel.Optimal).Open()) new XDocument(content).Save(stream); }
        private static XDocument Load(ZipArchive zip, string path)
        {
            var entry = zip.GetEntry(path) ?? throw new InvalidDataException("Brak części skoroszytu: " + path);
            using (var stream = entry.Open())
            using (var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null })) return XDocument.Load(reader);
        }
        private static XDocument ParseXml(string xml) { using (var text = new StringReader(xml)) using (var reader = XmlReader.Create(text, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null })) return XDocument.Load(reader); }
        private static string ColumnName(int index) { var name = ""; for (var value = index + 1; value > 0; value = (value - 1) / 26) name = (char)('A' + (value - 1) % 26) + name; return name; }

        private static XElement Styles() => XElement.Parse(@"<styleSheet xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main'>
<fonts count='3'><font><sz val='11'/><name val='Calibri'/></font><font><b/><sz val='11'/><color rgb='FFFFFFFF'/><name val='Calibri'/></font><font><sz val='11'/><color rgb='FF666666'/><name val='Calibri'/></font></fonts>
<fills count='5'><fill><patternFill patternType='none'/></fill><fill><patternFill patternType='gray125'/></fill><fill><patternFill patternType='solid'><fgColor rgb='FF334155'/></patternFill></fill><fill><patternFill patternType='solid'><fgColor rgb='FFF0F2F4'/></patternFill></fill><fill><patternFill patternType='solid'><fgColor rgb='FFDDEBF7'/></patternFill></fill></fills>
<borders count='1'><border><left/><right/><top/><bottom/><diagonal/></border></borders><cellStyleXfs count='1'><xf numFmtId='0' fontId='0' fillId='0' borderId='0'/></cellStyleXfs>
<cellXfs count='5'><xf numFmtId='0' fontId='0' fillId='0' borderId='0' xfId='0'/><xf numFmtId='0' fontId='1' fillId='2' borderId='0' xfId='0' applyFont='1' applyFill='1'/><xf numFmtId='0' fontId='2' fillId='3' borderId='0' xfId='0' applyFont='1' applyFill='1'/><xf numFmtId='0' fontId='0' fillId='4' borderId='0' xfId='0' applyFill='1'/><xf numFmtId='0' fontId='2' fillId='4' borderId='0' xfId='0' applyFont='1' applyFill='1'/></cellXfs><cellStyles count='1'><cellStyle name='Normal' xfId='0' builtinId='0'/></cellStyles></styleSheet>");
    }
}
