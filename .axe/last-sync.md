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
- Current Info measures 460 x 430 DIP; compact Info measures 380 x 213 DIP. Explicit compact minimum dimensions override WPF UI's default 460 x 320 minimum.
- Both rendered window contents were inspected. Formal pixel comparison is not claimed: no approved native compact visual contract exists.
- The original AboutWindow XAML/code-behind and accepted docs/ui-gallery.md are unchanged.
- Local verification helpers and rendered images stay in ignored .tools/; no test suite is committed.
- Start-Gallery.ps1 successfully launched the actual executable using the shared local SDK from the main worktree, even with a runtime-only system dotnet on PATH.
- Freeze updated after successful compilation and native behavior verification.


# Compact preview refinement
- User requested moving Close upwards and placing the website link at the bottom.
- Close is aligned beside the author/copyright group; the link is the last content row.
- Gallery specification and freeze are unchanged: this refines the exploratory compact variant within the accepted scope.
- Rebuilt and inspected the compact render; native Close/Escape/link checks passed for both variants.

# Footer alignment refinement
- User requested bottom alignment of Close and the website address, with bottom spacing equal to the side spacing.
- Close and the address share a footer grid, both bottom-aligned. Content margins are 20 DIP left/right/bottom.
- Start-Gallery.ps1 -CompactInfo displays the compact window directly for user review.
- The gallery was rebuilt and launched with that preview. Per user instruction, no automated behavior or visual checks were run; the user owns verification.
- The approved gallery specification and freeze remain unchanged; this is an exploratory layout refinement.
# Schedule export preview
- User requested displaying the schedule export window with a funnel icon replacing the category dropdown.
- Added a standalone gallery preview based on the existing ScheduleExportWindow layout, using example schedules and no Revit dependencies.
- The category header funnel opens a checked category menu with an All Categories option; the selected category combines with name search and highlights the funnel.
- Added the preview to the gallery and Start-Gallery.ps1 -ScheduleExport for direct display.
- The gallery was rebuilt and the export preview launched. No automated behavior/visual verification was performed: review remains with the user.
- Production schedule export files and documentation are unchanged; this is a gallery design variant.
# Export table readability refinement
- User requested removing the hint below the table and making the table clearer.
- Removed the lower hint and its layout row; kept the introduction above the table.
- Added 40-DIP rows, 44-DIP headers, padded and vertically centered cell text, horizontal grid lines and subtle alternating backgrounds.
- Increased space for schedule names, centered single-click selection checkboxes and shortened mode labels to Edytowalne / Tylko do odczytu; details remain in tooltips.
- Rebuilt and displayed the export preview. User owns visual/behavior review; no automated checks were run.
# Stable export row selection
- User reported selected rows shifting horizontally.
- Replaced inherited row/cell templates with fixed geometry: zero margins/padding/borders, a stationary cells presenter and background-only row selection/hover states.
- Keyboard focus uses an overlay outline that does not change the content layout.
- Rebuilt and displayed the export preview. No interaction or visual checks were run; verification remains with the user.