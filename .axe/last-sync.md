# Docs changes since last sync
- Added: docs/about-window.md
- Deleted: none
- Modified: none

# Consistency
- The approved about-window specification is the complete delta; revit-compatibility.md is unchanged.
- docs/main.md, docs/arch.md, a visual mockup and STP do not exist. No pixel contract or new test suite is inferred; the approved behavior is implemented using standard WPF UI controls.
- The website URL matches the approved external destination.
- Version 0.3 must agree between the dialog, assembly metadata and add-in manifest.

# Change list (implementation)
- [x] Replace Info's TaskDialog with a modal WPF UI window owned by Revit.
- [x] Show AmberBIM Tools, version 0.3, dynamic current-year copyright and author.
- [x] Open the approved website using Windows shell association.
- [x] Support closing with Zamknij, Escape and the window close button.
- [x] Set assembly/file versions to 0.3.0.0 and manifest description to 0.3.
- [x] Include compatible WPF UI dependencies in all build outputs, with resources scoped to the dialog.

# Verification
- [x] Compile all 18 Revit 2021–2027 configurations.
- [x] Inspect binaries, version metadata, manifest and dependency output.
- [x] Render the WPF window outside Revit and verify close behavior and current-year text.
- [ ] Revit integration has not been exercised inside Revit. The standalone preview verified native ownership, owner disabling and re-enabling.

# Results
- WPF-UI 4.3.0 is pinned. Dialog resources are scoped locally; no global Revit application resources are changed.
- All 18 Debug/Release configurations compiled: 2021–2027 and net10 variants for 2025/2026.
- All 18 outputs contain assembly/file version 0.3.0.0, the version 0.3 manifest, compiled AboutWindow XAML, WPF UI dependencies and the existing nine ribbon images. Autodesk DLLs are not copied.
- Standalone previews on .NET Framework 4.8, .NET 8 and .NET 10 passed native owner/modal checks, version/current-year text checks and close-button, Escape and system-close checks. PNG previews were visually inspected.
- Website handling uses the approved HTTPS URL with Windows shell execution; the browser was not launched during verification.
- Existing compile warnings include unused CAD exception variables, deprecated solid-rebar API for Revit 2023, and installed Revit API dependency version conflicts. All builds succeeded.
- Verification utilities, downloaded packages and screenshots remain in ignored .tools/; no new repository test suite was added.
- The approved docs are unchanged.
