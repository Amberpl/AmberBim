using Autodesk.Revit.DB;

namespace AmberBim.ScheduleExcel
{
    internal static class RevitCompatibility
    {
        internal static long IdValue(ElementId id)
        {
#if REVIT_2021 || REVIT_2022 || REVIT_2023
            return id.IntegerValue;
#else
            return id.Value;
#endif
        }

        internal static ForgeTypeId DataType(Definition definition)
        {
#if REVIT_2021
            return definition.GetSpecTypeId();
#else
            return definition.GetDataType();
#endif
        }

        internal static bool IsMeasurable(ForgeTypeId spec)
        {
#if REVIT_2021
            return UnitUtils.IsSpec(spec) && UnitUtils.GetValidUnits(spec).Count != 0;
#else
            return UnitUtils.IsMeasurableSpec(spec);
#endif
        }

        internal static bool IsYesNo(Definition definition)
        {
#if REVIT_2021
            return definition.ParameterType == ParameterType.YesNo;
#else
            return definition.GetDataType() == SpecTypeId.Boolean.YesNo;
#endif
        }
    }
}
