---
category: Bugs
status: done
created: 2026-11-16
design_state: implemented
verification: "Unity 6000.4.7f1 compilation: 0 errors; source/test review and
  diff checks complete. Tests, gameplay and matched CPU captures not run."
completed: 2026-11-16
acceptance: pending-user
---
# Multiselect Attack Overlap

## Goal

- Prevent persistent ship overlap after multiselect Attack / Attack Move while retaining budgeted navigation and cached static obstacle graphs.
- User authorized execution and a scoped commit on 2026-11-16. Implementation complete; gameplay, tests and matched CPU acceptance remain with the user.

## Decision

- Chosen: distinct attack bearings plus shared asynchronous Burst overlap detection and budgeted route-based settling.
- Why: combat range stops bypass normal arrival clearance; concentric offsets projected onto one firing radius share bearings.
- Avoid: moving ships in static graphs, per-frame all-pairs scans, per-ship jobs, direct Transform pushes, unlimited retries.
- Scope: persistent stopped combat overlap. Crossing ships can temporarily overlap; this is not continuous collision avoidance.

## Implementation

### Combat slots and holds

- `UnitOrderService.AttackOffsets`: O(N) single angular ring, clockwise from group-side bearing; radius = max(2R, R / sin(π/N)) for N > 2, otherwise 2R. R = largest receiver navigation radius.
- Hull-relative offsets retain distinct bearings; `AttackTargetState` adds live `ContactDistance` radially without independently clamping rings. Closing pursuit retains the assigned bearing.
- `ShipOrderRunner` retains offsets on replacement attacks. Coincident-center standoff uses a nonzero fallback direction.
- `HoldPosition(target)` stops the original approach once, then preserves a checked settling leg across repeated range holds and preserve-course pursuit.
- Attack, Attack Move, Guard, Hunt, minimum-range, hardpoint and artillery hold branches use this intent. Target loss/change, state exit, ability approach, new orders and explicit Stop cancel it.
- Normal arrival correction remains bounded to one retry. Settling arrival skips that independent retry; the shared coordinator owns subsequent correction timing.

### Snapshot and bounded queries

- One `ShipSpacingCoordinator` owned by battle `ShipNavigationService`; authority-only, suspended when scaled delta time is zero.
- Reusable persistent native buffers; one `ShipSpacingJob : IJobParallelFor` batch when due at most every 0.2 s, without catch-up.
- Main thread snapshots registrations, revisions, positions, radii, vertical spans and eligibility. Jobs contain numeric data only; no entity or Transform access.
- Center cells use width max(2 × largest radius, 1); 3 × 3 cells therefore cover every current penetrating pair, including large/small hull combinations.
- O(N) index build creates contiguous cell ranges and full-cell quantized occupancy hashes. Each query checks at most 64 occupants and 9 cell hashes.
- Fair cursors rotate bounded occupant visits; rejected candidates consume the cap. Saturation never establishes clearance. Full-neighborhood hashes detect changes outside the visited slice.
- Higher registration ID yields when both can move; immobile/failed participants anchor. Exact-center pairs use stable nonzero separation.
- Results are consumed only after IsCompleted then Complete. Complete precedes buffer resize/disposal. Identity, revision, own/blocker position, target position, eligibility and actual pair overlap are revalidated.

### Navigation and retries

- Normal orders retain priority. At most one correction uses spare capacity within the existing maximum 4 plans/frame and 2 ms check-between-plans budget.
- Main-thread consumption examines at most 32 results/frame. Correction requests are deduplicated by movement eligibility; routes retain turns, braking, map/static clearance and hazard costs.
- Correction reservation checks use a reusable scoped spatial index of live current, active-final and pending-final centers; no new O(N) scan per route candidate.
- Reservation query cap: 256 visited centers / 64 cells; cap exhaustion conservatively blocks the candidate. Normal queries outside the correction scope retain their existing semantics.
- Retry cooldown = 1 s. Failed routes wait for meaningful pair, target, local occupancy or reservation changes; do not use the registry's scanning Version getter for each retry decision.
- Reservation revision is cheap and global; unrelated reservation changes can conservatively wake a failed correction, still subject to the cooldown and spare navigation budget.
- Obstacle-ignoring ships preserve their existing exemption and receive no automatic spacing correction. Their occupancy still blocks ordinary ships.
- Ion-disabled, zero-speed, hyperspace, released and unregistered ships receive no correction. Explicitly stopped ships remain anchors.

