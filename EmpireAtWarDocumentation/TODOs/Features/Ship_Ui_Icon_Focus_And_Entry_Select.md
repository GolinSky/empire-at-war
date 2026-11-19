---
category: Features
status: in-progress
created: 2026-11-19
updated: 2026-11-19
---
# Ship UI — Icon Double-Tap Focus + Group Entry Select

## Goal
- Single ship UI: double-tap the ship icon → camera jumps to the unit when it is off-screen (same pattern as hero icons).
- Multiple ship UI: single tap on any individual unit entry → deselect all, select only that unit (switches to single ship UI).

## Current State (verified 2026-11-19)
- Hero pattern — `HeroIconUi`: `focusButton.onClick` → `IHeroPresenter.SelectHero`; `OnPointerClick` with `clickCount == 2` (left button, interactable) → `FocusHero` → `ICameraService.MoveTo(transform.position)`. No on-screen check.
- Single card — `ShipUi.prefab` (`ShipUi`): `focusButton` (`ShipIcon`) and `disableSelectionButton` (`ShipCard`) are disabled in `ShipUi.Initialize()` (`enabled = false`). `IShipUiPresenter.FocusSelection()` exists but has no caller.
- Group entry — `ShipGroupEntryUi.prefab` (`ShipUi` via `ConfigureEntry`):
  - `ShipCard` (`disableSelectionButton`) → `ShipSelectionGroupUi.HandleClick` → `SelectShipGroup(type)` / `SelectSquadronGroup(type)` → `SelectCurrentUnitsByType`.
  - `ShipIcon` (`focusButton`) → `ShipUiEntry.Focus` → `ShipUiController.FocusEntity` → `MoveTo(IMoveFacade.WorldPosition)` on single tap, no on-screen check.
- Group header — `ShipGroupUi.prefab` root `ShipGroup` (`ShipSelectionGroupUi.button`) → select type.
- Collapsed group (`> MAX_INDIVIDUAL_SHIPS = 4`) → summary card only, no per-unit entries.
- `ISelectionService.SelectEntity(entity)` → resets all selections, selects one (`SelectionService.cs:112`).
- On-screen test exists privately in `SelectionQuery.IsOnScreen`: viewport `z > 0`, `x,y ∈ [0,1]`.

## Decision
- Double-tap detection lives in `ShipUiController` (presenter), keyed by entity id + unscaled time, not `PointerEventData.clickCount`.
  - Why: first tap on a group entry selects one unit → group UI hides, single `ShipUi` shows → second click hits a different object, so Unity's `clickCount` resets to 1.
  - Avoid: per-view `clickCount` for entries.
- Off-screen check: viewport test on `IMoveFacade.WorldPosition` (same bounds as `SelectionQuery.IsOnScreen`). On-screen → no camera move.
- Group header / collapsed summary keep select-by-type behavior.

## Implementation

### Phase 1 — Shared focus logic
1. [x] Add on-screen helper reachable from `ShipUiController` (e.g. `bool IsOnScreen(Vector3 worldPoint)` on `ICameraService`, or a focused helper in `Services/Camera`). Reuse it in `SelectionQuery.IsOnScreen`.
   - If added to `ICameraService` → update test fakes: `FactionAnnouncementTests`, `PlayerOrderInputHandlerTests`, `SelectionQueryTests`, `ReinforcementZonesSystemTests`, `BattleSaveServiceTests`.
2. [x] `ShipUiController.FocusEntityIfOffScreen(IEntity)` → `MoveTo(WorldPosition)` only when off-screen and not destroyed.
3. [x] Double-tap tracker in presenter: `_lastTappedEntityId`, `_lastTapTime`; window const `DOUBLE_TAP_SECONDS` (match `SelectionService.IsRepeatedTap` window if one exists, else 0.3 s).

### Phase 2 — Single ship UI icon
1. [x] `IShipUiPresenter`: replace unused `FocusSelection()` with `TapSelectedShipIcon()` (or rename to `FocusSelectionIfOffScreen`).
2. [x] `ShipUi` (non-entry mode): enable `focusButton`, route tap → presenter; presenter focuses on second tap within window on same entity.
3. [x] Keep `disableSelectionButton` behavior unchanged (still disabled in single mode).
4. [x] Update tooltip `"Focus"` text: `"Double-click to move the camera to the selected ship."`.

