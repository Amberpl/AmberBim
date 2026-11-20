using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Autodesk.Revit.DB;

namespace AmberBim.ScheduleExcel
{
    public sealed class RevitModelGateway : IModelGateway
    {
        private readonly Document document;
        private readonly Dictionary<string, HashSet<string>> scheduleMembers = new Dictionary<string, HashSet<string>>();
        public RevitModelGateway(Document document) { this.document = document; }
        public string ModelId => document.ProjectInformation.UniqueId;

        public ParameterState Read(CellBinding binding)
        {
            var parameter = Resolve(binding);
            return new ParameterState { Kind = KindOf(parameter), Raw = Raw(parameter), ReadOnly = parameter.IsReadOnly };
        }

        public double ToInternal(CellBinding binding, double value)
        {
            var parameter = Resolve(binding);
            var spec = RevitCompatibility.DataType(parameter.Definition);
            if (RevitCompatibility.IsMeasurable(spec))
            {
                var unit = new ForgeTypeId(binding.UnitId);
                if (!UnitUtils.IsValidUnit(spec, unit)) throw new InvalidOperationException("Jednostka w pliku nie odpowiada parametrowi.");
                return UnitUtils.ConvertToInternalUnits(value, unit);
            }
            if (!string.IsNullOrEmpty(binding.UnitId)) throw new InvalidOperationException("Parametr nie obsługuje zapisanej jednostki.");
            return value;
        }

        public void Apply(IReadOnlyList<ParameterChange> changes)
        {
            if (document.IsReadOnly) throw new InvalidOperationException("Dokument jest tylko do odczytu.");
            using (var transaction = new Transaction(document, "AmberBIM — import zestawień z Excela"))
            {
                transaction.Start();
                var options = transaction.GetFailureHandlingOptions();
                options.SetFailuresPreprocessor(new ImportFailures());
                options.SetClearAfterRollback(true);
                transaction.SetFailureHandlingOptions(options);
                try
                {
                    foreach (var change in changes)
                    {
                        var parameter = Resolve(change.Binding);
                        var current = Raw(parameter);
                        if (parameter.IsReadOnly || (!ImportEngine.RawEqual(change.Binding.Kind, current, change.Binding.OriginalRaw) &&
                            !ImportEngine.RawEqual(change.Binding.Kind, current, change.NewRaw)))
                            throw new InvalidOperationException("Parametr zmienił się lub został zablokowany przed zapisem.");
                        bool written;
                        switch (change.Binding.Kind)
                        {
                            case ValueKind.Text: written = parameter.Set(change.NewRaw); break;
                            case ValueKind.Integer:
                            case ValueKind.Boolean: written = parameter.Set(int.Parse(change.NewRaw, CultureInfo.InvariantCulture)); break;
                            default: written = parameter.Set(double.Parse(change.NewRaw, CultureInfo.InvariantCulture)); break;
                        }
                        if (!written) throw new InvalidOperationException("Revit odrzucił zapis parametru „" + parameter.Definition.Name + "”.");
                    }
                    if (transaction.Commit() != TransactionStatus.Committed) throw new InvalidOperationException("Revit nie zatwierdził transakcji importu.");
                }
                catch
                {
                    if (transaction.GetStatus() == TransactionStatus.Started) transaction.RollBack();
                    throw;
                }
            }
        }

