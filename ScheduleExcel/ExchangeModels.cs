using System;
using System.Collections.Generic;
using System.Globalization;

namespace AmberBim.ScheduleExcel
{
    public enum ValueKind { Text, Integer, Number, Boolean }

    public sealed class CellValue
    {
        public string Type { get; set; } = "text";
        public string Value { get; set; } = "";
        public static CellValue Text(string value) => new CellValue { Value = value ?? "" };
        public static CellValue Number(double value) => new CellValue { Type = "number", Value = value.ToString("R", CultureInfo.InvariantCulture) };
        public static CellValue Boolean(bool value) => new CellValue { Type = "boolean", Value = value ? "1" : "0" };
    }

    public sealed class ScheduleInfo
    {
        public string UniqueId { get; set; }
        public string Name { get; set; }
        public string CategoryId { get; set; }
        public string CategoryName { get; set; }
        public bool ReadOnly { get; set; }
        public string Reason { get; set; }
        public bool IsSelected { get; set; }
        public string Status => ReadOnly ? "Tylko do odczytu: " + Reason : "Eksport i import parametrów";
    }

    public sealed class ExchangeColumn
    {
        public string Key { get; set; } = Guid.NewGuid().ToString("N");
        public string Heading { get; set; }
        public string ParameterId { get; set; } = "";
        public bool IsType { get; set; }
    }

    public sealed class CellBinding
    {
        public string ColumnKey { get; set; }
        public string ScheduleUniqueId { get; set; }
        public int FieldId { get; set; }
        public string SourceUniqueId { get; set; }
        public string OwnerUniqueId { get; set; }
        public string ParameterKey { get; set; }
        public string ParameterId { get; set; }
        public ValueKind Kind { get; set; }
        public string OriginalRaw { get; set; }
        public string UnitId { get; set; } = "";
        public CellValue OriginalExcel { get; set; }
    }

    public sealed class ExchangeRow
    {
        public string Key { get; set; } = Guid.NewGuid().ToString("N");
        public string ElementUniqueId { get; set; } = "";
        public List<CellValue> Cells { get; set; } = new List<CellValue>();
        public List<CellBinding> Bindings { get; set; } = new List<CellBinding>();
        public HashSet<string> TypeColumnKeys { get; set; } = new HashSet<string>();
    }

    public sealed class ExchangeSheet
    {
        public string Key { get; set; } = Guid.NewGuid().ToString("N");
        public string ScheduleUniqueId { get; set; }
        public string Name { get; set; }
        public string ReadOnlyReason { get; set; } = "";
        public List<ExchangeColumn> Columns { get; set; } = new List<ExchangeColumn>();
        public List<ExchangeRow> Rows { get; set; } = new List<ExchangeRow>();
    }

    public sealed class ExchangeDocument
    {
        public string ModelId { get; set; }
        public List<ExchangeSheet> Sheets { get; set; } = new List<ExchangeSheet>();
    }

    public sealed class CellEdit
    {
        public CellBinding Binding { get; set; }
        public CellValue Value { get; set; }
        public string Location { get; set; }
    }

    public sealed class WorkbookImport
    {
        public string ModelId { get; set; }
        public List<CellEdit> Edits { get; } = new List<CellEdit>();
        public List<string> Errors { get; } = new List<string>();
        public List<string> SkippedSheets { get; } = new List<string>();
    }

    public sealed class ExportReport
    {
        public string Path { get; set; }
        public int SheetCount { get; set; }
    }

    public sealed class ImportReport
    {
        public int ChangedElements { get; set; }
        public int ChangedValues { get; set; }
        public List<string> SkippedSheets { get; } = new List<string>();
        public List<string> Errors { get; } = new List<string>();
    }

    public sealed class ParameterState
    {
        public ValueKind Kind { get; set; }
        public string Raw { get; set; }
        public bool ReadOnly { get; set; }
    }

    public sealed class ParameterChange
    {
        public CellBinding Binding { get; set; }
        public string NewRaw { get; set; }
    }

    public interface IModelGateway
    {
        string ModelId { get; }
        ParameterState Read(CellBinding binding);
        double ToInternal(CellBinding binding, double value);
        void Apply(IReadOnlyList<ParameterChange> changes);
    }
}
