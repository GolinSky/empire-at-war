---
category: Features
status: in-progress
---
# Settings Implementation Plan

## Goal

- PC settings screen (display, graphics, camera, key bindings) with one versioned save file.
- Spec: `RTS_PC_Settings_Logic_Data_UI.md` (external; DI adapted to Zenject).

## Decision

- Settings core knows no consumer. Two communication paths:
  - **Apply** (engine state): `ISettingsApplier.Apply(SettingsData)`; implementations live with their system; must be idempotent.
  - **Observe** (gameplay): narrow read-only interfaces, e.g. `ICameraPreferences`; read on use, no event.
- Binding persistence: Unity override JSON (`SaveBindingOverridesAsJson`) stored in `SettingsData.Input`. Why: enough until chord capture. Avoid: a second typed binding store.
- Settings screen lives only in MainMenu → no live preview; values apply on Apply.

## Implementation

- `SettingsService` (`ISettingsService`, `ICameraPreferences`): Saved / Draft, `IsDirty` = JSON compare, `Apply` / `KeepDisplay` / `RevertDisplay` / `Discard` / `ResetDraftToDefaults`.
- `JsonSettingsRepository`: `persistentDataPath/settings.json`; temp write → `File.Replace` + `.bak`; unreadable file → `.corrupt` copy + backup load.
- First run imports legacy PlayerPrefs `QualityPreset`, `InputBindingOverrides`.
- Applier order = `ProjectContextInstaller` binding order: `DisplaySettingsApplier` → `GraphicsSettingsApplier` → `InputBindingService`.
- Display change → 15 s Keep/Revert prompt (`Time.unscaledTime`); timeout / Escape → revert.
- `InputBindingService`: suspends all enabled maps during capture, Escape cancels, cross-map chord conflicts (`BindingConflicts`), Replace / Swap / Cancel.
- Camera: `CameraService` (pan/zoom multipliers, invert zoom), `CameraInput` (edge scrolling).
- UI: `SettingsRouteController` + `SettingsDraftEditor` + `KeyBindingEditor`; views `SettingsUi`, row views, `SettingsPromptView`.

## Files

- `Assets/Scripts/Services/Settings/*`, `Assets/Scripts/Services/Graphics/*`
- `Assets/Scripts/Services/Input/InputBindingService.cs`, `BindingConflicts.cs`, `BindingSlotCatalog.cs`
- `Assets/Scripts/Entities/MainMenu/Settings/*`, `Assets/Prefabs/Ui/MainMenu/SettingsUi.prefab`

## Progress

- [x] Phase 1 — core data, repository, service
- [x] Phase 2 — input migration, conflicts, Escape-safe capture (closes audit IN2–IN4)
- [x] Phase 3 — quality preset, VSync, FPS cap, window mode, resolution + confirmation
- [x] Phase 4 — camera preferences
- [ ] Phase 5 — audio (blocked: no AudioMixer in project)
- [x] Phase 6 — settings screen UI
- Verified 2026-09-30 in Play Mode: rows render, Apply writes file, unsaved prompt, Discard, conflict detection (Up=S → Move Down; Ctrl+A ≠ A). No automated tests run.

## TODO

- Display crash-recovery marker; monitor selection; refresh rate.
- Texture / shadow / AA / render scale rows (needs owned runtime URP asset copies).
- Required-action protection on Replace; reset-page / profile presets.
- `SettingsRouteController` ≈ 250 lines → consider extracting display countdown.
- Pre-existing: `CloseButtonText` uses `✕`, font renders a box; guideline wants ASCII `X`.
- Verify display change + Keep/Revert in a Windows standalone build.
- Audit follow-ups (tests, per-edit allocations, mutable preferences, I/O failure policy): [[TODOs/Features/Settings_Audit_Followups]].