        private Parameter Resolve(CellBinding binding)
        {
            var schedule = document.GetElement(binding.ScheduleUniqueId) as ViewSchedule;
            if (schedule == null || !string.IsNullOrEmpty(RevitScheduleExchange.ReadOnlyReason(schedule)))
                throw new InvalidOperationException("Zestawienie nie istnieje lub jest teraz tylko do odczytu.");
            if (!scheduleMembers.TryGetValue(binding.ScheduleUniqueId, out var members))
            {
                members = new HashSet<string>(new FilteredElementCollector(document, schedule.Id).WhereElementIsNotElementType().Select(e => e.UniqueId));
                scheduleMembers.Add(binding.ScheduleUniqueId, members);
            }
            if (!members.Contains(binding.SourceUniqueId)) throw new InvalidOperationException("Element nie należy do eksportowanego zestawienia.");
            var source = document.GetElement(binding.SourceUniqueId);
            var owner = document.GetElement(binding.OwnerUniqueId);
            if (source == null || owner == null || (source.Id != owner.Id && source.GetTypeId() != owner.Id))
                throw new InvalidOperationException("Nieznany element lub zmieniony typ elementu.");
            var field = schedule.Definition.GetFieldOrder().Select(id => schedule.Definition.GetField(id))
                .SingleOrDefault(f => f.FieldId.IntegerValue == binding.FieldId);
            if (field == null || field.IsHidden || field.IsCalculatedField || field.IsCombinedParameterField ||
                (field.FieldType != ScheduleFieldType.Instance && field.FieldType != ScheduleFieldType.ElementType))
                throw new InvalidOperationException("Pole zestawienia zostało usunięte lub nie jest edytowalnym parametrem.");
            var parameter = FindFieldParameter(source, field);
            if (parameter == null || RevitCompatibility.IdValue(field.ParameterId).ToString(CultureInfo.InvariantCulture) != binding.ParameterId ||
                parameter.Element.Id != owner.Id || ParameterKey(parameter) != binding.ParameterKey || KindOf(parameter) != binding.Kind)
                throw new InvalidOperationException("Nieznany lub niezgodny parametr.");
            if (field.FieldType == ScheduleFieldType.ElementType && source.GetTypeId() != owner.Id)
                throw new InvalidOperationException("Nieprawidłowe powiązanie parametru typu.");
            if (field.FieldType == ScheduleFieldType.Instance && source.Id != owner.Id && !parameter.IsShared)
                throw new InvalidOperationException("Nieprawidłowe powiązanie parametru wystąpienia.");
            return parameter;
        }

        internal static Parameter FindParameter(Element owner, ElementId id) => owner == null ? null
            : owner.Parameters.Cast<Parameter>().FirstOrDefault(p => p.Id == id);

        internal static Parameter FindFieldParameter(Element source, ScheduleField field)
        {
            var type = source.Document.GetElement(source.GetTypeId());
            if (field.FieldType == ScheduleFieldType.ElementType) return FindParameter(type, field.ParameterId);
            var instance = FindParameter(source, field.ParameterId);
            if (instance != null) return instance;
            var sharedType = FindParameter(type, field.ParameterId);
            return sharedType != null && sharedType.IsShared ? sharedType : null;
        }

        internal static string ParameterKey(Parameter parameter) => parameter.IsShared ? "shared:" + parameter.GUID.ToString("D")
            : "id:" + RevitCompatibility.IdValue(parameter.Id).ToString(CultureInfo.InvariantCulture);

        internal static ValueKind KindOf(Parameter parameter)
        {
            if (parameter.StorageType == StorageType.String) return ValueKind.Text;
            if (parameter.StorageType == StorageType.Double) return ValueKind.Number;
            if (parameter.StorageType == StorageType.Integer)
                return RevitCompatibility.IsYesNo(parameter.Definition) ? ValueKind.Boolean : ValueKind.Integer;
            throw new InvalidOperationException("Parametr jest odwołaniem do elementu lub ma nieobsługiwany typ.");
        }

        internal static string Raw(Parameter parameter)
        {
            switch (parameter.StorageType)
            {
                case StorageType.String: return parameter.AsString() ?? "";
                case StorageType.Integer: return parameter.AsInteger().ToString(CultureInfo.InvariantCulture);
                case StorageType.Double: return parameter.AsDouble().ToString("R", CultureInfo.InvariantCulture);
                default: throw new InvalidOperationException("Nieobsługiwany typ parametru.");
            }
        }

        private sealed class ImportFailures : IFailuresPreprocessor
        {
            public FailureProcessingResult PreprocessFailures(FailuresAccessor accessor)
            {
                foreach (var failure in accessor.GetFailureMessages())
                {
                    if (failure.GetSeverity() == FailureSeverity.Warning) accessor.DeleteWarning(failure);
                    else return FailureProcessingResult.ProceedWithRollBack;
                }
                return FailureProcessingResult.Continue;
            }
        }
    }
}
