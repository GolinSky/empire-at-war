---
category: Bugs
status: in-progress
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
1. [x] **Attack ring** (design A): `UnitOrderService.AttackOffsets` gives ships only, a ±60° arc on the group side, spacing from neighbour radii, capped at engagement range; squadrons get zero.
2. [x] **Approach point**: `AttackTargetState` uses the slot as a side only. Approach = target + slot direction × standoff distance. Remove the `MovementTargetPosition` leg (offset magnitude + contact).
3. [x] **Hold band** (design B): hold at 100% reach (any ship-capable mount, main batteries first) whether stopped or moving; resume only beyond 100%; a pursuing ship stops at 80% (predicted for a receding target). Replace the `!IsMoving && IsInRange` hold test.
4. [x] **Same-target re-issue**: `ShipOrderRunner.Attack` with the same target updates the offset without `CancelCombatHold`/state re-entry; `AttackTargetState` keeps `_hasHeldPosition`.
5. [ ] **Artillery**: hold within eligible main-battery range and retreat only at hull contact without turning away. Contact-only retreat is implemented. Launcher-only fallback remains (see Remaining).
6. [x] **Telemetry**: instrument Attack samples, hold transitions and new legs. The 18:25 battle exercised the new CSV fields.

## Initial progress 2026-11-18
- Deviations: ring spacing is an even spread over ±60° (slots sorted by current bearing), not neighbour-radius spacing; offset magnitude is only the max ship radius (direction is all the state uses). Artillery keeps the formation side for a fresh approach, `fromTarget` once held/retreating; `HasMainBatteryCover` no longer read by `AttackTargetState`.
- Hold: new `IWeaponComponent.IsWithinReach` (100%) + `ShipEngagement.IsInReach`; `AttackTargetState.ShouldHold` = pursuing ? 80% : 100%. Formation path merged into the standoff path; `MovementTargetPosition` and `_isClosingRange` removed.
- Same-target re-issue: `ShipOrderRunner.TryRefreshAttackSlot` + `UnitOrderModel.UpdateOffset`; `SetData` resets only on target/hardpoint change.
- Tests edited (not run): `ShipPursuitTests` (artillery ring/retreat, 2 new hold tests), fakes in `ShipStopTests`, `SquadronTargetSelectorTests`.

## Review 2026-11-18 (commit `001ab9e5b`)
- Matches design: offsets for ships only (`IHardPointAttackFacade`), side-ordered arc, `MovementTargetPosition` leg removed, hold at full reach / stop at 80% while pursuing, same-target refresh in `ShipOrderRunner.TryRefreshAttackSlot`, artillery retreats only at hull contact.
- [x] (fixed 2026-11-18) **Stale test (will fail):** `UnitOrderServiceTests.Attack_LargeGroupKeepsDistinctFiringBearingsAndMixedHullClearance` still asserts the old full ring; its `FakeCommand` has no `IHardPointAttackFacade`, so every offset is now zero. Rewrite for the arc contract: squadrons get zero, ship slots lie on the group's side of the target, slot order follows ship order, one ship gets its own bearing.
- [x] (fixed 2026-11-18: neighbour step = `2·asin((rᵢ+rⱼ)/(2·contact))`, contact = target radius + largest ship radius; whole arc scaled to ≤ ±60°) **Arc width ignores group size:** fixed ±60° → two ships are sent 120° apart (long sideways legs). Use angular spacing from neighbour radii at standoff distance, capped at ±60° (design A.3).
- [x] (added `ShipPursuitTests.Runner_SameTargetReorderedKeepsHoldAndOnlyUpdatesSlot`) Missing test: `ShipOrderRunner` re-issued Attack on the same target does not `CancelCombatHold` or re-enter the state.
- [x] (removed; test renamed `Attack_ArtilleryHoldsInsideLauncherBandInsteadOfBackingOff`) Orphan: `IArtilleryCapability.HasMainBatteryCover` (+ `ArtilleryCapability`, `WeaponComponent`, `ShipPursuitTests` stub) has no consumer after the retreat change; remove it.

## Playtest 2026-11-18 check (`Logs/Playtests/20261118T1013Z/movement_audit.txt`, `scripts/movement_audit.py`)
- Attack legs whose destination lies farther from the target than the ship (> 300 units, beyond weapon range): **2 samples in 32 min** (one AI ship, Player2). The map-edge run-away is gone.
- Same-target re-clicks kept the hold: 704 `refresh` events (human 33).
- Wrong-way travel per player 1.1–2.2% of moving samples. The visible "random direction" moves are AI order churn and patrol flip-flop (see the AI plan), not attack-order legs.
- The playtest build (`8787a78e1`) already had the hull-spaced arc; no run-away legs were seen. A dedicated group-attack check is still needed (below).

## Decision 2026-11-18 (user)
- Artillery ships do NOT hold at launcher range when their guns cannot reach. An artillery ship has main batteries assigned as missiles/torpedoes; that range is the only stop/keep-distance range.
- Too close → no turn-around/retreat rotation; retreat only at hull contact.

