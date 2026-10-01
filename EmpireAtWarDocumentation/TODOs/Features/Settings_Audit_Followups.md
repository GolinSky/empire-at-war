---
category: Features
status: in-progress
created: 2026-09-30
tags:
  - code-audit
  - settings
---
# Settings Audit Follow-ups

[[TODOs/Refactoring/Audit_Remediation_Coordination|← Coordination]] · Wave 1 · Unity lane: no · Parent: [[TODOs/Features/Settings_Implementation_Plan]]

## Goal
- Close the gaps found in the settings system (`5ae00d3b`) without changing its design (draft/saved, appliers, preferences).
- Input rebinding code (`Services/Input/*`) is out of scope.

## Findings

| ID | Pri | Conf | Issue | Location |
|---|---|---|---|---|
| ST1 | P2 | Confirmed | No automated tests for the settings core: `SettingsData` (sanitize, clone, unknown fields), `JsonSettingsRepository` (atomic save, `.bak`/`.corrupt` fallback), `SettingsService` (Apply/Keep/Revert/Discard/IsDirty), `SettingsDraftEditor`, `SettingsRouteController` prompts | `Tests/Editor/` (only `CameraInputBindingsTests`) |
| ST2 | P2 | Confirmed | The repository path is hardcoded (`Application.persistentDataPath`), so it can't be tested without touching the real save file | `JsonSettingsRepository.cs:18` |
| ST3 | P3 | Confirmed | Every slider tick → `Refresh()` → `IsDirty` serializes the whole draft to pretty-printed JSON, and `Resolutions` rebuilds and sorts `Screen.resolutions`. Allocation per drag frame. | `SettingsDraftEditor.Refresh`, `SettingsService.IsDirty:20`, `DisplaySettingsApplier.Resolutions` |
| ST4 | P3 | Confirmed | `ICameraPreferences.Camera` returns the mutable saved `CameraSettingsData` (public setters) → gameplay could write saved settings | `ICameraPreferences.cs`, `SettingsService.Camera` |
| ST5 | P3 | Likely | `TryRead` catches only `ArgumentException`. An `IOException` (file locked by antivirus or cloud sync) during `Initialize` throws out of ProjectContext → the game fails to boot. `Save` failures surface in the UI after the engine state was already applied. | `JsonSettingsRepository.TryRead/Save`, `SettingsService.Commit` |
| ST6 | P3 | Confirmed | `SettingsUi.Initialize` throws when `_model`/`_navigation` are unset. AGENTS says to verify UI setup in tests, not with production null guards. | `SettingsUi.cs:56-59` |
| ST7 | P3 | Confirmed | `SettingsService.Initialize` sets `Application.backgroundLoadingPriority`, which is unrelated to settings (carried over from the old service). `SettingsRouteController` is 251 lines (the parent plan already notes extracting the display countdown). | `SettingsService.cs:35`, `SettingsRouteController.cs` |

## Files (owned)
- `Assets/Scripts/Services/Settings/*`, `Assets/Scripts/Services/Graphics/*`, `Assets/Scripts/Entities/MainMenu/Settings/*`
- New tests under `Assets/Scripts/Tests/Editor/`
- `ProjectContextInstaller.cs`: only the settings binding lines, if ST2 needs a path argument.

## Steps
1. [x] ST2: `JsonSettingsRepository(string directory)`; `ProjectContextInstaller` binds `.WithArguments(Application.persistentDataPath)`.
2. [x] ST1: tests written, **not run** (run only on request): `SettingsDataTests`, `JsonSettingsRepositoryTests` (temp dir, corrupt → `.bak` + `.corrupt`, locked file), `SettingsServiceTests` (fake repo + recording applier), `SettingsDraftEditorTests` (fake options), `DisplayConfirmationCountdownTests`, `SettingsUiPrefabTests`.
   - Route-controller prompt/Escape transitions not unit-tested: needs a fake `ISettingsUi`/`BaseUi`; countdown covered via `DisplayConfirmationCountdown` instead.
3. [x] ST3: `IsDirty` = `SettingsData.Matches` (field compare per section, no JSON). `SettingsDraftEditor.Open()` caches resolutions + labels per opening; frame-rate labels static; `GraphicsSettingsApplier` caches `QualitySettings.names`.
4. [x] ST4: `ICameraPreferences` exposes getters only (`PanSpeedMultiplier`, `ZoomSpeedMultiplier`, `EdgeScrolling`, `InvertZoom`), read from saved settings.
5. [x] ST5: `ISettingsRepository.Load` → `SettingsLoadStatus` (`Loaded` / `Missing` / `Unreadable`). Corrupt or locked (`IOException`, `UnauthorizedAccessException`) → backup, else defaults without overwriting. Save failure → `SettingsApplyResult.SaveFailed`, status message, draft stays dirty.
6. [x] ST6: guard removed from `SettingsUi.Initialize`; `SettingsUiPrefabTests` asserts references, volume slider ranges, unique + registered tooltip keys.
7. [x] ST7: `backgroundLoadingPriority` → `SceneService.Initialize`; countdown → `DisplayConfirmationCountdown`.
- Also fixed: audio rows had cloned tooltip keys (`Pan speed`, `Edge scrolling`) and were not registered in `TooltipHoverView.triggers`; now own keys + tooltip texts.
- Verified 2026-09-30: compile clean; prefab references assigned (editor eval); Play Mode open → edit → Discard, audio tooltip hover, no console errors.

## Verification
- Compile clean. Manual Play Mode: change each row → Apply → restart → values persist; display change → Revert/Keep/timeout; corrupt `settings.json` → loads the `.bak`.
