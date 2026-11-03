---
category: Features
status: done
completed: 2026-11-03
acceptance: pending-user
created: 2026-11-03
tags:
  - reinforcement
  - ui
  - input
---
# Reinforcement Drag Fix + Shift-Drag Batch Spawn

## Goal
- Second drag from the reinforcement panel after a deploy must always start a placement (or visibly explain why not).
- Shift + drag a reserve button → deploy every available unit of that type at the drop point, limited by population.

## Root Cause (bug) — source analysis 2026-11-03
- `ReinforcementService.TrySpawnReinforcement` rejects when `Time.time < _nextSpawnTime`; `SPAWN_COOLDOWN = 1f` set after each deploy (commit `e6f8468da`, 2026-10-31 — matches regression timing).
- Rejection path is silent: `ReinforcementUi.Handle` → `_presenter.Hide()` → service rejects → `InvokeSpawnShipEvent(false)` → `HandleSpawning` → `_presenter.Show()` in the same frame → no preview, no sound, no message → "nothing happens".
- `Time.time` is scaled: slow game speed stretches the lock; if pause ever sets `timeScale = 0` the lock never expires while paused. (Current pause flow does not write `Time.timeScale` in source — verify.)
- Other silent rejects on the same path: `_inputLock.IsLocked`, `_hasBattleEnded`. Capacity reject already raises `CapacityRejected` (announcement) and greys the button.
- Ruled out by source: `IsTrySpawning` stuck (cleared in both `Interrupt` and `CancelPlacement`); `BaseUi.Hide` only toggles `CanvasGroup` (no `SetActive`), so drag handlers keep receiving events.

## Decision — bug fix
- Remove the deploy cooldown (`SPAWN_COOLDOWN`, `_nextSpawnTime`). Drag/release already gates placement; cooldown adds no rule the player can see.
  - Why: commit message only says "limit spawning"; double-spawn is impossible because `IsTrySpawning` + one drag = one deploy.
  - Avoid: switching to `Time.unscaledTime` but keeping a hidden 1 s lock — still reads as "nothing happens".
- If a cooldown is still wanted (open question 1): keep it unscaled, grey the buttons while active, and show the remaining time in the tooltip.
- `ReinforcementUi.Handle`: do not hide the panel until the service confirms placement started (avoid Hide→Show flicker on reject).

## Feature Design — Shift-drag batch spawn
- Trigger: Shift held at drag start (`SpawnShipUi.StartPlacement`). Read via the input service (Shift already used by `InputBindingService`); never poll `Keyboard.current` in the view.
- Scope: `ShipUnitRequest` and `SquadronUnitRequest` only. Structures (`MiningFacility`, `DefendPlatform`) ignore Shift → normal single placement.
- Count: `n = min(reserveCount, floor(CapacityLeft / FactionData.UnitCapacity))`.
  - `n == 0` → existing `CapacityRejected` path.
  - `n == 1` → identical to single placement.
- Preview: one preview per unit in a formation around the cursor (reuse `FormationModel` offsets, spacing from `IShipSpawnClearance.GetPlanarRadius`); badge `xN` on the lead preview. Invalid slots tinted red per preview.
- Release validation (per slot, in order):
  1. Lead slot must pass `IReinforcementPlacement.IsPositionValid`; else whole batch rejected (same as single).
  2. Other slots: `IsPositionValid` → spawn; invalid → skip (unit stays in reserve).
  3. Re-check capacity before each spawn (`CanSpawnUnit`) — each `Spawn` calls `AddUnitCapacity`.
  4. `ShipSpawnClearance.ReserveLanding` from earlier spawns in the same batch must block overlapping slots (verify `IsClear` sees same-frame reservations).
- Consume: `ConsumeReinforcement` once per spawned unit; UI `DecreaseUnitCount` per spawned unit (event must carry the spawned count, not `bool`).
- Feedback: `ReinforcementDeployed` per unit or one batch event (open question 3); partial deploy → tooltip/announcement "N of M deployed".

## Outcome (2026-11-03)
- Cooldown removed; deploy is gated only by drag/release, battle end and input lock.
- `ReinforcementUi.Handle` hides the panel only when `IsTrySpawning` became true → rejected drag no longer flickers.
- Shift (`IReinforcementDeployInput` → `ReinforcementDeployInput`, `Keyboard.shiftKey`) at drag start: ships/squadrons get `ReinforcementModel.GetDeployableCount` previews; structures stay single.
- Slots: `FormationModel.CalculateGridDestination` around the cursor, spacing = `UnitSpawnView.PlanarRadius × 2.2` (`SLOT_SPACING_FACTOR`).
- Release: per slot → skip blocked (`CanSpawn` / `IsPositionValid`), stop on `HasRoomFor` false (+ `CapacityRejected`), else spawn + consume + `ReinforcementDeployed`.
- `OnSpawnUnit` is now `Action<int>` (deployed count); `ISpawnShipUi.DecreaseUnitCount(int)`.
- Tooltip appends "Shift + drag deploys all (N)." when N > 1.
- Partial feedback: capacity stop → existing `UnitCapReached` announcement; blocked slots → units stay in panel count. No transient toast added.
- Announcements: `ReinforcementDeployed` fires per unit; `FactionAnnouncementService` per-clip cooldown keeps it to one line.

## Decisions on open questions
1. Cooldown: removed.
2. Formation: centred grid around cursor.
3. Announcement: one line (existing clip cooldown).
4. Squadrons: included.

## Verification
- Unity recompile: completed, 0 errors.
- Tests authored, not run (local override): `ReinforcementDeployableCountTests` (new, `Economy`), `ReinforcementInteractionTests` updated for `_previews` / count event.
- Not verified in-game.

## Reusable guidance
- None extracted: behaviour is feature-local; the "silent reject looks like nothing happens" lesson is recorded here only.

## Files
- `Assets/Scripts/Services/Reinforcement/ReinforcementService.cs` (283 lines — candidate to extract a placement session), `IReinforcementDeployInput.cs`, `ReinforcementDeployInput.cs`
- `Assets/Scripts/Services/VContainer/Scopes/Players/LocalPlayerScope.cs`
- `Assets/Scripts/Entities/Reinforcement/ReinforcementUi.cs`, `SpawnShipUi.cs`, `ISpawnShipUi.cs`, `IReinforcementVisitor.cs`, `IReinforcementPresenter.cs`, `ReinforcementUiController.cs`, `ReinforcementModel.cs`
- `Assets/Scripts/Services/Reinforcement/ReinforcementPlacementFactory.cs`, `ShipReinforcementPlacement.cs`, `SquadronReinforcementPlacement.cs`
- `Assets/Scripts/Services/ShipSpawning/ShipSpawnClearance.cs`, `Components/Movement/Formation/FormationModel.cs`
- Tests: `ReinforcementInteractionTests`, `ReinforcementDeployableCountTests`

## Test Categories
- `Economy`, `BattleInput`, `Ui`, `Navigation` (clearance/formation). Not run locally (test override).

## Acceptance (user)
- Deploy one ship, immediately drag another → preview appears.
- Shift-drag with reserve 4, room for 3 → 3 deploy, 1 stays in panel with `x1`.
