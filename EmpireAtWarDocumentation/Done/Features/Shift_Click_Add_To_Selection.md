---
category: Features
status: done
created: 2026-11-19
updated: 2026-11-19
completed: 2026-11-19
acceptance: pending-user
---
# Shift + Click — Add Unit To Selection

## Goal
- Hold Shift + left-click a local unit in the battle view → unit joins the current selection (multi-selection).
- Plain left-click keeps today's behavior: replace selection.

## Current State (verified 2026-11-19)
- `SelectionService.HandleWorldClicked` (`SelectionService.cs:177`) → `ResetAllSelections()` first, then resolves the hit, then `SetSelection(scope, [hit])`. Every click replaces.
- Hit resolve: `_pressedMarker` (marker captured on press) → else `ISelectionQuery.CollectAt` + `GetPointSelectionIndex` (slow repeated clicks cycle overlaps; `ClickCount >= 2` → same-type select).
- `SelectionContext` has `Replace(entries)`, `Remove(entity)`, `Contains(entity)`, `ResetCurrentSelectable()`. No `Add`.
- `SelectionContext.Entities` exposes `IEntity` only → service cannot rebuild `SelectionEntry` list from it.
- Two contexts: `Local` (own units) and `Other` (inspect enemy / ally). `Accepts(entity)` gates by owner.
- Death tracking: `SetSelection` subscribes `HealthModel.OnDestroy` per entity into `_deathSubscriptions`.
- `ISelectionInput` (`SelectionInput`) → keyboard chords only (Ctrl+A etc.). No modifier-held query.
- Rebindable held-key precedent: `Battle/QueueWaypoint` (Button, `<Keyboard>/alt`) → `UnitOrderInput.IsQueueWaypointHeld => _battle.QueueWaypoint.IsPressed()`.
- `BindingSlotCatalog.Build(Camera, Battle)` lists every `Button` action of the `Battle` map as a rebind slot → a new action appears in Settings automatically, label from the action name.
- `EmpireAtWar.inputactions.meta`: `generateWrapperCode: 1` → `Assets/Scripts/Services/Input/GameInputActions.cs` regenerates on import.
- Hardcoded Shift (not rebindable, left as is): `ReinforcementDeployInput.IsDeployAllHeld`.
- Shift conflicts: none on left-click. `Ctrl+Shift+A/Q/W` are key chords. `ControlGroupInput` only checks modifiers with digit keys. Shift+drag is reinforcement-panel UI, not world.
- `CaptureSitesSystem.HandleWorldClicked` also listens to `WorldClicked` → clears site selection on any click. Unaffected.

## Decision
- Rebindable key: new `Battle/AddToSelection` Button action, default `<Keyboard>/shift`; read through `bool IsAddToSelectionHeld` on `ISelectionInput` (`_battle.AddToSelection.IsPressed()`).
  - Why: same shape as `QueueWaypoint`; Settings slot, conflict check, save/restore of overrides and tooltip text come for free.
  - Why: `SelectionService` already injects `ISelectionInput`; test fake already implements it.
  - Avoid: `Keyboard.current.shiftKey` (not rebindable); a `OneModifier` composite with the mouse button (left click is not a rebindable action).
- Mutation: add `SelectionContext.Add(SelectionEntry)`; service adds one entry, subscribes death, notifies.
  - Why: no need to rebuild the whole list; `Replace` would need stored entries the service does not have.
  - Avoid: `SelectEntities(current + new)` → resets `Other` handling and re-walks facades.
- Shift + click on an already selected unit → remove it (toggle). Confirmed 2026-11-19.
- Additive hit pick: first local unit in the hit list; no overlap cycling, no double-click type select while Shift is held.

## Behavior
- "Shift" below = the `Add To Selection` key (default Shift, rebindable).

| Shift + click on | Result |
|---|---|
| Local unit, not selected | Added to `Local` selection; `Other` selection cleared |
| Local unit, selected | Removed from selection (toggle) |
| Empty space | Selection kept |
| Enemy / ally / neutral entity | Selection kept; no inspect |
| Local structure (station, mine, satellite) | Selection kept (units only) |
| Local unit, current selection is a structure | Plain select (replace) |
| Nothing selected | Plain select |

## Implementation

