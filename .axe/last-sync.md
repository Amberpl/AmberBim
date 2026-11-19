# Docs changes since last sync
- Added: docs/revit-compatibility.md (approved in the preceding turn)
- Deleted: none
- Modified: none

No freeze or docs/main.md / docs/arch.md exists. This sync implements only the just-approved compatibility specification, following axe's just-written-spec exception. The approved file is the scope; no unrelated application specification is inferred.

# Consistency
- The specification has no links, labels, RPC contracts or mockup dependencies.
- Per-year configurations, framework selection and legacy rebar command availability agree.
- There is no STP; no new test code is derived.

# Change list (implementation)
- [x] Configure per-year Debug/Release x64 builds and isolated outputs.
- [x] Select net48 / net8.0-windows / net10.0-windows and matching API references with path override and clear validation.
- [x] Exclude unavailable solid-rebar commands from Revit 2024–2027; preserve the remaining ribbon commands.
- [x] Open CAD files with Windows shell association across frameworks.
- [x] Copy the add-in manifest and eliminate unused incompatible references.

# Verification
- [x] Compile Debug and Release for 2021–2027 plus net10 variants for 2025–2026.
- [x] Verify invalid versions, missing references and isolated output content.
- [x] Verify ribbon command availability in compiled assemblies and manifest contents.
- [ ] Runtime checks inside Revit require a separate interactive smoke check; not performed in this compile-focused sync.

# Results
- All 18 Debug/Release builds passed: 2021–2027 and net10 variants for 2025–2026.
- API sources: installed Revit 2022, 2023, 2024, 2025.5 (.NET 10), 2027; temporary NuGet reference packages Revit_All_Main_Versions_API_x64 2021.0.0, 2025.0.0 (.NET 8), 2026.0.0 (.NET 8), 2026.5.0 (.NET 10).
- All 18 binaries passed metadata/content inspection: x64, target framework, matching API majors, public ribbon command classes, legacy solid command/API exclusion from 2024 onward, nine embedded images, unchanged manifest, no Autodesk DLLs in output.
- Six negative configuration checks passed: unsupported year, missing API, mismatched API year, conflicting configuration/year, wrong framework override, invalid net10 option for legacy Revit.
- Default project evaluation selects Revit 2027 / net10.0-windows.
- Existing warnings remain: unused exception variables in CAD commands and deprecated SetSolidInView for the deliberately retained Revit 2023 commands.
- SDK 10.0.404 and all verification dependencies remain local in ignored .tools/; no system SDK installation was performed.
- docs/revit-compatibility.md is unchanged from the approved specification.
