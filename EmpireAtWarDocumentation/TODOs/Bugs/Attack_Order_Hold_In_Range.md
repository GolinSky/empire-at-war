---
category: Bugs
status: todo
created: 2026-11-18
---
# Attack Order: Hold When in Range, No Run-Away

## Goal
- A ship ordered to attack does not move while its weapons reach the target (a **range band**, not a point).
- No leg away from the target / toward the map edge on an Attack order.
- Parent investigation: [[TODOs/Bugs/Capital_Movement_And_Class_Rework_Fixes]] (H3, 16:01 battle ISD II 256).

## Evidence
- 16:01 battle: ISD II 256, one Attack on target 164: drove (1087, 1405) → (1678, 2140) away from station (1723, −1660), distance 3130 → 3800; then turned 180° and headed to (1434, −1397).
- 3800 ≈ `ARTILLERY_APPROACH_FACTOR` 0.9 × artillery reach ≈ 4200. At 3130 the ship was inside the retreat band (`ARTILLERY_RETREAT_FACTOR` 0.8 × 4200 = 3360) → the retreat leg. Consistent; not yet proven (no reach value in telemetry).
- Online battles 07/08 have no movement telemetry (release build).

## Primary cause — attack ring radius (Argon analysis, source-verified 2026-11-18)
- `UnitOrderService.AttackOffsets`: ring radius = `max(2R, R / sin(π/N))`, where R = the **largest** `NavigationRadius` in the selection and N counts every `IAttackFacade` receiver, **squadrons included**. Bearings span the full 360°.
- Executor R 1059.4, N 22 → 1059.4 / sin(π/22) = **7,443 units**. `AttackTargetState.UpdateFormationMove` → `MovementTargetPosition` = target + offset + dir × contact → beyond the map → clamped at the edge. On arrival `_isClosingRange` → `StandoffPosition` → 180° turn back.
- Matches ISD II 256 (16:01): Argon reads save `isdIbug.json` `order.type 2` on target 164 with |offset| 7,444.31 → leg to (1678.2, 2140.1), then a standoff at (1434.8, −1397.1). Supersedes the artillery-band guess below.
- Ships whose slot is on the far side of the target drive around/away from it before engaging.
- Fix direction: cap the ring radius (e.g. ≤ engagement range), exclude squadrons from N and R, only use bearings on the group's side (arc, not full circle), and ignore the offset once `IsInRange`.
- Also from Argon (verified): `WeaponComponent.ENGAGE_RANGE_FACTOR` 0.8 → `IsInRange` is 80% of engagement range; a moving ship keeps closing because the hold check is `!IsMoving && IsInRange`.

## Fix design (proposed 2026-11-18)
### A. Attack ring (`UnitOrderService.AttackOffsets`)
1. Only ships get offsets. Squadrons get `Vector3.zero` and are excluded from N and R.
2. Arc, not circle: bearings spread over ±60° around the group's own side (centre → target reversed), so no slot is behind the target.
3. Offsets are directions plus lateral spacing only: spacing = sum of neighbouring `NavigationRadius` along the arc, not `R / sin(π/N)`. Radius never exceeds the group's engagement range.
4. `AttackTargetState`: a slot only picks the side. Approach point = target + slot direction × standoff distance (`StandoffPosition`); `MovementTargetPosition` (offset magnitude + contact) is removed.

### B. Hold band instead of the 80% point (user requirement: an ordered ship in range never moves)
- The 80% factor (`ENGAGE_RANGE_FACTOR`) exists so a pursuing ship closes past the edge and a fleeing target stays in range. Today it is also the hold test → a stopped ship at 80–100% of range moves again.
- New rule (hysteresis):
  - **Hold** when any ship-capable mount reaches the target from the current position (100% reach), whether the ship is stopped or just got a new order → brake and face.
  - **Resume** only when the target leaves 100% reach.
  - **While pursuing**, stop at 80% (or predicted 80% for a target moving away), so a fleeing target does not cause stop/start at the edge.
- Covers: re-issued Attack on the same target (no move), a new Attack while already moving and in reach (brake and hold), a target moving away (pursue to 80%), a target moving closer (keep holding; retreat only at hull contact).
- Same-target re-issue keeps `_hasHeldPosition` and the hold (no `CancelCombatHold`/state re-entry).
- Tests (Navigation/Ships): one per case above; assert movement calls, not distances.

