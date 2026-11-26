---
category: Bugs
status: done
created: 2026-11-25
completed: 2026-11-25
acceptance: pending-user
---
# Spacing Nudge Turns Ship Away From Target

## Goal
- A ship holding on an Attack / Attack-Move target never turns its back to that target because of hull spacing.
- Overlapping holders still separate when a sideways slide is possible.
- Related: [[Review/Bugs/Attack_Order_Hold_In_Range]], [[Done/Bugs/Capital_Movement_And_Class_Rework_Fixes]] (overlap settling `bf9a0b564`).

## Cause (source, 2026-11-25)
- Ships hold the moment guns reach (`AttackTargetState.HoldAndLookAtTarget`, `AttackMoveState` `CanHoldOn`) → a multi-selected group stops bunched.
- `ShipSpacingJob.Execute`: `Goal = other.Position + direction * (radius + SEPARATION_MARGIN)`; `direction` = away from the blocker. Ignores target position and ship heading.
- `ShipMoveComponent.TrySettle` → `Plan(destination)` flies the hop as a forward route. Blocker between ship and target → goal behind the ship → ~180° turn, hop, turn back.
- Only guard: `KeepsHoldTargetInReach` (goal inside weapon reach). No turn check, no short-hop loop check.
- Yield rule: of an eligible pair the higher `RegistrationId` moves; the lower moves only if the other is `Anchored` or not eligible.

## Evidence
- Telemetry `BattleTelemetry/Loading_20261124_{173558,185817,204616}_ai_orders.csv`, `sample` rows with a live target, `facingDot` = forward · direction to target.

| State | Rows | `facingDot < 0` | `facingDot < -0.7` |
|---|---|---|---|
| Settling (`movement` field 5 = 1) | 852 | 38–43% | 19–21% |
| Holding | 9,934 | 4–5% | 1% |
| Moving | 11,469 | 4–6% | 1–2% |

- Ruled out: standoff point behind a close ship. ~14,000 `attackLeg` rows, 1 with `destDist > dist`.
- Not verified: the exact in-game moment the user saw; Attack-Move pursuit legs have no leg telemetry.

## Decision
- Chosen: slide the nudge sideways around the hold target; refuse a nudge that needs too large a turn.
- Why: a tangential hop keeps range and keeps the target abeam or ahead; a refused pair costs only a small overlap.
- Avoid: raising `MIN_PENETRATION_FRACTION` (hides overlap, keeps the 180° turn); reverse/strafe motion (new movement mode); retries.
- Accepted cost: refused pairs stay overlapped.

## Outcome (2026-11-25)
- Steps 1–6 implemented. Unity compile clean (0 errors). Tests written, not run (local override).
- Deviations from the plan:
  - Turn check runs once after the blocker loop, on the deepest blocker's goal → refused = `HasCorrection` false, `HasOverlap` stays true.
  - Side tie-break: `dot(tangent, separation)` → `dot(tangent, Forward)` → `CoincidentDirection` (id hash).
  - Coordinator heading staleness: `HEADING_TOLERANCE_DEGREES = 5` between snapshot and current planar forward.
  - Short hop: `ShipMoveComponent.Plan` applies `ARRIVAL_CORRECTION_MAX_ROUTE_FACTOR` (2×) when `_isSettling` → clears `_isSettling`, stops, `TrySettle` returns false.
