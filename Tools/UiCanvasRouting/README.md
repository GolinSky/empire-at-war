# UI canvas routing

- `UiService` selects placement by `UiType`; controllers call `CreateUi(UiType)` and semantic `Activate(bool)`.
- Service root has no Canvas. Every screen view retains its bound `CanvasGroup` and inherits a service Canvas.
- Default `0`: CoreGame, MainMenu, Skirmish, Economy, Reinforcement, Faction, Hero.
- Dynamic `10`: MiniMap, Ship, ShipGroup, ShipBuild, Fps.
- Feedback `-10`: UnitOrderFeedback; no GraphicRaycaster.
- Popup `20`: Contextual (CaptureSiteBuild), EndGame, Modal (PauseMenu/Settings), Tooltip, Fade; sibling slots fix stacking independently of creation order.
- CoreGame chrome draws first; its orbital popup also uses Contextual. HUD hide covers contextual panels while keeping other Popup slots available.
- Faction and Ship content have separate scroll/viewport/mask hierarchies. Faction owns its cards and restores the service scroll binding on disposal.
- `CaptureSitesSystem` retains selection/build authority. `CaptureSiteBuildUiController` owns one reusable screen view; world-site prefabs retain status UI, rings, rocks and construction visuals.

## Verification

```powershell
unity command eval_file --file Tools/UiCanvasRouting/Verify.cs --json
```

- Uses prefab preview scenes; never enters Play Mode or writes production assets.
- Checks saved placement, asset registration, CanvasGroups, popup ordering, HUD visibility, capture-panel clamping at 1280×720 / 1920×1080 / 2560×1440, scroll hierarchies and repeated faction slot attachment/disposal.
- Evidence: `Logs/UiCanvasRouting/verification.json` (git-ignored).
- New battle/performance analysis was explicitly deferred on 2026-10-12. Extra Feedback batching cost and Development Player behavior/performance acceptance remain unmeasured.
