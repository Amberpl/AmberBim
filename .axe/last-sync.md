# Docs changes since last sync
- Added: none
- Deleted: none
- Modified: docs/revit-compatibility.md, Release file layout section.

# Consistency
- about-window.md is unchanged.
- The approved Release layout retains the manifest's root assembly path and requires dependency probing from AmberBIM for all supported frameworks.
- Debug keeps its existing flat output.
- There is no new STP, mockup, RPC contract or label in the delta. No test suite or visual contract is inferred.

# Change list (implementation)
- [x] Move all Release output support files into AmberBIM, keeping only AmberBim.dll and AmberBim.addin at the root.
- [x] Preserve nested dependency paths and include relocated files in MSBuild clean tracking.
- [x] Resolve private dependencies from AmberBIM before creating the WPF window, without redirecting unrelated requesting assemblies.
- [x] Preserve flat Debug layout and document Release deployment.

# Verification
- [x] Compile all 18 Debug/Release variants and inspect each layout.
- [x] Exercise dependency loading and the WPF window from an isolated Release deployment on net48, net8 and net10.
- [x] Verify incremental build and Clean/Rebuild behavior.
- [x] Verify Debug runtime and unrelated assembly requests remain unaffected.

# Results
- All 18 Debug/Release builds succeeded, including both net8 and net10 variants for Revit 2025/2026.
- Every Release output root contains only AmberBim.dll and AmberBim.addin. Dependencies, symbols, config and deps files are under AmberBIM. Debug retains the flat layout.
- Relocated files participate in FileWrites tracking. Incremental builds, Clean and rebuilding were verified for Revit 2024/net48, 2025/net8 and 2027/net10. Clean removed all generated output files.
- A separate layout fixture confirmed nested support-file paths are preserved.
- Isolated preview hosts have no WPF UI package or add-in compile reference. Release previews on net48/net8/net10 instantiated the WPF window and confirmed WPF UI libraries load from AmberBIM. Modal ownership and button/Escape/system-close behavior passed.
- Debug previews passed on all three frameworks with WPF UI loaded from the original flat directory.
- Resolver checks reject unrelated requesting assemblies, unknown dependencies and mismatched dependency versions. Initialization is idempotent and shutdown unregisters the handler.
- All 18 output inspections passed: x64/framework/API/version metadata, WPF UI dependencies, compiled XAML, existing ribbon images and manifest, and no copied Autodesk DLLs.
- Integration inside Revit has not been exercised. Existing compilation warnings remain.
- Verification scripts and fixture files stay in ignored .tools/; no repository test suite or docs changes were added.
