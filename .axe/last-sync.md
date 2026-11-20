# Docs changes since last sync
- Added: docs/schedule-excel/sdd.md and docs/schedule-excel/stp.md.
- Modified/deleted: none.

# Approved delta, 2026-11-20: export presentation and categories
- SDD and STP agree on schedule-based filenames, no export success dialog, schedules before Info, type-cell colors, WPF UI import report, category display and combined filtering.
- [x] Implement workbook order, styles, filename and import report.
- [x] Implement schedule category and category header filter preserving selection.
- [x] Verify standalone STP cases and compile Release2024.
- All 12 standalone scenario groups passed, including OpenXML schema validation, active schedule / Info order, type and readonly styling, filename sanitization, combined category/name filters and preserved selection.
- Release2024 compiled; two pre-existing CAD CS0168 warnings remain. Native category extraction, filtering interaction and report appearance await user testing in Revit 2024.
- Native verification remains assigned to the user; freeze stays unchanged.

# Consistency
- The accepted scope is XLSX files, multiple selected schedules, immediate validated import and grouped/read-only schedules skipped during import.
- There is no docs/main.md / docs/arch.md or approved visual mockup for this service. Standard existing WPF UI controls implement the behavioral flow; no new layout contract is inferred.
- The user subsequently authorized building the remaining Revit versions and publishing the feature branch through a PR. Native testing remains assigned to the user.

# Implementation
- [x] XLSX writer/reader with model metadata, UniqueId in hidden column A and parameter IDs in hidden row 1 (format 2).
- [x] Import validation/planning with units, conflicts, duplicate targets, read-only handling and idempotency.
- [x] Revit schedule extraction and atomic model transaction implemented; native model verification remains pending user testing.
- [x] Ribbon export/import commands and WPF multi-schedule selection. Excel panel precedes Info as requested.

# Verification from STP
- [x] Workbook roundtrip, multiple sheets, naming, Unicode/numbers and moved rows/columns.
- [x] Read-only grouped schedules, text/numeric/bool/type updates and units in standalone tests.
- [x] Wrong model, invalid IDs/values, duplicates, stale values and idempotency in standalone tests.
- [x] Atomic rollback contract checked with an injected write failure; native transaction check remains pending.
- [x] Debug2024 compiled during initial development; current corrected implementation compiled as Release2024.
- [ ] Actual Revit 2024 model export/import checks.

Freeze stays unchanged until required verification succeeds.

# Accepted corrections and local verification, 2026-11-20
- The accepted SDD/STP now require parameter IDs above names. Format 2 writes IDs as text in hidden row 1 and keeps labels in row 2. Caption changes and duplicate captions no longer affect mapping. Missing, unknown or duplicate parameter IDs prevent writes.
- Plain itemized schedules with direct parameter fields and no summary decorations build rows directly from collected Revit elements. Repeated values do not affect ID assignment. Complex native tables retain conservative matching; unresolved associations raise an export error rather than creating blank/wrong element IDs.
- Shared type parameters are resolved on the actual type even when Revit reports ScheduleFieldType.Instance.
- All 10 standalone scenario groups passed, including schema validation, ID/header placement, repeated values, column/row reorder, caption independence, malformed parameter IDs, data types/units, type conflicts, stale model values, rollback contract, readonly/Info handling and idempotency.
- Release2024 compiled successfully. Two pre-existing unused-exception warnings remain in CAD commands.
- The user will run native Revit tests. No further Revit launches, temporary test add-ins, push or remote PR were performed. Freeze remains unchanged pending native verification.

# Multi-version build and publication, 2026-11-20
- User authorized builds for remaining versions and a feature-branch push with PR.
- Added version-specific API adapters for numeric ElementId values, parameter data types, measurable specs and Yes/No parameters; modern IDs remain 64-bit.
- All 18 configurations passed: Debug/Release 2021–2027 plus .NET 10 variants for 2025 and 2026. API versions match each configuration.
- No native Revit sessions were launched; freeze remains unchanged pending native verification.
