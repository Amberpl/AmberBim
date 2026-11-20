using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AmberBim.ScheduleExcel
{
    public static class ScheduleSelection
    {
        public static bool Matches(ScheduleInfo schedule, string search, string categoryId) =>
            (schedule.Name ?? "").IndexOf((search ?? "").Trim(), StringComparison.CurrentCultureIgnoreCase) >= 0 &&
            (categoryId == null || schedule.CategoryId == categoryId);

        public static string DefaultFileName(IEnumerable<ScheduleInfo> schedules)
        {
            var name = schedules.First(s => s.IsSelected).Name ?? "";
            var invalid = Path.GetInvalidFileNameChars();
            name = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).TrimEnd(' ', '.');
            if (name.Length == 0) name = "Zestawienie";
            return name + ".xlsx";
        }
    }
}
