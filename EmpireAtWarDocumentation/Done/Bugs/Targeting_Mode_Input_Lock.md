---
category: Bugs
status: done
created: 2026-11-25
completed: 2026-11-25
acceptance: pending-user
---
# Targeting Mode Input Lock

## Goal
- An armed ship action or ability never leaves the player without a way out by mouse.
- The cursor shows which action is armed.
- Every targeted action marks its target, like the guard shield does.

## Outcome (2026-11-25)
- Implemented all of parts 1–3; tests updated (part 4), not run (local override). Unity compile: 0 errors. Prefab wired through `PrefabUtility.LoadPrefabContents` / `SaveAsPrefabAsset`.
- Open: user in-game acceptance (part 5) → [[TODOs/User_Acceptance_Queue#Targeting Mode Input Lock]].
- Reusable guidance → [[GameDesign/Commands_And_Controls#Waypoints and targeting]] (exit rule, cursor, markers).

## Original state (source, 2026-11-25; not reproduced in-game)
- `PointerGestures.Tick`: any pending mode → `CancelDrag()` + early return; left-click exit only for ability targeting and `AttackMove`.
- `PlayerOrderInputHandler.HandleCommand`: `Attack` on a non-enemy / `Guard` on a non-friendly → `return`, mode stayed armed with no feedback.
- `OrderCursor` had only `None`, `Attack`, `Invalid`; ability, orbital and attack-move targets had no marker.

## Decision
- Chosen: one rule for every mode. World left-click cancels. Right-click: valid target → issue; ineligible unit of the right side → stay armed + "Invalid target"; anything else → cancel.
- Why: matches ability targeting; no second rule set.
- Avoid: left-click that cancels and selects in one press; per-mode exceptions.
- `WaypointMove` left-click: default taken — cancel and discard placed points (finish with the button or Alt release).
- `Invalid` cursor only where a click keeps the mode armed (ability on ineligible enemy, Guard on unselectable friendly, blocked attack-move point). Orbital: no `Invalid` cursor, because `ISuperWeaponFireService` is absent when the SuperWeapons module is off; the click still reports "Invalid target".

## Implementation

### 1. Exits
1. [x] `PointerGestures.Tick`: world left-click cancels ability, orbital and any `UnitActionTargetingModel` mode; HUD press does not.
2. [x] `PlayerOrderInputHandler.HandleCommand`: `Attack` + non-enemy → `_targeting.Cancel()`; `Guard` + non-friendly or non-selectable → `_targeting.Cancel()`. Destroyed/uninstalled hardpoint → unchanged (stays armed).
3. [x] Orbital: left-click cancel from step 1; right-click rules unchanged.
4. [x] Alt `QueueWaypoint` on focus loss: `InputSystem.inputsettings.asset` `m_BackgroundBehavior: 0` (ResetAndDisableNonBackgroundDevices) → keyboard reset fires `canceled` → `FinishWaypoints`. Left-click is now a second exit.

### 2. Action cursor
1. [x] `OrderCursor`: + `Guard`, `AttackMove`, `Waypoint`, `AbilityTarget`, `SuperWeapon` (also used as marker kind).
2. [x] `OrderCursorService.Resolve()`: reinforcement preview first; armed + over HUD → `None`; orbital → `SuperWeapon`; ability → `AbilityTarget` / `Invalid`; `AttackMove` → `AttackMove` / `Invalid`; `Attack` → `Attack`; `Guard` → `Guard` / `Invalid`; `WaypointMove` → `Waypoint`; idle → `Attack` over an enemy with an attacker.
3. [x] `UnitOrderFeedbackUi.SetCursor(cursor, pos, actionIcon)`: `ActionCursor` anchor + `ActionCursorIcon` image (28×28, offset (22, −22), `raycastTarget` off). `attackCursorAnchor` / `invalidTargetAnchor` kept.
4. [x] Sprites: `guardIcon`, `attackMoveIcon` (`UnitActions/AttackMove.png`), `waypointIcon` (`UnitActions/WaypointMove.png`), `superWeaponIcons[]` ordered by `SuperWeaponType` (`SuperWeapons/*Icon.png`); ability → `ShipAbilityCatalog.Get(PendingAbilityId).Icon`.
5. [x] Prefab `Assets/Prefabs/Ui/UnitOrderFeedback/UnitOrderFeedbackUi.prefab` updated; `UI_UX_GUIDELINES` read.

### 3. Target markers
1. [x] `PlayTargetOrder(pos, OrderCursor action, Sprite actionIcon, completed)`: `Attack` → reticle; others → icon at `TARGET_ICON_SCALE` 0.45. Colour: Guard/Waypoint → formation blue; ability/orbital → white (own icon colours); Attack/AttackMove → attack colour.
2. [x] `IShipAbilityTargeting.TargetAccepted(ShipAbilityId, IEntity)` + `PendingAbilityId`; fired by `ShipAbilityService.SubmitTarget` after an accepted pick only.
3. [x] `SuperWeaponTargetingModel.Accept(target)` → cancels, then `TargetAccepted(type, target)`; `SuperWeaponPresenter.HandleTargetSubmitted` calls it after validation.
4. [x] `AttackMove`: move ripple + attack-move icon at the point (`_feedbackPoint` tracking in the controller).
5. [x] `Stop`, `Hunt`, `Retreat`: no marker.

### 4. Tests (`BattleInput`, `Ui`, `Abilities`; not run — local override)
1. [x] `PointerGesturesTests`: left-press cancel + HUD-press cases for all 6 modes.
2. [x] `PlayerOrderInputHandlerTests`: `PendingAttack_WrongTargetCancelsWithoutOrder`, `PendingGuard_WrongTargetCancelsWithoutOrder` (enemy, unselectable friend), `PendingGuard_SelectableFriendIssuesGuard`; `CaptureSiteCommand_DoesNotReplacePendingAttack` world case now expects cancel.
3. [x] Cursor per `OrderCursor`: `UnitOrderFeedbackUiTests.SetCursor_ShowsOneCursorPerOrder`. `OrderCursorService` has no fixture; resolution logic not unit-tested.
4. [x] `UnitOrderFeedbackUiTests`: guard/attack test migrated; ability/orbital/attack-move marker icon; prefab wiring of every icon.
5. [x] `ShipAbilityServiceTests.Targeting_RejectsOutOfRangeAndActivatesInRange`: `TargetAccepted` only for the accepted pick. `SuperWeaponTests.TargetingModel_AcceptEndsTargetingThenReportsTheWeapon`.
- Fakes updated: `IShipAbilityTargeting` stubs in `PointerGesturesTests`, `PlayerOrderInputHandlerTests`, `UnitActionsPresenterTests`.

### 5. User acceptance (queued)
- [ ] Arm each mode, leave it by left-click and by a wrong right-click.
- [ ] Cursor icon matches the armed action; invalid cursor over rejected targets.
- [ ] Marker appears on ability, orbital and attack-move targets.

## Edge Cases
- Left-click on HUD while armed → no cancel (button toggles keep working).
- Ability armed + ineligible enemy → stays armed, "Invalid target" (unchanged).
- Selected units die while armed → `UnitActionsPresenter.UpdateState` / `PruneTargeting` cancel (existing).
- Marker target destroyed or removed → marker stops (existing `HandleEntityRemoved`).
- Ability with no icon → marker falls back to the attack reticle; cursor shows no action icon.
- Online client: cursor and markers are local UI; no new network messages.

## Files
- `Assets/Scripts/Services/Input/PointerGestures.cs`
- `Assets/Scripts/Services/UnitOrders/PlayerOrderInputHandler.cs`, `OrderCursor.cs`, `OrderCursorService.cs`
- `Assets/Scripts/Entities/UnitOrderFeedback/UnitOrderFeedbackUi.cs`, `UnitOrderFeedbackUiController.cs`
- `Assets/Scripts/Services/ShipAbilities/ShipAbilityService.cs`, `IShipAbilityTargeting.cs`
- `Assets/Scripts/Entities/SuperWeapon/Model/SuperWeaponTargetingModel.cs`, `Controller/SuperWeaponPresenter.cs`
- `Assets/Prefabs/Ui/UnitOrderFeedback/UnitOrderFeedbackUi.prefab`