### Phase 1 — Input
1. [x] `Assets/Settings/Input/EmpireAtWar.inputactions`, `Battle` map: add action `AddToSelection` (`type: Button`, `expectedControlType: Button`, `initialStateCheck: false`, new GUID) next to `QueueWaypoint`; add binding `<Keyboard>/shift`, group `Keyboard&Mouse`, new GUID.
2. [x] Import the asset (`AssetDatabase.Refresh`) → `GameInputActions.cs` regenerates with `Battle.AddToSelection`; check the console for import errors. Do not hand-edit the wrapper.
3. [x] `ISelectionInput`: add `bool IsAddToSelectionHeld { get; }`.
4. [x] `SelectionInput`: `public bool IsAddToSelectionHeld => _battle.AddToSelection.IsPressed();`.
5. [x] `SelectionInputTests.FakeInputService` (`SelectionInputTests.cs:562`): add settable `IsAddToSelectionHeld`.
6. [ ] (user gate) Settings → key bindings: confirm the slot shows as `Add To Selection` with `Shift`, rebinds, resets, and persists in `SettingsData.Input.BindingOverridesJson`. No `KeyBindingEditor` code change expected.

### Phase 2 — Selection
1. [x] `SelectionContext.Add(SelectionEntry) → bool`: reject when `!Accepts` or already contained; `Command.Select(true)`; append to `_entries` / `_entities` / `_selectedIds`; `UpdateSelectionType()`.
2. [x] `SelectionService.HandleWorldClicked`: resolve the hit before `ResetAllSelections()`; when `IsAddToSelectionHeld` and `Local` selection holds only units → additive branch, else existing path unchanged.
3. [x] Additive branch (new private method, e.g. `ToggleInSelection(SelectionEntry)`):
   - Pressed marker valid → use it; else first entry in `_pointBuffer` with local owner and `IsUnit()`.
   - No such entry → return, selection untouched.
   - `_lastTappedEntityId = null` (no cycling / double-click carry-over).
   - Contained → `UnsubscribeDeath` + `context.Remove` + `NotifyObservers(Local)`.
   - Not contained → clear `Other` if set; `context.Add`; add to `_deathSubscriptions` + subscribe `OnDestroy`; `NotifyObservers(Local)`.
4. [x] Keep `IsPruningSelection = false` on both paths (player action, not a death prune).
5. [x] `SelectionService.cs` is 418 lines (> 200). Keep the addition to one method; no refactor in this scope.

### Phase 3 — Observers (review, change only if broken)
- [x] `ShipUiController`: 1 → 2 units switches single card → group UI; 2 → 1 switches back.
- [x] `SelectionSfx`: select cue on add; confirm no cue spam / wrong cue on toggle-off.
- [x] `UnitActions` panel, `SelectionHintCondition`, selection markers: re-read context on notify.
- [x] `ControlGroupService.CaptureSelection`: reads context → works with shift-built selections.

### Phase 4 — Docs and hints
1. [x] `GameDesign/Commands_And_Controls`: add rule under selection + shortcut table row `Shift + left-click | Add / remove unit in selection` (rebindable: `Add To Selection`).
2. [ ] Optional, not requested → skipped: tutorial hint for Shift + click; key text from `TooltipBindings.Get(_bindings, "Battle", "AddToSelection")` so it follows a rebind. Only if requested.

### Phase 5 — Tests (`BattleInput`; write, run only on explicit request)
- [x] Shift + click unselected local unit → both units in `PlayerSelectionContext`, order preserved.
- [x] Shift + click selected unit → removed; `Select(false)` called once.
- [x] Shift + click empty space / enemy / local structure → selection unchanged, no notification.
- [x] Shift + click with `Other` selection active → `Other` cleared, unit added to `Local`.
- [x] Shift + click with empty selection → unit selected.
- [x] Added unit destroyed → pruned from selection (death subscription wired).
- [x] Removed unit destroyed → no notification (subscription released).
- [x] Plain click after shift-built selection → replaces with one unit.
- [x] `SelectionContext.Add`: duplicate rejected; mixed ship + squadron → `SelectionType.None`.
- [x] `CameraInputBindingsTests`: add `AssertBindings(Battle/AddToSelection, "<Keyboard>/shift")` beside the `QueueWaypoint` assert (`CameraInputBindingsTests.cs:97`).
- [x] `SelectionInput` with real `InputActionsProvider` + test keyboard: `LeftShift` down → `IsAddToSelectionHeld` true; released → false; after `ApplyBindingOverride` to another key → follows the new key, Shift no longer counts.
- [x] `InputBindingServiceTests`: review slot lookups by label and conflict cases (`Shift+B`, `Ctrl+Shift+A`); a lone `Shift` slot must not conflict with chords that contain Shift (`BindingConflicts` compares whole chords).
- Review, no change expected: `SelectionQueryTests`, `SelectionMarkerTests`, `ShipSelectionReuseTests`, `UnitActionsPresenterTests`, `PlayerOrderInputHandlerTests` (own `ISelectionService` fakes; interface unchanged).

