using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Autodesk.Revit.DB;

namespace AmberBim.ScheduleExcel
{
    public sealed class RevitScheduleExchange
    {
        private readonly Document document;
        public RevitScheduleExchange(Document document) { this.document = document; }

        public IReadOnlyList<ScheduleInfo> ListSchedules() => new FilteredElementCollector(document).OfClass(typeof(ViewSchedule)).Cast<ViewSchedule>()
            .Where(s => !s.IsTemplate && !s.IsTitleblockRevisionSchedule)
            .Select(s => new ScheduleInfo { UniqueId = s.UniqueId, Name = s.Name,
                CategoryId = RevitCompatibility.IdValue(s.Definition.CategoryId).ToString(CultureInfo.InvariantCulture),
                CategoryName = s.Definition.CategoryId == ElementId.InvalidElementId ? "Wiele kategorii"
                    : Category.GetCategory(document, s.Definition.CategoryId)?.Name ?? "Nieustalona",
                ReadOnly = ReadOnlyReason(s).Length != 0, Reason = ReadOnlyReason(s) })
            .OrderBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase).ToList();

        public ExportReport Export(IReadOnlyList<string> scheduleIds, string path)
        {
            var exchange = new ExchangeDocument { ModelId = document.ProjectInformation.UniqueId };
            foreach (var id in scheduleIds.Distinct())
            {
                var schedule = document.GetElement(id) as ViewSchedule;
                if (schedule == null || schedule.IsTemplate) throw new InvalidOperationException("Wybrane zestawienie nie istnieje.");
                exchange.Sheets.Add(Extract(schedule));
            }
            XlsxExchange.Write(path, exchange);
            return new ExportReport { Path = path, SheetCount = exchange.Sheets.Count };
        }

        public ImportReport Import(string path)
        {
            try { return ImportEngine.Apply(XlsxExchange.Read(path), new RevitModelGateway(document)); }
            catch (Exception exception) { var report = new ImportReport(); report.Errors.Add(exception.Message); return report; }
        }

        internal static string ReadOnlyReason(ViewSchedule schedule)
        {
            var definition = schedule.Definition;
            if (!definition.IsItemized) return "Zestawienie grupowane";
            if (definition.IsKeySchedule) return "Zestawienie kluczy";
            if (definition.IsMaterialTakeoff) return "Zestawienie materiałów";
            if (definition.HasEmbeddedSchedule) return "Zestawienie zagnieżdżone";
            if (definition.IncludeLinkedFiles) return "Dane obejmują modele podłączone";
            var category = Category.GetCategory(schedule.Document, definition.CategoryId);
            if (category == null || category.CategoryType != CategoryType.Model) return "Ten rodzaj zestawienia jest eksportowany tylko do odczytu";
            return "";
        }

        private sealed class Candidate
        {
            internal Element Element;
            internal Parameter[] Parameters;
            internal string Signature;
        }

