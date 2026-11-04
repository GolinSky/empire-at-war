# UI canvas routing

- `UiService` selects placement by `UiType`; controllers call `CreateUi(UiType)` and semantic `Activate(bool)`.
- Service root has no Canvas. Every screen view retains its bound `CanvasGroup` and inherits a service Canvas.
- Default `0`: CoreGame, MainMenu, Skirmish, Economy, Reinforcement, Faction, Hero.
- Dynamic `10`: MiniMap, Ship, ShipGroup, ShipBuild, Fps.
- Feedback `-10`: UnitOrderFeedback; no GraphicRaycaster.
- Popup `20`: Contextual (CaptureSiteBuild), EndGame, Modal (PauseMenu/Settings), Tooltip, Fade; sibling slots fix stacking independently of creation order.
- CoreGame chrome draws first; its orbital popup also uses Contextual. HUD hide covers contextual panels while keeping other Popup slots available.
- Screen canvases use `ScaleWithScreenSize`, reference `1920×1080`, and `Expand`, including Loading. Fixed menu controls and HUD slots require at least that logical space; width/height matching at `0.5` clips them on 16:10 and ultrawide displays.
- Tooltips fit oversized content within the viewport before clamping their position; shorter content restores its original scale.
- Ship-group entries bottom-anchor their health/shield bars and status text; the icon stretches above them. Both 67-unit two-row cells and 136-unit single-row cells keep these elements separated and inside the card.
- Faction and Ship content have separate scroll/viewport/mask hierarchies. Faction owns its cards and restores the service scroll binding on disposal.
- Station cards retain their authored 138×106 px size; two rows plus 6 px spacing occupy 218 px of the 220 px viewport.
- `CaptureSitesSystem` retains selection/build authority. `CaptureSiteBuildUiController` owns one reusable screen view; world-site prefabs retain status UI, rings, rocks and construction visuals.

## Verification

```powershell
unity command eval_file --file Tools/UiCanvasRouting/Verify.cs --json
unity command eval_file --file Tools/UiCanvasRouting/VerifyFactionLayout.cs --json
```

- Uses prefab preview scenes; never enters Play Mode or writes production assets.
- Checks saved placement, asset registration, CanvasGroups, popup ordering, HUD visibility, capture-panel clamping at 1280×720 / 1920×1080 / 2560×1440, scroll hierarchies and repeated faction slot attachment/disposal.
- Evidence: `Logs/UiCanvasRouting/verification.json` (git-ignored).
- New battle/performance analysis was explicitly deferred on 2026-10-12. Extra Feedback batching cost and Development Player behavior/performance acceptance remain unmeasured.

## Resolution audit — 2026-11-04

- Inventoried 130 UI-folder prefabs (including 86 without inline RectTransforms), all five serialized screen canvases, three world canvases, and runtime health/hardpoint/squadron overlays.
- Preview measurements: 27 routed UI prefabs at 1024×768, 1280×720, 1366×768, 1440×900, 1512×982, 1920×1080, 2560×1600, 3440×1440, and 5120×1440. Enabled graphics outside scroll areas stayed within the screen bounds with `Expand`.
- Evidence: `Logs/UiAdaptivity/measurements.json` (git-ignored). Measurements use isolated prefab previews and logical canvas sizes; they do not prove native Mac/Windows rendering, open dropdown placement, or every runtime data state.
- No tests were started for this task under the local execution override. A concurrent suite was already running; early automation timeouts contaminated several results before that was identified. Those results do not validate this patch. Concurrent display-settings edits also produced compilation errors during the audit.
- Final checks: assets saved/imported, ship-card bounds measured at both row heights, `scriptCompilationFailed = false`, no new console errors after the final save, and `git diff --check` clean.