### Phase 3 — Group entry single select
1. [x] `IShipUiPresenter.SelectShip(long entityId)` → `_selectionService.SelectEntity(entity)` + record tap (id, time).
2. [x] `ShipSelectionGroupUi.Configure` → entry `onSelected` = `() => SelectShip(ship.Entity.Id)` instead of group `HandleClick`. Header `button` keeps `_onClicked` (type select).
3. [x] Entry `ShipIcon` (`focusButton`) → same `SelectShip` (single tap selects; whole entry = one target). Remove single-tap `ShipUiEntry.Focus` routing or turn it into double-tap focus.
4. [x] Second tap within window on same entity (now on single `ShipUi` icon) → `FocusEntityIfOffScreen`.
5. [x] Entry tooltip (`ShipUiEntry` → `EntityTooltipContent`) status: `"Click to select this ship. Double-click to focus."`. Group type tooltip keeps `"Click to select this unit type."`.
6. [x] Squadron entries: same flow (`SelectEntity` works for squadrons).

### Phase 4 — Tests (review/update; run only on explicit request)
- [x] `Tests/Editor/BattleInput/ShipSelectionReuseTests.cs` (ShipUiEntry ctor updated; constructs `ShipUiController`).
- [ ] `Tests/Editor/Ui/ShipDestroyFlowTests.cs`, `Tests/Editor/Abilities/ShipAbilityServiceTests.cs`, `Tests/Editor/Map/SpaceHazardPresentationTests.cs` (ctor / prefab refs).
- [ ] `Tests/Editor/Ui/ShipAutoPowerUiTests.cs`, `Tests/Editor/Ui/UiCanvasArchitectureTests.cs` (prefab wiring).
- [ ] New: entry tap → `SelectEntity(that entity)`; header tap → type select; double tap within window + off-screen → `MoveTo`; on-screen → no `MoveTo`; tap on other entity / after window → no focus.
- [x] `HeroUiControllerLifecycleTests` — reviewed; null camera, FocusHero exits early, no change needed.

## Edge Cases
- Entity destroyed between taps → no focus, clear tracker.
- Selection changed externally between taps (box select, hotkey) → tracker keyed by id; focus only if the same entity is still the single selection.
- Double-tap on entry → tap 1 selects, tap 2 (on single card icon) focuses if off-screen.
- Collapsed group (> 4 units) → no per-unit entries; tap = type select (unchanged).
- Paused battle → camera stays movable (`CameraService._canMove` true while paused).
- Entry context menu (`ShipCardContextInput` / destroy flow, right click) must keep working.

## Files
- `Assets/Scripts/Entities/ShipUi/ShipUiController.cs` (425 lines — over 200; keep additions small, consider extracting a tap/focus helper)
- `Assets/Scripts/Entities/ShipUi/IShipUiPresenter.cs`
- `Assets/Scripts/Entities/ShipUi/ShipUi.cs`
- `Assets/Scripts/Entities/ShipUi/ShipSelectionGroupUi.cs`
- `Assets/Scripts/Entities/ShipUi/ShipGroupUi.cs`
- `Assets/Scripts/Entities/ShipUi/Model/ShipUiEntry.cs`
- `Assets/Scripts/Services/Camera/CameraService.cs`, `Assets/Scripts/Services/Selection/SelectionQuery.cs`
- Prefabs: `Assets/Prefabs/Ui/Ship/ShipUi.prefab`, `ShipGroupEntryUi.prefab`, `ShipGroupUi.prefab`
- Reference: `Assets/Scripts/Entities/Heroes/HeroIconUi.cs`, `HeroUiController.cs`

## Implementation Status (2026-11-19)
- Code done; not compiled, tests not run. Helper: `CameraViewportExtensions.IsOnScreen` (no `ICameraService` change, no fakes touched). Tracker: `ShipUiTapTracker` (0.3 s).
- Hero off-screen guard added in `HeroUiController.FocusHero`.
- Remaining: compile check, new tests (tap tracker, entry select, focus on/off screen), in-game acceptance.

## Open Questions (resolved)
1. Hero icons: YES, skip move when on screen.
2. On-screen double-tap: do nothing.
3. Collapsed summary (> 4): no per-unit pick.

## Acceptance (user)
- In-game: single-card icon double-tap off-screen → camera jumps; on-screen → no jump.
- In-game: mixed and same-type multi-selection → tap any entry → only that unit selected; header tap → type selected.
