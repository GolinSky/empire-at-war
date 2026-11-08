---
type: engineering-reference
system:
  - settings
  - display
verified: 2026-11-04
verification: "Source review, Unity compilation/import, persisted prefab
  bindings, semantic object comparison and isolated 1920×1080 layout/prompt
  previews verified. Explicitly authorized EditMode tests passed 2026-11-04: Ui
  245/245, Core 136/136. Standalone Windows/macOS acceptance pending."
---
# Display Settings

## Rules

- `DisplaySettingsApplier` owns Unity `Screen` calls; await monitor movement before reading `Screen.resolutions` or applying size/Hz.
- Windows modes: Windowed, Borderless, Exclusive Fullscreen. macOS: Windowed, Borderless, Native Fullscreen (`MaximizedWindow`). Sanitize unsupported saved modes to Borderless.
- Borderless accepts a chosen output size; Unity scales and letterboxes. `0×0` retains desktop resolution. Render scale remains a separate graphics setting.
- Refresh rate uses numerator/denominator; `0/0` = monitor default. Only Exclusive changes physical Hz; other modes show current monitor Hz read-only.
- Persist monitor name + index; index disambiguates repeated names. Missing monitor → current display. Never use `Display.Activate` for window placement.
- `GraphicsSettingsData.MONITOR_FRAME_RATE = -1`; resolve to rounded monitor Hz after display application. `0` = Unlimited. VSync owns pacing when enabled.

## Implementation

- `SettingsService.Initialization` applies saved display before other appliers; settings screen awaits initialization.
- `ApplyAsync`, `PreviewMonitorAsync`, `RevertDisplayAsync`, `DiscardAsync` await display completion. Start the 15 s Keep/Revert countdown only afterward.
- Capture actual mode, size, rational Hz and monitor before preview. Revert restores this snapshot even when saved monitor/size use defaults.
- Monitor selection previews the full draft and opens Keep/Revert; Keep saves all draft changes. UI input is blocked during the async operation.
- `SettingsDisplayEditor` owns display choice construction; `SettingsModel` holds render state; `SettingsUi` forwards input and renders rows; route controller owns confirmation flow.

## Edge Cases

- Exclusive size must come from the current monitor's supported list. Unsupported saved size → nearest supported size; unavailable selected Hz → highest for that size.
- Move to another monitor → rebuild size/Hz options after movement; unsupported size → desktop.
- macOS Retina/notch labels and actual applied size require standalone validation. Prompt displays actual screen dimensions.
- Windows single/dual monitor 60/144 Hz and macOS Retina/notch/external-monitor acceptance remain with [[Done/Features/Display_Modes_And_Monitors_Plan]].
- Editor play mode: `GraphicsSettingsApplier` assigns a DontSave URP clone to `QualitySettings.renderPipeline`. A settings save during play, or play ending with the clone still assigned → `QualitySettings.asset` gets `customRenderPipeline: {fileID: 0}` → next start throws `ArgumentException: The Object you want to instantiate is null` at `GraphicsSettingsApplier.Apply` (seen 2026-11-08 on High Fidelity; drifted `antiAliasing` too).
  - Guards (Editor only): `QualityPipelinePlayModeGuard` captures each level's pipeline on `ExitingEditMode` (`SessionState`) and restores + saves on `EnteredEditMode`, logging a warning; `QualitySettingsPlayModeSaveFilter` drops `ProjectSettings/QualitySettings.asset` from saves while playing.
  - Manual repair: set the level's pipeline to its `URP-*.asset` (e.g. High Fidelity → `URP-HighFidelity.asset`) via `QualitySettings.renderPipeline`, then save.
  - Root cause (2026-11-08, recurred after EditMode runs): `SettingsService` applies asynchronously (`DisplaySettingsApplier.ApplyQueuedAsync` → `ApplyAllAsync`), so `GraphicsSettingsApplier.Apply` could run **after** `Dispose` — e.g. `RealEntityCompositionTests` building a real `ProjectScope`, or play stopping during settings load → clone stays assigned, nothing restores it. Fix: `Apply` returns after `Dispose` (`_isDisposed`).
  - Guard bug fixed: `QualityPipelinePlayModeGuard.Capture` recorded a clone-holding level as empty and `Restore` wrote **none**. Restore now skips levels without a captured asset path.
  - `antiAliasing` on High Fidelity flips 0 → 2 when `URP-HighFidelity` is active (URP syncs MSAA into QualitySettings); not caused by the guard.

## Files

- `Assets/Scripts/Services/Graphics/{DisplaySettingsApplier,IDisplayOptions,GraphicsSettingsApplier}.cs`
- `Assets/Scripts/Services/Settings/{DisplaySettingsData,DisplayWindowMode,GraphicsSettingsData,SettingsService,ISettingsService}.cs`
- `Assets/Scripts/Entities/MainMenu/Settings/{SettingsDisplayEditor,SettingsDraftEditor,SettingsRouteUiController,SettingsUi,DisplayConfirmationCountdown}.cs`
- `Assets/Prefabs/Ui/MainMenu/SettingsUi.prefab`
- `Assets/Scripts/Editor/Rendering/{QualityPipelinePlayModeGuard,QualitySettingsPlayModeSaveFilter}.cs`
