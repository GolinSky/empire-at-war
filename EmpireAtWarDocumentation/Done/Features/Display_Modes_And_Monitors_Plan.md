---
category: Features
status: done
created: 2026-11-04
completed: 2026-11-04
acceptance: pending-user
---
# Display Modes and Monitors Plan

## Goal

- Let the player set a resolution in every window mode it applies to.
- Add a refresh-rate choice and a monitor choice to Settings → Display.
- Platforms: Windows and macOS standalone. Unity `6000.4.7f1`.

## Original State (verified before implementation 2026-11-04)

- `DisplaySettingsApplier.Apply` (`Services/Graphics/DisplaySettingsApplier.cs:39`) forces `DesktopResolution` when mode is `Borderless`.
- `SettingsDraftEditor.Refresh` (`Entities/MainMenu/Settings/SettingsDraftEditor.cs:76`) disables the resolution dropdown in `Borderless`.
- Default mode is `Borderless` (`DisplaySettingsData.windowMode`) → by default the player cannot pick a resolution. This is the reported limitation.
- `Resolutions` dedupes `Screen.resolutions` by size and drops `refreshRateRatio` → no refresh rate stored or applied.
- `ExclusiveFullscreen` is offered on macOS, where Unity does not support it.
- No monitor selection; window opens on the display Unity picks.
- `ProjectSettings`: `fullscreenMode: 1` (FullScreenWindow), `resizableWindow: 0`, `defaultIsNativeResolution: 1`, `allowFullscreenSwitch: 1`, `macRetinaSupport: 1`.

## Official Unity APIs (Context7, Unity 6000.6 docs)

| Need | API | Notes |
|---|---|---|
| Set size + mode | `Screen.SetResolution(w, h, FullScreenMode)` | Applied at end of frame. Unsupported size → closest. |
| Set size + mode + Hz | `Screen.SetResolution(w, h, FullScreenMode, RefreshRate)` | Hz change only in `ExclusiveFullScreen`. No match → highest. |
| Supported sizes/Hz | `Screen.resolutions` → `Resolution.width/height/refreshRateRatio` | Only for the monitor the window is on now. macOS notch → some entries resized. |
| Current monitor mode | `Screen.currentResolution` | Desktop size + Hz in windowed/borderless. |
| Monitor list | `Screen.GetDisplayLayout(List<DisplayInfo>)` | Win/macOS/Linux. No GC outside Editor. `DisplayInfo`: `name`, `width`, `height`, `refreshRate`, `workArea`. |
| Current monitor | `Screen.mainWindowDisplayInfo` | |
| Move window | `Screen.MoveMainWindowTo(in DisplayInfo, Vector2Int)` → `AsyncOperation` | Async, can take several frames. |

### Window modes per platform

| Mode (`FullScreenMode`) | Windows | macOS | Resolution behaviour |
|---|---|---|---|
| `ExclusiveFullScreen` | Yes | No | Changes OS display mode; use only `Screen.resolutions` entries (others hurt perf). Only mode that changes Hz. |
| `FullScreenWindow` (borderless) | Yes | Yes | Renders at script-set size, scales to fill, letterboxes to keep aspect. Arbitrary sizes, no perf penalty. |
| `MaximizedWindow` | Yes (maximized with title bar) | Yes (native fullscreen, hidden menu bar/dock) | |
| `Windowed` | Yes | Yes | Window = app resolution. |

- Conclusion: the borderless limitation is ours, not Unity's. Borderless officially supports any size.
- Refresh rate can be **chosen** only in Exclusive (Windows). Elsewhere it is **read-only** = monitor Hz.
- Avoid `Display.displays` / `Display.Activate`: multi-display rendering, not window placement.

## Decision

- Borderless: enable resolution dropdown; Unity scales + letterboxes. Default stays "desktop" (`0×0`). Confirmed by user 2026-11-04.
  - Why: official behaviour; matches player expectation.
  - Overlap: `GraphicsSettingsData.RenderResolution` (render scale) already lowers GPU cost. Keep both; resolution = output size, render scale = 3D buffer.
