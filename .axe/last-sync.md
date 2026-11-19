# Docs changes since last sync
- Added: docs/ribbon-icons.md, docs/mockup/DESIGN.md, docs/mockup/ribbon-icons.png (binary reference, recorded by hash).
- Deleted: none
- Modified: none

# Consistency
- The design link resolves to the accepted seven-icon light/dark board.
- Legacy commands remain limited to 2021–2023; icon changes do not add or remove commands.
- 2021–2023 always use light assets with no theme access or subscription. 2024–2027 select at startup and update on ThemeChanged.
- Existing about-window and Revit compatibility specifications are unchanged.
- docs/main.md, docs/arch.md and an STP do not exist. The approved behavior/design files are the bounded delta. No new labels or test suite are inferred.

# Change list (implementation)
- [x] Produce 28 transparent PNG resources: seven icons, two themes, 16/32 px, following the accepted board.
- [x] Keep reproducible vector sources and generation tooling for precise small-size rendering.
- [x] Assign Image and LargeImage on all actual ribbon buttons, including stacked items.
- [x] Compile theme detection/events only for Revit 2024–2027, with startup selection and shutdown unsubscription.
- [x] Keep Revit 2021–2023 light-only without theme calls.

# Verification
- [x] Inspect both themes at native sizes and compare the family to the accepted design board.
- [x] Validate PNG dimensions, transparency, resource loading and light/dark differences.
- [x] Compile all 18 configurations and inspect legacy/modern API references.
- [x] Verify theme resource changes with standalone previews; actual Revit theme event integration remains unexercised.

# Results
- Added 28 transparent PNG resources (7 symbols × 2 themes × 2 sizes), 14 corresponding SVGs and deterministic WPF geometry-based generation tooling. The native-size light/dark preview was visually compared with the approved board; obscured bars retain visible gaps.
- Image and LargeImage are assigned to actual PushButton objects, including both stacked rebar pairs. Frozen bitmaps are cached and reused when a theme returns.
- Revit 2021–2023 binaries contain no UIThemeManager.CurrentTheme calls, ThemeChanged subscriptions/unsubscriptions or theme handler. They always initialize light icons.
- Revit 2024–2027 binaries contain startup theme selection, ThemeChanged subscription/update and shutdown unsubscription. Repeated notifications for the same theme reuse existing images.
- All 18 Debug/Release configurations compiled and passed metadata/resource inspections. All 28 new PNGs and compiled AboutWindow are embedded; Release folder layout and dependencies remain intact.
- Standalone resource loading passed for Revit 2021/net48, 2024/net48, 2025/net8 and 2027/net10. Verified image dimensions, alpha transparency, byte-for-byte embedded PNG equality, different light/dark colors, identical theme silhouettes, frozen images and cache reuse.
- Actual theme switching inside Revit has not been exercised. Existing compilation warnings remain.
- The accepted docs and reference PNG are unchanged. Freeze stores the reference image hash, not a binary copy.
- Verification utilities stay in ignored .tools/; no repository test suite was added.