## Telemetry implemented 2026-11-18
- `AttackTargetState.AttackDiagnostics` and `TakeAttackEvent()` → `ShipOrderRunner.Tick` → `ShipOrderTelemetry` writes the `attack` column in `persistentDataPath/BattleTelemetry/*_ai_orders.csv`.
- Five-second `sample` rows capture hold/move reasons, pursuit and hold flags, range flags, formation bearing and magnitude, target and destination distances, and artillery reach. `attackHold` and `attackLeg` capture transitions.
- Compare the `destDist` and `dist` values in the `attackLeg` reason. A positive difference is an outward leg. The reason identifies the permitted `contactRetreat` exception. The CSV rounds distances to whole units.

## Battle verification 2026-11-18
- Source: `persistentDataPath/BattleTelemetry/Loading_20261118_182527_ai_orders.csv`. 9,599 rows, 777 Attack samples, 454 logged legs and 210 hold transitions.
- 453 approach legs (414 ordinary, 35 artillery, 4 hardpoint) had `destDist <= dist`. The sole outward destination was the hull-contact exception at 619.63 s: Player3 ship 702 → target 695, 288 → 626 units (+338), `artReach=696`, `preserveCourse=1`. The order was replaced 0.52 s later, so the run did not establish whether a complete retreat preserves facing.
- 158 `inReach` holds and 42 `pursuitStop` holds. Every pursuit stop had `inReach=1` and `inRange=1`; the CSV has no target velocity to identify a controlled fleeing-target case.
- 88 same-target refresh rows reported that the order was kept (5 Player0). Thirty were stationary, including 15 whose preceding same-target diagnostic showed `held=1`. The event verifies order reuse; the latest prior diagnostic and movement fields provide evidence of held ships staying put.
- Same-frame Attack issues included 16 Player3 ships against target 372 (59.11 s) and 13 Player0 ships against target 544 (125.45 s). The companion squadron log shows a Player0 V-wing attacking 544 at that time. The logs do not identify shared selection or target class, so a controlled mixed-group station/capital scenario remains unverified.
- All 10 `artilleryBand` holds were within computed `artReach`. Four had ordinary `inReach=0`. That flag uses a different range calculation; neither field identifies the eligible main batteries or proves the launcher-only fallback was exercised.

## Test report 2026-11-18
- `Assets/TestResults_20261118_184635.xml` (EditMode, 16:43:32–16:44:03 UTC): 4,114 total, 4,052 passed, 58 failed, 4 skipped. This audit read the supplied report and did not rerun tests.
- `Navigation` / `ShipPursuitTests`: 70 passed, 4 failed. Hold across full reach, pursuit stop, same-target refresh and all seven artillery state cases passed. Failures: target replacement expects old offset magnitude in the destination (225 vs 205); three old compact-ring cases expect hull spacing the new capped arc does not promise, including two strict float comparisons. Replace old ring expectations with live bearing-only and contact contracts while retaining meaningful clearance coverage.
- `BattleInput` / `UnitOrderServiceTests`: 27 passed, 3 failed. Two squadron `GroupPace` failures predate source fix `264854d96` and need a rerun. `GuardReservesTargetHull_AttackIssuesHullRelativeSlot` still expects the old 10-unit magnitude; the bearing token uses the 5-unit ship radius.
- `Combat`: 37 failures share a null reference at `WeaponHardPoint.ExpectedShotsInFlight`. It blocked the real artillery range and hardpoint approach assertions, including `WeaponRangeTests` fixtures that pass a null shot prefab. The other 14 failures span squadron, capture, game flow, abilities, core, economy and enemy AI fixtures.

## Remaining
- [ ] Enforce the recorded artillery decision when every eligible main battery is absent, destroyed or unable to engage the target. `ArtilleryCapability.GetArtilleryRange` currently clamps launcher reach only when `mainReach > 0`, so it can still return launcher-only reach. Verify whole-order behavior, including the fallback to ordinary engagement.
- [ ] Verify that a hull-contact retreat completes without turning away. The state requests ordinary movement with `preserveCourse` on the first attempt, and the observed retreat ended before it could establish facing behavior.
- [ ] Reconcile obsolete formation assertions and restore the blocked weapon fixture coverage. Rerun the affected `Navigation`, `BattleInput` and `Combat` cases when authorized.
- [ ] Confirm the exact mixed-group attack on a station/capital and fleeing-target pursuit cases if closing playtest acceptance.
- Keep active while the artillery rule and affected regression coverage remain unresolved. The instrumented battle confirms no outward approach legs and confirms the expected contact-retreat exception.

## Files
- `Entities/Ship/StateMachine/AttackTargetState.cs`, `ShipEngagement.cs`
- `Entities/Ship/Orders/ShipOrderRunner.cs`
- `Components/Weapon/ArtilleryCapability.cs`, `WeaponComponent.cs` (`HasEnoughRange`, `TryGetHardPointApproach`)
- `Components/Ship/Movement/ShipMoveComponent` (`BrakeForReplan`)