- Refresh rate row: enabled only in Exclusive; other modes show current monitor Hz, disabled.
- Monitor row: list from `Screen.GetDisplayLayout`; persisted by `name` + index (names can repeat).
- macOS: hide `ExclusiveFullscreen`; saved value sanitized to `Borderless`.
- Keep `resizableWindow: 0` (out of scope).

## Implementation

### Phase 1 — Borderless resolution + platform modes
- [x] `DisplaySettingsApplier.Apply`: drop the Borderless → desktop override; desktop only when `UsesDesktopResolution`.
- [x] `SettingsDraftEditor.Refresh`: resolution row interactable in all modes.
- [x] Window-mode list per platform via an `IDisplayOptions.WindowModes` member (Win: 3 modes; macOS: Windowed, Borderless).
- [x] `DisplaySettingsData.Sanitize`: unsupported mode on this platform → `Borderless` (needs platform input; keep data pure — pass supported modes in).
- [x] Exclusive: snap saved size to a `Screen.resolutions` entry before applying.

### Phase 2 — Refresh rate
- [x] `DisplaySettingsData`: add `refreshNumerator` / `refreshDenominator` (0 = monitor default); include in `Matches`, `Clone`, `Sanitize`. Save format stays compatible (missing → 0).
- [x] `IDisplayOptions`: `RefreshRatesFor(Vector2Int size)` from `Screen.resolutions`; `CurrentRefreshRate` from `Screen.currentResolution.refreshRateRatio`.
- [x] Applier: Exclusive → `SetResolution(w, h, mode, RefreshRate)`; other modes ignore stored Hz.
- [x] UI row "Refresh Rate" (`SettingsDropdownRow`), labels `59.94 Hz` / `144 Hz` (round `RefreshRate.value` to 2 dp, trim `.00`).
- [x] Resolution change → reset Hz to highest available for that size if old Hz missing.
- [x] FPS-cap preset `Monitor (N Hz)` accepted by user 2026-11-04; persisted sentinel resolves after display application.

### Phase 3 — Monitor selection
- [x] `IDisplayOptions`: `Monitors` (`Screen.GetDisplayLayout`), `CurrentMonitor` (`Screen.mainWindowDisplayInfo`).
- [x] `DisplaySettingsData`: `monitorName` + `monitorIndex`; empty = current/primary.
- [x] Applier: monitor differs → `Screen.MoveMainWindowTo(display, pos)`; await `AsyncOperation`, then `SetResolution` (resolution list belongs to the new monitor).
  - Windowed: centre in `workArea`. Fullscreen modes: `Vector2Int.zero`.
- [x] Apply becomes async-capable for display; `DisplayConfirmationCountdown` starts after move + resize complete.
- [x] Monitor change → rebuild resolution + Hz lists from new monitor; reset unsupported size to desktop.
- [x] Saved monitor not connected → fall back to `mainWindowDisplayInfo`, no error.
- [x] Startup: apply monitor before first `SetResolution` so the game opens on the saved display.

### Phase 4 — UI + verification
- [x] Add Monitor and Refresh Rate rows to `SettingsUi.prefab` Display group; root interaction group assigned. Imported and saved through Unity.
- [x] Update Penpot `06 · Settings` in the verified EmpireAtWar/Drafts file; editable Monitor/Refresh Rate rows, read-only non-Exclusive Hz, monitor FPS example and borderless behavior description saved and visually checked.
- [x] Keep/Revert prompt covers mode, size, Hz and monitor; Revert restores all four, including arbitrary borderless sizes.
- [x] Source checks + Unity compilation/import completed; No compile/import/serialization errors found; unrelated exception logs from the user's existing test run were distinguished. Explicitly authorized follow-up: `Ui` 245/245 and `Core` 136/136 passed on 2026-11-04.
- [x] User gates transferred to [[TODOs/User_Acceptance_Queue]]: Windows single/dual monitor 60/144 Hz; macOS Retina/notch/external display.

## Edge Cases

