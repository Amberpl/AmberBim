using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AmberBim.ScheduleExcel
{
    public static class ImportEngine
    {
        public static ImportReport Apply(WorkbookImport workbook, IModelGateway model)
        {
            var report = new ImportReport();
            report.Errors.AddRange(workbook.Errors);
            report.SkippedSheets.AddRange(workbook.SkippedSheets);
            if (!string.Equals(workbook.ModelId, model.ModelId, StringComparison.Ordinal))
                report.Errors.Add("Plik pochodzi z innego modelu Revita.");
            if (report.Errors.Count != 0) return report;

            var changes = new Dictionary<string, ParameterChange>(StringComparer.Ordinal);
            foreach (var edit in workbook.Edits)
            {
                try
                {
                    var binding = edit.Binding;
                    var current = model.Read(binding);
                    if (current.Kind != binding.Kind) throw new InvalidOperationException("Zmieniono rodzaj parametru.");
                    if (ExcelEqual(edit.Value, binding.OriginalExcel)) continue;
                    var proposed = ConvertValue(edit.Value, binding, model);
                    if (RawEqual(binding.Kind, current.Raw, proposed)) continue;
                    if (current.ReadOnly) throw new InvalidOperationException("Parametr jest teraz tylko do odczytu.");
                    if (!RawEqual(binding.Kind, current.Raw, binding.OriginalRaw))
                        throw new InvalidOperationException("Konflikt: wartość w modelu zmieniła się po eksporcie.");
                    var key = binding.OwnerUniqueId + "|" + binding.ParameterKey;
                    if (changes.TryGetValue(key, out var previous) && !RawEqual(binding.Kind, previous.NewRaw, proposed))
                        throw new InvalidOperationException("Sprzeczne wartości tego samego parametru w pliku.");
                    changes[key] = new ParameterChange { Binding = binding, NewRaw = proposed };
                }
                catch (Exception exception) { report.Errors.Add(edit.Location + ": " + exception.Message); }
            }
            if (report.Errors.Count != 0 || changes.Count == 0) return report;
            try
            {
                model.Apply(changes.Values.ToList());
                report.ChangedValues = changes.Count;
                report.ChangedElements = changes.Values.Select(c => c.Binding.OwnerUniqueId).Distinct().Count();
            }
            catch (Exception exception) { report.Errors.Add("Import wycofany w całości: " + exception.Message); }
            return report;
        }

        public static bool RawEqual(ValueKind kind, string first, string second)
        {
            if (kind != ValueKind.Number) return string.Equals(first ?? "", second ?? "", StringComparison.Ordinal);
            return double.TryParse(first, NumberStyles.Float, CultureInfo.InvariantCulture, out var a) &&
                double.TryParse(second, NumberStyles.Float, CultureInfo.InvariantCulture, out var b) && NearlyEqual(a, b);
        }

        private static bool NearlyEqual(double a, double b) => Math.Abs(a - b) <= 1e-10 * Math.Max(1, Math.Max(Math.Abs(a), Math.Abs(b)));

        private static bool ExcelEqual(CellValue first, CellValue second)
        {
            if (first.Type == "number" && second.Type == "number")
                return RawEqual(ValueKind.Number, first.Value, second.Value);
            return first.Type == second.Type && first.Value == second.Value;
        }

        private static string ConvertValue(CellValue value, CellBinding binding, IModelGateway model)
        {
            if (value.Type == "error") throw new InvalidOperationException("Komórka zawiera błąd lub formułę bez zapisanej wartości.");
            if (binding.Kind == ValueKind.Text) return value.Value ?? "";
            if (binding.Kind == ValueKind.Boolean)
            {
                var text = (value.Value ?? "").Trim().ToLowerInvariant();
                if (text == "1" || text == "true" || text == "tak") return "1";
                if (text == "0" || text == "false" || text == "nie") return "0";
                throw new InvalidOperationException("Wymagana wartość Tak/Nie, TRUE/FALSE lub 1/0.");
            }
            var numberText = value.Value ?? "";
            if (value.Type == "text") numberText = numberText.Trim().Replace(',', '.');
            if (!double.TryParse(numberText, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ||
                double.IsNaN(number) || double.IsInfinity(number)) throw new InvalidOperationException("Wymagana poprawna liczba.");
            if (binding.Kind == ValueKind.Integer)
            {
                if (number != Math.Truncate(number) || number < int.MinValue || number > int.MaxValue)
                    throw new InvalidOperationException("Wymagana liczba całkowita.");
                return ((int)number).ToString(CultureInfo.InvariantCulture);
            }
            var raw = model.ToInternal(binding, number);
            if (double.IsNaN(raw) || double.IsInfinity(raw)) throw new InvalidOperationException("Nieprawidłowy wynik przeliczenia jednostek.");
            return raw.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
