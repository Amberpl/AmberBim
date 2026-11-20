# Docs changes since last sync
- Added: docs/ui-gallery.md.
- Deleted: none.
- Modified: none.

# Consistency
- Scope: standalone gallery and the current/compact Info variants only.
- Existing AboutWindow is available from the about-window feature branch; the gallery PR is stacked on that branch.
- docs/main.md, docs/arch.md, DESIGN.md and a formal compact visual contract are absent in this checkout. The accepted gallery specification defines behavior; the compact layout is an exploratory native preview following the sketch discussed in chat.
- No links, RPC contracts or explicit labels are introduced by ui-gallery.md.
- No STP is supplied; no test suite is added.

# Change list (implementation)
- [x] Create a standalone WPF executable without a Revit project/assembly dependency.
- [x] Share the existing AboutWindow source files and WPF UI version with the add-in.
- [x] Add an independently selectable compact Info variant with the same information/actions.
- [x] Open modal previews and return to the gallery after closing them.
- [x] Exclude gallery sources from the add-in build.

# Verification
- [x] Build the gallery and inspect output/dependencies for Autodesk references.
- [x] Launch the gallery; inspect both Info variants and verify close/Escape/link behavior.
- [x] Confirm the existing AboutWindow files and approved specification remain unchanged.
- [x] Update freeze after successful verification.

# Results
- Standalone Release build passed with zero warnings/errors; dependencies are WPF UI 4.3.0 and WPF UI Abstractions 4.3.0 only, with no Autodesk or add-in assembly dependency.
- Debug2024 add-in build passed; two pre-existing CS0168 warnings remain in CAD commands.
- Native WPF verification exercised both gallery factory selections, modal ownership, version/year, real hyperlink navigation, close buttons, Escape and return to the enabled gallery.
- Current Info measures 460 x 430 DIP; compact Info measures 380 x 261 DIP. Explicit compact minimum dimensions override WPF UI's default 460 x 320 minimum.
- Both rendered window contents were inspected. Formal pixel comparison is not claimed: no approved native compact visual contract exists.
- The original AboutWindow XAML/code-behind and accepted docs/ui-gallery.md are unchanged.
- Local verification helpers and rendered images stay in ignored .tools/; no test suite is committed.
- Start-Gallery.ps1 successfully launched the actual executable using the shared local SDK from the main worktree, even with a runtime-only system dotnet on PATH.
- Freeze updated after successful compilation and native behavior verification.