## Edge Cases
- Shift held, drag starts → `_pressedMarker` reset → marquee runs as today (replace).
- Shift + double-click same unit → add, then remove (toggle twice).
- Overlapping units under cursor → first local unit wins; no cycling while Shift is held.
- Unit hidden by fog / destroyed → already filtered by `SelectionQuery`.
- No keyboard / slot unbound → `IsPressed()` false → never additive.
- `Battle` map disabled (rebind capture, menus) → `IsPressed()` false.
- Rebind capture of a single-key slot stores the pressed control (`leftShift`, not `shift`) → right Shift stops working after a rebind to Shift; Reset restores `<Keyboard>/shift`. Same as `QueueWaypoint`.
- Rebound to a key used by another slot (e.g. `A` = camera left) → existing conflict dialog (Replace / Swap / Cancel).
- Rebound to Ctrl → `Ctrl+A` still selects visible units; click with Ctrl held is additive. No special handling.
- Reinforcement panel Shift + drag stays hardcoded Shift; independent of this binding.
- Toggle removes the last unit → `Local` selection empty; observers get a normal notification.
- Click on UI → `WorldClicked` not raised → no change.

## Files
- `Assets/Settings/Input/EmpireAtWar.inputactions`
- `Assets/Scripts/Services/Input/GameInputActions.cs` (generated)
- `Assets/Scripts/Tests/Editor/BattleInput/CameraInputBindingsTests.cs`
- `Assets/Scripts/Services/Selection/ISelectionInput.cs`
- `Assets/Scripts/Services/Selection/SelectionInput.cs`
- `Assets/Scripts/Services/Selection/SelectionContext.cs`
- `Assets/Scripts/Services/Selection/SelectionService.cs`
- `Assets/Scripts/Tests/Editor/BattleInput/SelectionInputTests.cs`
- `EmpireAtWarDocumentation/GameDesign/Commands_And_Controls.md`
- Reference: `Assets/Scripts/Services/UnitOrders/UnitOrderInput.cs` (`QueueWaypoint` held key), `Assets/Scripts/Services/Input/BindingSlotCatalog.cs`

## Open Questions (resolved 2026-11-19)
1. Shift + click on an already selected unit → remove it.
2. Shift + double-click → no type select; toggles twice.
3. Shift + box drag → out of scope; box replaces the selection.
4. Shift + click on multi-ship UI list entries → out of scope.
5. Key is rebindable (`Battle/AddToSelection`).

## Outcome (2026-11-19)
- `Battle/AddToSelection` (`<Keyboard>/shift`) added; `GameInputActions.cs` regenerated by import.
- `ISelectionInput.IsAddToSelectionHeld`; `SelectionContext.Add`; `SelectionService.ToggleInSelection` + `IsUnitOnlySelection` (additive path only when `Local` holds units only).
- Tests added (`BattleInput`): `SelectionInputTests` — `AddToSelectionClick_*` (toggle, death prune, non-unit targets, no unit selection), `SelectionContextAdd_RejectsDuplicatesAndOtherScope`, `AddToSelectionHeld_FollowsBattleAction`; `CameraInputBindingsTests` binding assert.
- Reviewed, no change: `InputBindingServiceTests` (lone `Shift` chord ≠ `Shift+B` / `Ctrl+Shift+A`), other `ISelectionService` fakes, `ShipUiController`, `ControlGroupService`.
- `SelectionSfx`: toggle-off of one unit from a group changes the id set → plays the select cue for the remaining units. Left as is; check in-game.
- Verification: Unity import + compile clean (`scriptCompilationFailed = false`; new test methods present in `Assembly-CSharp-Editor`). `BattleInput` EditMode 433/433 passed on 2026-11-19 (user-requested run), including all 10 new cases.
- Reusable guidance → `GameDesign/Commands_And_Controls` (selection rule + shortcut row).

## Acceptance (user)
- In-game: select ship A, Shift + click ship B → both selected, group UI shown, one move order moves both.
- In-game: Shift + click B again → only A selected.
- In-game: Settings → rebind `Add To Selection` to another key → that key + click adds; Shift + click replaces; Reset restores Shift.
- In-game: Shift + click empty space / enemy → selection unchanged.
- In-game: plain click still replaces the selection.