- Tests: `ShipSpacingJobTests` +4 (sideways slide, blocker on target line, goal behind heading refused, ship on target point fallback); helper default target `(0, 1000)`, forward `(0, 1)`. `ShipSpacingCoordinatorTests` +`heading` stale case; fake defaults target `forward * 1000`, `SpacingForward = forward`. `ShipNavigationServiceTests` +`CombatHold_SettleHopLongerThanItsDistanceFailsInsteadOfLooping` (fake `RouteVia`). Reviewed, unchanged expectations: all existing spacing job/coordinator cases and settle cases (fake routes are straight lines → pass the 2× check).
- Reusable guidance: none extracted; the rule lives in `ShipSpacingJob` comments (no canonical navigation-spacing note exists).
- Open user gates (steps 7–8) → [[TODOs/User_Acceptance_Queue#Spacing Nudge]].

## Implementation
1. [x] `ShipSpacingInput`: add `float2 TargetPosition`, `float2 Forward`. Filled in `ShipSpacingCoordinator.Tick` from `entry.TargetPosition` and a new `IShipSpacingAgent.SpacingForward` (`ShipMoveComponent` → `transform.forward`).
2. [x] `ShipSpacingJob.Execute`, goal for an eligible ship:
   - `radial = normalize(own.Position - own.TargetPosition)`; `tangent = ±perp(radial)`, sign with `dot(tangent, separation) ≥ 0` (away from the blocker's side).
   - Step `t` along `tangent` until clear: `t = -s·tan + sqrt((s·tan)² + R² − |s|²)`, `s = separation`, `R = radius + SEPARATION_MARGIN`. `Goal = own.Position + tangent * t`.
   - Ship on the target point (`radial` degenerate) → keep the current away-from-blocker goal.
3. [x] Turn limit in the job: angle between `own.Forward` and `Goal − own.Position` > `MAX_SETTLE_TURN_DEGREES` → no correction (`HasOverlap` stays true).
   - Start value 100°. A bow-on ship needs ~90° for any sideways slide, so 90° refuses almost everything; tune in-game.
4. [x] `ShipSpacingCoordinator.TryCorrect`: stale-snapshot check also covers forward (heading changed since snapshot → skip).
5. [x] Short hop: apply the `ARRIVAL_CORRECTION_MAX_ROUTE_FACTOR` route-length check in `ShipMoveComponent.Plan` when `_isSettling` → fail the settle instead of flying a loop. Likely, not verified: settle hops shorter than the turning circle loop today.
6. [x] Tests, category `Navigation` (not run — local override):
   - New in `ShipSpacingJobTests`: goal is sideways (distance to target ≈ current, within the tangent step); goal clears the blocker by `radius + SEPARATION_MARGIN`; goal behind the facing → no correction; ship on the target point → fallback goal.
   - Review: `CoincidentEligibleShips_OnlyHigherRegistrationYieldsWithNonzeroGoal`, `EqualPenetrations_SelectStableLowestBlockerIdRegardlessOfSnapshotOrder`, `FailedAnchoredBlocker_LowerRegistrationCanYield`, all `ShipSpacingCoordinatorTests` fakes (new `SpacingForward`).
   - `ShipNavigationServiceTests` settle cases: fixtures and expected goals.
7. [ ] (user gate, queued) Telemetry check after a battle: settling rows `facingDot < -0.7` ≤ 2% (holding baseline 1%).
8. [ ] (user gate, queued) User acceptance: multi-select 6+ ships, Attack and Attack-Move on one target; no ship turns its back while holding.

## Edge Cases
- Blocker exactly on the ship–target line → both tangents equal; pick the sign by `Forward`, then by id hash (`CoincidentDirection`).
- Broadside holder (solved facing up to 90° off the bearing) → one tangent is ahead, the other behind; the blocker-side choice may be the one behind → refused.
- Refused higher-id ship → pair stays overlapped. Optional follow-up: treat it as `Anchored` so the lower id may slide instead.
- Tangent step slightly increases range (`sqrt(D² + t²)`) → `KeepsHoldTargetInReach` still decides.
- Blocker not holding on any target (idle ship, station) → the yielding ship still uses its own target for the tangent.

## Files
- `Assets/Scripts/Services/ShipNavigation/ShipSpacingJob.cs`, `ShipSpacingInput.cs`, `ShipSpacingCoordinator.cs`, `IShipSpacingAgent.cs`
- `Assets/Scripts/Components/Ship/Movement/ShipMoveComponent.cs` (`TrySettle`, `Plan`, `SpacingForward`)
- `Assets/Scripts/Tests/Editor/Navigation/ShipSpacingJobTests.cs`, `ShipSpacingCoordinatorTests.cs`, `ShipNavigationServiceTests.cs`