        private ExchangeSheet Extract(ViewSchedule schedule)
        {
            var sheet = new ExchangeSheet { ScheduleUniqueId = schedule.UniqueId, Name = schedule.Name, ReadOnlyReason = ReadOnlyReason(schedule) };
            var fields = schedule.Definition.GetFieldOrder().Select(id => schedule.Definition.GetField(id)).Where(f => !f.IsHidden).ToList();
            using (var table = schedule.GetTableData())
            {
                var body = table.GetSectionData(SectionType.Body);
                for (var c = 0; c < body.NumberOfColumns; c++) sheet.Columns.Add(new ExchangeColumn
                {
                    Heading = c < fields.Count ? fields[c].GetName() : "Kolumna " + (c + 1),
                    IsType = c < fields.Count && fields[c].FieldType == ScheduleFieldType.ElementType,
                    ParameterId = c < fields.Count && fields[c].ParameterId != ElementId.InvalidElementId
                        ? RevitCompatibility.IdValue(fields[c].ParameterId).ToString(CultureInfo.InvariantCulture) : ""
                });
                var elements = new FilteredElementCollector(document, schedule.Id).WhereElementIsNotElementType().ToElements();
                for (var c = 0; c < fields.Count && c < sheet.Columns.Count; c++)
                    if (!sheet.Columns[c].IsType && IsParameterField(fields[c]))
                    {
                        var parameters = elements.Select(e => RevitModelGateway.FindFieldParameter(e, fields[c])).Where(p => p != null).ToList();
                        sheet.Columns[c].IsType = parameters.Count != 0 && parameters.All(p => p.Element is ElementType);
                    }
                if (sheet.Columns.Count != fields.Count) sheet.ReadOnlyReason = "Układ tabeli nie pozwala jednoznacznie powiązać pól";
                for (var row = body.FirstRowNumber; row <= body.LastRowNumber; row++)
                {
                    var values = Enumerable.Range(body.FirstColumnNumber, body.NumberOfColumns).Select(c => CellValue.Text(schedule.GetCellText(SectionType.Body, row, c))).ToList();
                    if (values.Count == fields.Count && values.Select(v => Normalize(v.Value)).SequenceEqual(fields.Select(f => Normalize(f.ColumnHeading)))) continue;
                    sheet.Rows.Add(new ExchangeRow { Cells = values });
                }
            }
            if (!string.IsNullOrEmpty(sheet.ReadOnlyReason) || fields.Count == 0) return sheet;
            var matchColumns = fields.Select((field, index) => new { field, index }).Where(x => IsParameterField(x.field)).Select(x => x.index).ToList();
            if (matchColumns.Count == 0) throw new InvalidOperationException(schedule.Name + ": brak pól pozwalających przypisać ID elementów. Zestawienie nie zostało zapisane.");
            var candidates = new FilteredElementCollector(document, schedule.Id).WhereElementIsNotElementType().ToElements()
                .Select(element => MakeCandidate(element, fields, matchColumns)).ToList();
            var matches = new Dictionary<ExchangeRow, Candidate>();
            if (fields.All(IsParameterField) && !schedule.Definition.ShowGrandTotal &&
                schedule.Definition.GetSortGroupFields().All(g => !g.ShowHeader && !g.ShowFooter && !g.ShowBlankLine))
            {
                sheet.Rows.Clear();
                foreach (var candidate in SortCandidates(schedule, candidates))
                {
                    var row = new ExchangeRow { ElementUniqueId = candidate.Element.UniqueId,
                        Cells = fields.Select((field, c) => CellValue.Text(Display(candidate.Parameters[c], field))).ToList() };
                    sheet.Rows.Add(row);
                    matches.Add(row, candidate);
                }
            }
            else
            {
                var signatures = candidates.GroupBy(c => c.Signature).ToDictionary(g => g.Key, g => g.OrderBy(c => RevitCompatibility.IdValue(c.Element.Id)).ToList());
                foreach (var group in sheet.Rows.GroupBy(r => Signature(matchColumns.Select(c => r.Cells[c].Value))))
                {
                    if (!signatures.TryGetValue(group.Key, out var options)) continue;
                    var rows = group.ToList();
                    if (rows.Count == options.Count && rows.Select(r => Signature(r.Cells.Select(v => v.Value))).Distinct().Count() == 1)
                        for (var i = 0; i < rows.Count; i++) matches.Add(rows[i], options[i]);
                }
                var used = new HashSet<string>(matches.Values.Select(c => c.Element.UniqueId));
                var proposed = new Dictionary<ExchangeRow, Candidate>();
                foreach (var row in sheet.Rows.Where(r => !matches.ContainsKey(r)))
                {
                    var options = candidates.Where(c => !used.Contains(c.Element.UniqueId)).ToList();
                    var anchored = false;
                    foreach (var column in matchColumns.Where(c => candidates.Any(e => e.Parameters[c]?.StorageType == StorageType.String)))
                    {
                        var value = Normalize(row.Cells[column].Value);
                        if (value.Length == 0) continue;
                        options = options.Where(c => Normalize(c.Parameters[column]?.AsString()) == value).ToList();
                        anchored = true;
                    }
                    if (anchored && options.Count == 1) proposed[row] = options[0];
                }
                foreach (var group in proposed.GroupBy(p => p.Value.Element.UniqueId).Where(g => g.Count() == 1))
                    matches.Add(group.Single().Key, group.Single().Value);
                if (matches.Values.Select(c => c.Element.UniqueId).Distinct().Count() != candidates.Count)
                    throw new InvalidOperationException(schedule.Name + ": nie można bezpiecznie przypisać wszystkich ID elementów do wierszy z polami obliczanymi lub podsumowaniami. Eksport został przerwany zamiast zapisania pustych lub błędnych ID.");
            }
            foreach (var match in matches)
            {
                var row = match.Key;
                var candidate = match.Value;
                row.ElementUniqueId = candidate.Element.UniqueId;
                for (var c = 0; c < fields.Count; c++)
                {
                    var parameter = candidate.Parameters[c];
                    if (parameter?.Element is ElementType) row.TypeColumnKeys.Add(sheet.Columns[c].Key);
                    if (parameter == null || parameter.IsReadOnly || !IsParameterField(fields[c]) ||
                        (parameter.StorageType != StorageType.String && parameter.StorageType != StorageType.Double && parameter.StorageType != StorageType.Integer)) continue;
                    var binding = new CellBinding
                    {
                        ColumnKey = sheet.Columns[c].Key, ScheduleUniqueId = schedule.UniqueId, FieldId = fields[c].FieldId.IntegerValue,
                        SourceUniqueId = candidate.Element.UniqueId, OwnerUniqueId = parameter.Element.UniqueId,
                        ParameterKey = RevitModelGateway.ParameterKey(parameter), ParameterId = RevitCompatibility.IdValue(fields[c].ParameterId).ToString(CultureInfo.InvariantCulture),
                        Kind = RevitModelGateway.KindOf(parameter), OriginalRaw = RevitModelGateway.Raw(parameter)
                    };
                    switch (binding.Kind)
                    {
                        case ValueKind.Text: binding.OriginalExcel = CellValue.Text(parameter.AsString()); break;
                        case ValueKind.Boolean: binding.OriginalExcel = CellValue.Boolean(parameter.AsInteger() != 0); break;
                        case ValueKind.Integer: binding.OriginalExcel = CellValue.Number(parameter.AsInteger()); break;
                        default:
                            var spec = RevitCompatibility.DataType(parameter.Definition);
                            var value = parameter.AsDouble();
                            if (RevitCompatibility.IsMeasurable(spec))
                            {
                                using (var format = fields[c].GetFormatOptions())
                                {
                                    var unit = format.UseDefault ? document.GetUnits().GetFormatOptions(spec).GetUnitTypeId() : format.GetUnitTypeId();
                                    binding.UnitId = unit.TypeId;
                                    value = UnitUtils.ConvertFromInternalUnits(value, unit);
                                }
                            }
                            binding.OriginalExcel = CellValue.Number(value);
                            break;
                    }
                    row.Cells[c] = binding.OriginalExcel;
                    row.Bindings.Add(binding);
                }
            }
            if (!sheet.Rows.Any(r => r.Bindings.Count != 0)) sheet.ReadOnlyReason = "Wszystkie parametry są tylko do odczytu";
            return sheet;
        }