## Other causes (source, `AttackTargetState.cs`)
1. **Artillery band = point, not range.** `TryUpdateArtilleryMove` holds only in `[retreat, 0.95 × reach]`.
   - Without main-battery cover: retreat below `0.8 × reach` → backs off to `0.9 × reach` (thousands of units, stern first, toward the map edge).
   - `reach` = min(launcher reach, main-battery reach) (`ArtilleryCapability.GetArtilleryRange`); losing a mount bumps `ArtilleryRevision` → `ResetPursuit` → new leg.
   - Cover lost when main batteries die → `HasMainBatteryCover` false → retreat band jumps from `contact + 1` to `0.8 × reach` mid-fight.
2. **Formation offset picks the side in world space.** Bearing = `_formationOffset` when not retreating and `!_hasHeldPosition` → destination `target + offsetDir × 0.9 reach` may be on the far side → long detour.
3. **A re-issued Attack resets everything.** `ShipOrderRunner.Attack` → new formation offset (group centre moved) ≠ old → `Issue` → `CancelCombatHold` + state re-entry → `ResetPursuit` (`_hasHeldPosition = false`). Same target, new click → ship re-plans.
4. **Non-artillery hold is ok, but approach uses formation slot.** `UpdateFormationMove` holds if `IsInRange`; otherwise drives to `MovementTargetPosition` (formation slot at contact distance) instead of nearest in-range point.
5. **Moving ships brake for any new route > 1° off heading** (`ShipMoveComponent.BrakeForReplan`) → re-orders stop moving ships.
6. Big targets (BattleCruiser/Dreadnought, footprint > weapon reach) take `TryUpdateHardPointApproach` first: rescans every 0.1 s and moves toward the nearest hardpoint offset even when other mounts already fire.

## Rule (target behaviour)
- In range = any ship-capable mount (main batteries first) can hit the target from the current position → **hold + face**, regardless of order re-issue or formation offset.
- Retreat only on hull contact (`contact + 1`), never because of launcher bands.
- Out of range → move to the **nearest** in-range point on the current bearing (`fromTarget`), not a formation slot on the far side.
- Same target re-issued → keep hold state (`_hasHeldPosition`), ignore formation offset change while in range.

## Plan
1. [ ] **Attack ring** (design A): `UnitOrderService.AttackOffsets` gives ships only, a ±60° arc on the group side, spacing from neighbour radii, capped at engagement range; squadrons get zero.
2. [ ] **Approach point**: `AttackTargetState` uses the slot as a side only. Approach = target + slot direction × standoff distance. Remove the `MovementTargetPosition` leg (offset magnitude + contact).
3. [ ] **Hold band** (design B): hold at 100% reach (any ship-capable mount, main batteries first) whether stopped or moving; resume only beyond 100%; a pursuing ship stops at 80% (predicted for a receding target). Replace the `!IsMoving && IsInRange` hold test.
4. [ ] **Same-target re-issue**: `ShipOrderRunner.Attack` with the same target updates the offset without `CancelCombatHold`/state re-entry; `AttackTargetState` keeps `_hasHeldPosition`.
5. [ ] **Artillery**: `TryUpdateArtilleryMove` holds while any main battery reaches; retreat only at hull contact; approach along `fromTarget`.
6. [ ] Telemetry (Editor/dev): per Attack sample log slot direction, hold/resume thresholds and the reason for each move.
- Open question for the user: should artillery ships hold at launcher range when their guns cannot reach?

## Agent handoff
- Implement steps 1–5 in order; do not change `ENGAGE_RANGE_FACTOR` users outside `AttackTargetState`/`ShipEngagement` without noting it.
- Do not run tests (local override); update/add tests listed below and report them as not run.
- Hand back: list changed files, which steps are done, and any deviation from designs A/B, then ask the user for review.

## Tests (review/update; not run — local override)
- `Navigation`/`Ships`: `ShipPursuitTests`, AttackTargetState tests, artillery hold tests (H3 main-battery hold).
- New: re-issued same-target Attack while holding in range → no `MoveToPosition`; ship at 0.5 × reach with guns in range → holds.

## Files
- `Entities/Ship/StateMachine/AttackTargetState.cs`, `ShipEngagement.cs`
- `Entities/Ship/Orders/ShipOrderRunner.cs`
- `Components/Weapon/ArtilleryCapability.cs`, `WeaponComponent.cs` (`HasEnoughRange`, `TryGetHardPointApproach`)
- `Components/Ship/Movement/ShipMoveComponent` (`BrakeForReplan`)
