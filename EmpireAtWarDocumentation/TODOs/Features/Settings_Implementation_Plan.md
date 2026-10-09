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
- No live preview except audio volumes; other values apply on Apply.

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
- [x] Phase 5 — audio: `AudioSettingsApplier` (`Services/Audio`) sets `MasterVolume` / `MusicVolume` / `VoiceVolume` / `SfxVolume` on `Assets/Audio/AudioMixer.mixer`
  - dB = authored level + `20·log10(volume)`; volume 0 → −80 dB; 1 keeps authored mix (Master authored −10 dB).
  - Mixer injected via `ProjectContextInstaller.audioMixer`; `SfxDuckVolume` stays owned by `ShipSfxService`.
  - Sliders preview live through `IAudioSettingsPreview`; Discard re-applies saved volumes.
  - Mute when unfocused (default On) → master −80 dB via `Application.focusChanged`; stored volume untouched.
  - Verified 2026-09-30 Play Mode: 50% master → −16.02 dB, 25% music → −12.04 dB, Discard restores, unfocused → −80 dB.
- [x] Phase 6 — settings screen UI
  - 2026-10-02: fullscreen `SettingsUi.prefab`; Audio / Display / Controls / Camera tabs use `ToggleGroup` + `SettingsCategoryTab` and separate `CanvasGroup` panels. Hidden panels have alpha 0, interaction and raycasts disabled.
  - Display groups screen + graphics; Controls retains the inactive rebinding-row template and has its own scroll area. Apply / Discard / Reset All Settings stay outside category panels.
  - Penpot: [06 · Settings — four clickable category screens](https://design.penpot.app/#/workspace?team-id=19c47d73-0a5d-8067-8008-ba41b7ce6810&project-id=19c47d73-0a5d-8067-8008-ba41b7ce88ca&file-id=71b39894-c9c5-81cd-8008-ba57254d0595&page-id=0a9544de-1801-8012-8008-ba9aeb21e9f6).
  - Penpot styling implemented: main-menu starfield, Rajdhani typography, beveled `HudFrame` panels, selected-tab accents, grouped rows, sliders, checkboxes, dropdowns, footer actions and confirmation card. Empty status shows “Changes apply across all categories.”; unsaved status uses amber.
  - Verified: prefab imported and saved; both changed scripts have no diagnostics; settings, tab, rebinding-template and prompt Inspector references assigned. Isolated 1920×1080 previews inspected for all four categories; Controls renders 21 live-asset bindings with 1294 px content in a 438 px viewport. Audio/Display/Camera content fits its viewport.
  - No automated tests, Play Mode session or live rebind capture run for this layout change. Live category/rebind acceptance remains pending.
  - 2026-10-09: six Settings sliders → `Assets/Prefabs/Ui/Controls/MenuSlider.prefab` (470 × 28 px); Skirmish money slider → `SkirmishSlider.prefab` variant (448 × 28 px). Existing visuals and bindings preserved; shared transparent hit area covers the full 28 px height.
  - Rounding: audio 1%; camera 0.05×; starting credits $100. Bar clicks update the rounded value and handle; 98% audio → 98%, 98% of the money range → $9,800.
  - Verification: `SliderInteractionTests` 11/11, `SettingsUiPrefabTests` 4/4, `SkirmishUiPrefabTests` 2/2, `SkirmishModelTests` 4/4. Compilation clean; isolated slider render checked. No manual Play Mode acceptance for this fix.
- Verified 2026-09-30 in Play Mode: rows render, Apply writes file, unsaved prompt, Discard, conflict detection (Up=S → Move Down; Ctrl+A ≠ A). No automated tests run.

## TODO

- Display crash-recovery marker; monitor selection; refresh rate.
- Texture / shadow / AA / render scale rows (needs owned runtime URP asset copies).
- Required-action protection on Replace; reset-page / profile presets.
- `SettingsRouteController` ≈ 250 lines → consider extracting display countdown.
- Verify display change + Keep/Revert in a Windows standalone build.
- Audit follow-ups (tests, per-edit allocations, mutable preferences, I/O failure policy): [[TODOs/Features/Settings_Audit_Followups]].