- Window on monitor B, `Screen.resolutions` lists B only → lists must be read after move completes.
- Monitor unplugged while in Exclusive → OS moves window; next Open() rebuilds lists.
- macOS Retina: `Screen.resolutions` in points vs pixels → verify labels on a Retina build.
- macOS notch: some listed sizes resized on apply → confirm prompt shows actual `Screen.width/height`.
- Same-name monitors (two identical panels) → index disambiguates.
- Borderless with non-native aspect → letterbox bars (expected; document in tooltip).
- Editor: `SetResolution` only affects Game view; `GetDisplayLayout` allocates → verify in standalone build.

## Files

- `Assets/Scripts/Services/Graphics/DisplaySettingsApplier.cs`, `IDisplayOptions.cs`
- `Assets/Scripts/Services/Settings/DisplaySettingsData.cs`, `DisplayWindowMode.cs`
- `Assets/Scripts/Entities/MainMenu/Settings/SettingsDraftEditor.cs`, `SettingsModel.cs`, `SettingsUi.cs`, `DisplayConfirmationCountdown.cs`, `SettingsRouteUiController.cs`
- `Assets/Prefabs/Ui/MainMenu/SettingsUi.prefab`
- Tests: `SettingsDataTests`, `SettingsDraftEditorTests`, `SettingsServiceTests`

## Related

- Parent: [[TODOs/Features/Settings_Implementation_Plan|Settings Implementation Plan]] (TODO: monitor selection; refresh rate).

## Decisions

- Both accepted by user 2026-11-04; modes appear according to platform support.
- Unity compile/prefab verification completed after the user's active test run finished; tests were deferred during initial implementation; authorized follow-up results below.
- Penpot design completed through the signed-in browser. Replacement key supplied by user saved to ignored local Penpot configuration; no credential values recorded in notes.

## Implementation Evidence (2026-11-04)

- Phases 1–3 source implemented; `SettingsDisplayEditor` separates display choices from general settings edits.
- Async service startup/application/revert/discard; actual-display snapshot protects defaults-based monitor restoration. Busy UI blocks overlapping operations.
- Regression cases authored for legacy data, rational Hz, platform sanitization, borderless editing, monitor FPS, async confirmation and restoration. Tests passed in the authorized follow-up below.
- Reusable guidance → [[Rules/DISPLAY_SETTINGS]].
- Prefab semantic comparison: 0 existing object IDs deleted; 124 new IDs for cloned rows; 3 existing blocks changed (view bindings, content children, tooltip registrations).
- Isolated 1920×1080 prefab previews verified: `2: DELL U2723QE`, `59.94 Hz`, and 3-line Keep/Revert summary fit. No Play Mode or scene changes required.

## Outcome

- Completed 2026-11-04: all four implementation phases and both optional features. Acceptance pending-user for Windows/macOS hardware checks; automated test gate passed.
- Source checks, Unity compile/import, saved prefab bindings, semantic prefab comparison and isolated layout/prompt previews verified.
- Penpot: [06 · Settings](https://design.penpot.app/#/workspace?team-id=19c47d73-0a5d-8067-8008-ba41b7ce6810&project-id=19c47d73-0a5d-8067-8008-ba41b7ce88ca&file-id=71b39894-c9c5-81cd-8008-ba57254d0595&page-id=0a9544de-1801-8012-8008-ba9aeb21e9f6).
- Reusable implementation rules extracted to [[Rules/DISPLAY_SETTINGS]].

## Verification Follow-up (2026-11-04)

- User explicitly authorized `Ui` + `Core` execution. Both ran asynchronously in EditMode with exact category filters; no full-suite run.
- `Ui`: 245 passed / 245 total; `Core`: 136 passed / 136 total. Zero failures, skipped or inconclusive tests.
- New display regressions and prefab reference checks passed; no code fixes required.
- Reports: `Logs/Tests/20261104-DisplaySettings/Ui.json`, `Logs/Tests/20261104-DisplaySettings/Core.json`.
- Removed the automated-test acceptance gate; Windows/macOS hardware gates remain pending.