## Important Values

- Separation margin: 0.1 world units; hull bottom/top overlap semantics unchanged.
- Markers: `Battle.Spacing.Snapshot`, `Battle.Spacing.Index`, `Battle.Spacing.Consume`; worker: `ShipSpacingJob`; existing `Battle.Navigation.*` route markers remain.
- `ShipNavigationService` exposes visited-pair, saturated-query, correction, failed-correction, reservation-saturation and queue-age counters.
- Existing candidate limits remain 2 × 8 + center, then 1 × 8 + center fallback. One expensive plan can still exceed the 2 ms budget.
- Snapshot/index cost is O(N) at up to 5 Hz; correction scope construction is O(N) per admitted correction. CPU neutrality and savings are not established by source review.

## Edge Cases

- Full firing circumference, map bounds or static obstacles can make a slot infeasible. Reservations and bounded settling defer movement; no guarantee every ship can fire while separated.
- The single-ring allocator removes repeated projected bearings; it does not solve optimal multi-ring annulus packing. Explicit caller-supplied offsets remain supported.
- Mixed vertical hulls pass when their spans are disjoint; navigation circles remain conservative.
- Registration identity and command revisions reject stale pooled/dead/new-order work. Clients receive authoritative movement through existing replication.
- Normal static graph caching, stationary pursuit suppression, facing solve budgets and weapon cadence are preserved.
- Relevant existing classes exceed 200 lines; changes remain surgical. Any broader navigation/movement split is outside this fix.

## TODO

- [x] Trace and revalidate combat stops, formation offsets, reservations, route completion and CPU limits.
- [x] Implement distinct attack bearings, live contact floor, replacement offsets and nonzero coincident standoff.
- [x] Implement shared unmanaged Burst batch, bounded fair queries, scoped serial revalidation and settling intent.
- [x] Review source and affected tests; add regression cases for geometry, lifecycle, CPU bounds and failure wakeups.
- [x] Complete independent source review and Unity script compilation/import verification.
- [x] Extract reusable rules into [[Rules/BATTLE_PERFORMANCE_GUIDELINES#Combat overlap settling · 2026-11-16]].
- User gates → [[TODOs/User_Acceptance_Queue#Multiselect Attack Overlap]]: authorized test runs, in-game overlap/lifecycle checks and matched Editor/Development Player CPU comparison.

## Verification

- Unity 6000.4.7f1 script recompilation completed with 0 errors; changed sources and new script metadata imported. Editor console reported 0 errors.
- Independent source review and scoped `git diff --check` completed. Regression tests were authored/reviewed, not executed.
- No baseline reproduction, Play Mode run, profiler capture or Development Player comparison performed; no measured CPU improvement or neutrality claimed.

## Files

- Movement: `Assets/Scripts/Components/Ship/Movement/{IShipMovement,ShipMoveComponent}.cs`.
- Orders/states: `Assets/Scripts/Entities/Ship/Orders/ShipOrderRunner.cs`; `StateMachine/{AttackTargetState,AttackMoveState,GuardState,HuntState,ShipEngagement}.cs`.
- Formation: `Assets/Scripts/Services/UnitOrders/UnitOrderService.cs`.
- Navigation: `Assets/Scripts/Services/ShipNavigation/{ShipNavigationService,ShipDestinationRegistry,IShipSpacingAgent,ShipSpacingCoordinator,ShipSpacingBatch,ShipSpacingInput,ShipSpacingResult,ShipSpacingJob}.cs`.
- Updated tests: `ShipNavigationServiceTests`, `ShipPursuitTests`, `ShipDestinationRegistryTests`, `ShipStopTests`, `NavigateStateTests`, `UnitOrderServiceTests`; new `ShipSpacingJobTests` / `ShipSpacingCoordinatorTests`.
- `EnemyUnitCommanderTests`: updated four construction sites for explicit offline session dependency; assertions unchanged.
- Reviewed unchanged: `FormationModelTests` (Move/Guard compact slots), `ShipAvoidancePlannerTests` (ship contacts excluded), `AttackMoveEngagementTests` (target membership/claims unaffected).
- Categories reviewed: Navigation, Combat, Ships, BattleInput; EnemyAi construction fixtures also updated. Test execution disabled by checkout override; no tests run.
- Related: [[TODOs/Optimization/CPU_Allocation_And_Jobs_Plan]], [[TODOs/Optimization/Battle_CPU_Performance_Plan]].