        private Candidate MakeCandidate(Element element, IList<ScheduleField> fields, IList<int> matchColumns)
        {
            var parameters = fields.Select(field => !IsParameterField(field) ? null : RevitModelGateway.FindFieldParameter(element, field)).ToArray();
            return new Candidate { Element = element, Parameters = parameters,
                Signature = Signature(matchColumns.Select(c => Display(parameters[c], fields[c]))) };
        }

        private static bool IsParameterField(ScheduleField field) => !field.IsCalculatedField && !field.IsCombinedParameterField &&
            field.ParameterId != ElementId.InvalidElementId && (field.FieldType == ScheduleFieldType.Instance || field.FieldType == ScheduleFieldType.ElementType);

        private static string Display(Parameter parameter, ScheduleField field)
        {
            if (parameter == null) return "";
            if (parameter.StorageType == StorageType.String) return parameter.AsString() ?? "";
            using (var format = field.GetFormatOptions())
            {
                var value = (format.UseDefault ? parameter.AsValueString() : parameter.AsValueString(format));
                if (value != null) return value;
                if (parameter.StorageType == StorageType.ElementId)
                    return parameter.Element.Document.GetElement(parameter.AsElementId())?.Name ?? "";
                if (parameter.StorageType == StorageType.Integer) return parameter.AsInteger().ToString(CultureInfo.CurrentCulture);
                if (parameter.StorageType == StorageType.Double) return parameter.AsDouble().ToString(CultureInfo.CurrentCulture);
                return "";
            }
        }

        private IEnumerable<Candidate> SortCandidates(ViewSchedule schedule, IEnumerable<Candidate> candidates)
        {
            var comparer = Comparer<object>.Create((a, b) =>
            {
                if (a == null) return b == null ? 0 : -1;
                if (b == null) return 1;
                if (a is double x && b is double y) return x.CompareTo(y);
                return StringComparer.CurrentCultureIgnoreCase.Compare(a.ToString(), b.ToString());
            });
            IOrderedEnumerable<Candidate> ordered = null;
            foreach (var group in schedule.Definition.GetSortGroupFields())
            {
                var field = schedule.Definition.GetField(group.FieldId);
                Func<Candidate, object> key = c =>
                {
                    var parameter = RevitModelGateway.FindFieldParameter(c.Element, field);
                    if (parameter == null) return null;
                    if (parameter.StorageType == StorageType.Double) return parameter.AsDouble();
                    if (parameter.StorageType == StorageType.Integer) return (double)parameter.AsInteger();
                    return Display(parameter, field);
                };
                var descending = group.SortOrder == ScheduleSortOrder.Descending;
                ordered = ordered == null ? (descending ? candidates.OrderByDescending(key, comparer) : candidates.OrderBy(key, comparer))
                    : (descending ? ordered.ThenByDescending(key, comparer) : ordered.ThenBy(key, comparer));
            }
            return ordered == null ? candidates.OrderBy(c => RevitCompatibility.IdValue(c.Element.Id)) : ordered.ThenBy(c => RevitCompatibility.IdValue(c.Element.Id));
        }

        private static string Normalize(string text) => Regex.Replace((text ?? "").Trim(), @"\s+", " ");
        private static string Signature(IEnumerable<string> values) => string.Concat(values.Select(v => { var text = Normalize(v); return text.Length + ":" + text; }));
    }
}
