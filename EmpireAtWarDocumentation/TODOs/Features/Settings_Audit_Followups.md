---
category: Features
status: todo
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
1. [ ] ST2: the `JsonSettingsRepository` constructor takes the directory path; the installer passes `Application.persistentDataPath`.
2. [ ] ST1: tests for SettingsData, repository (temp dir), service (fake repository + recording applier), draft editor (fake options), route controller prompt transitions (Escape per prompt kind, 15 s revert via injected time or `Tick`). Run only on request.
3. [ ] ST3: compute `IsDirty` from a cheap comparison, or cache it and invalidate it on draft edit. Cache resolutions per `Refresh(open)`, not per edit.
4. [ ] ST4: expose a read-only view (e.g. `ICameraSettings` getters), or return a clone.
5. [ ] ST5: decide the policy. Unreadable/locked file on load → log and fall back to defaults without overwriting. Save failure → show a status message and keep the draft dirty.
6. [ ] ST6: remove the guard; add a prefab test asserting SettingsUi references (`SkirmishUiPrefabTests`-style).
7. [ ] ST7: move `backgroundLoadingPriority` to the bootstrap/scene loading owner; extract the display countdown if the controller is touched anyway.

## Verification
- Compile clean. Manual Play Mode: change each row → Apply → restart → values persist; display change → Revert/Keep/timeout; corrupt `settings.json` → loads the `.bak`.
