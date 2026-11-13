---
category: Bugs
status: done
created: 2026-11-13
updated: 2026-11-13
completed: 2026-11-13
acceptance: pending-user
---
# Ship Route Curvature Plan

Research/evidence → [[Done/Bugs/Ship_Route_Curvature_Plan - Research]]. Builds on [[Done/Optimization/Graph_Pathfinding_Plan]]. Must stay valid for every archetype in [[Done/Features/Map_Archetypes_Plan]].

## Implementation
- Implemented 2026-11-13 with three subagents: disk-tagged funnel, persistent buckets, Burst tangent/arc repair, exact route pieces, precomputed speed profile and tangent follower.
- Removed Bezier classes, sampled route rejection, hazard cost-preservation rejection and midpoint staircases. Direct shortcut still checks obstacle clearance and hazard penalty.
- Source review and Unity compilation passed; no tests, Play Mode, runtime profiling or online measurements were run. Graphify freshness query failed on the existing update lock; live source was used.
- Authored regression coverage: point/tag order, mirrored winding, exact arc geometry, speed profiles, moving replans, zero-speed ion effects, overlapping/protruding disks, bounds, hazards and explicit per-archetype sweeps.
- User-only verification → [[TODOs/User_Acceptance_Queue]]. Network route compression remains conditional on measured traffic; existing display-sample messages are retained.
- Reusable guidance extracted → [[Rules/BATTLE_PERFORMANCE_GUIDELINES#Ship pathfinding]].

### Reviewed corrections
- A non-corner disk can pierce an **arc**, not only a tangent line. Both use analytic bucket checks; arc repair inserts the obstructing disk between two occurrences of its enclosing corner circle.
- Untagged fillet portal width is only a displacement cap. Solve full-circle contact bounds against nearby disks before accepting the radius.
- Local repairs use append-only nodes with predecessor/successor indices; insert/remove touches neighbours without shifting the remaining route.
- Preserve long same-disk winding across merged corners. Re-check the new first tangent after rejecting a start circle.
- Escape prefix stays unchanged; fillet its original two adjoining lines only when the full circle is clear and the arc stays within map bounds. Otherwise stop/turn at the entry.
- Zero speed is valid during Ion Shot/Ion Pulse: brake, hold the route indefinitely, rebuild the remaining profile when speed returns.
- A goal closer than the current stopping distance cannot both preserve deceleration and stop exactly. Stop at the validated endpoint; never overshoot into unchecked geometry.
- Runtime guarantees and cheaper CPU/fewer keyframes remain acceptance claims, not measured results.

### Post-review fixes (2026-11-13)
- Burst `throw` → abort in Player builds. `ShipTangentRouteJob` now writes `Failure` (`FAILURE_NO_TANGENT`, `FAILURE_ARC_REPAIR`, `FAILURE_LINE_REPAIR`) to a `NativeReference<int>`; builder logs `Debug.LogError` and returns the funnel polyline (turn in place at corners).
- Missing tangent from the start circle (exact contact from `LimitRadius` + later repair) → reset start to `r = 0` inside the job and continue.
- Start turn direction: try both signs (`TryStartCircle`), keep the shorter start arc + leg. Why: forcing the first corner's wrap side swept ~320° when that corner lay on the opposite side of the heading.
- Regression test authored, **not run**: `ShipTangentRouteBuilderTests.FirstCornerAheadLeftWrappedClockwise_StartTurnTakesShortSide`. Unity compile passed.
- Reusable guidance → [[Rules/UNITY_JOBS_AND_THREADING_RULES]] JOB-12 (no Burst throws for runtime failures).

## Goal
- Every successful graph search yields a flyable curved route on the first build: no generate → reject → fallback chain.
- Ships keep speed through bends; slow only where geometry is genuinely tight, never stop at graph corners.
- Cheaper than today on CPU; fewer dead-reckoning keyframes online. Holds at 8 players on Large maps.

## Issues (pre-implementation evidence)
1. `ShipRoutePlanner.Build` = 3 candidates, all-or-nothing; any failed check → straight polyline.
2. Polyline corners have tangent breaks → `GetCornerSpeedLimit` → ~0, `isTurnLimited` holds progress → stop-and-turn at each corner.
3. Successful Bezier corners still have radius ≈ `0.15 × leg`, unrelated to ship turn radius `v/ω` → slowdown anyway.
4. Fallback candidates (2), (3) start with turn in place → speed 0 at route start.
5. Hazard rule (`× 1.05 + 0.01`) and portal-midpoint zig-zags on hazard maps reject curves.
6. Obstacle clearance checked twice (inflated graph + `IsRouteClear` sampling); second check can only reject.
7. Follower costs 8-step binary search + 8 corner samples per ship per tick; Bezier curvature drifts from dead-reckoning arcs → extra keyframes.

## Decision
- **Chosen:** tangent-arc route (lines tangent to circles + circular arcs) built from the funnel corners, plus a precomputed speed profile.
  - Why: blocking obstacles are disks inflated by clearance (`FieldVolume` circles, station disks); funnel corners are samples of those circles. The taut string around them is lines + arcs, clear by construction. Closed form, O(corners), Burst-compatible.
  - Why (online): each piece has constant turn rate = the shape `EntityMotion.Extrapolate` predicts. Not exact: `TurnRate` is sent as whole °/s (`sbyte`), speed in 0.5 steps, and speed ramps are not constant-speed → fewer keyframes, not zero.
  - Turn limits handled by speed, not geometry: arc of radius `R` → `v ≤ ω · R`. Always feasible, never a stop.
- **Corner radius = inflated disk radius `R`, always** (2026-11-13). No enlargement step, no extra checks. Wider/faster turns = possible later improvement, never required for a valid route.
- **Hazard edges are soft** (user-confirmed 2026-11-13, incl. Nebula Corridors): never a reason to reject or to add zig-zag corners; ships may clip hazard edges on curves. Impassable = disks only. See Phase 4.
- **Clear by construction covers corner circles only** (review 2026-11-13). Two pieces need one bounded check each, with a fallback that is always valid — not a rejection chain:
  - Start piece (start arc + tangent leg, also direct route and turnaround): check against disk buckets and map bounds → on hit use `r = 0` (turn in place onto the funnel leg, already safe).
  - Tangent line piercing a non-corner disk: insert that disk as a corner circle and recompute locally. Cause: tightening from polygon vertices to circles shifts lines by ≤ sagitta (`≈ cell² / 8R`, ≤ 8.2% of `R` at `n = 8`); a grazed larger disk has a smaller margin.
- **Heading = piece tangent exactly** (user-confirmed 2026-11-13). Set `_angularVelocity = v·κ` so banking still works.
  - Why: best dead-reckoning fit; no heading lag.
  - Accepted: yaw rate jumps at line↔arc joins; `turnAcceleration` no longer limits route turns (still used by `PlayLookAt` and turn in place).
  - Avoid: `StepYaw` chasing the tangent on routes (hull lags ≤ 10°, prediction drifts at joins).
  - Consequence: `ω` = `rotationSpeed`; the `_cornerTurnRate` yaw-acceleration margin and `_turnLead` are removed from route following.
  - Exception: start above the first arc's limit (`v0 > ω · r`) → hull cannot hold the tangent; follow the arc with `StepYaw` until `v ≤ ω · r`, then switch to exact tangent.
- **Avoid:** more Bezier handle retries; safe-corridor Bezier (needs solver); state lattice / Hybrid A* (search cost × ship count × replan rate).
- Planning stays host-only; no cross-machine determinism required.

## Map Archetypes Impact
| Archetype | Effect on routes | Plan response |
|---|---|---|
| Asteroid Belt, Twin Pass, Dense Field | Walls = chains of overlapping `FieldVolume` disks; scalloped outline for small-clearance ships; many corners, narrow gaps | Outer/inner tangents between overlapping disks are valid; merge same-disk corners; persistent disk buckets (more disks) |
| Orbital Ring | Planet blocks centre → one huge disk; long arcs around it | Large `R` → fast arcs. Requires planet as a disk `MapObstacle` (archetype-side) |
| Nebula Corridors, chokepoint plugs, central storm | Hazards act as walls; hazard outline is a raster staircase (`NavMeshHazardBoundaryJob`) → today: many midpoint zig-zags | Soft hazard edges: no cost guard, one fillet per corner (Phase 4). This archetype is the main hazard test case |
| Islands, perimeter/inner station slots | More routes along the map edge and across voids | Map-edge corners become common → Phase 4 bounds handling is core, not edge case |
| 6–8 players on Large | More ships → more plans per frame (cap 4/frame, 2 ms budget), more keyframes | Single-build planner + O(1) follower; measure on Large 8-player; compact route message moves up in priority |

- Contract for archetypes: **every blocking obstacle is a disk or a union of disks.** Polygon walls or other blocker shapes break the tangent-arc model → not allowed without revisiting this plan.
- Contract for archetypes: hazard "walls" steer by cost only; ships may clip their edge by ≤ one fillet bulge. If a hazard must be impassable, it must be a blocking obstacle (disks), not a hazard.
- Order: this plan does not depend on archetypes. Implement before archetype Phases 3–5 so their seed sweeps can include route metrics.

## Implementation Checklist

### Phase 1 — tag funnel corners with their circle
- [x] `NavMeshPointsJob`: store per point `int DiskIndex` (−1 for map hull, map-edge crossings, hazard points).
- [x] `NavMeshFunnel`: emit per waypoint `(position, diskIndex, turnSign)`; turn sign from `Turn(apex, …)` (left/right).
- [x] Tags must survive triangulation: funnel reads `NavMeshData.Positions` (output of `NavMeshTriangulateJob`), tags are per input point. `Args.Default`, no refinement → output order expected = input order; assert `Positions.Length == points.Length` and equal order in a test.
- [x] Keep disk spatial buckets per layer (today `DiskBuckets` is `TempJob`, build-only) for the start-arc query; dense archetypes multiply disk count.

### Phase 2 — tangent-arc builder (Burst)
- [x] Merge consecutive corners on the same disk → one arc.
- [x] Taut string: for each corner circle `(c, R, side)` compute the line tangent to previous/next circle (outer tangent for same side, inner for opposite). Drop a circle whose arc sweep ≤ 0 and recompute locally (amortized O(n)).
- [x] Overlapping disks (field walls): outer tangent between two corner circles stays outside both; the concave seam is never a funnel corner.
- [x] Corner arc radius = disk `R`. No enlargement.
- [x] Untagged corners (map edge, hazard boundary): one fillet arc, `r = min(rTurn, tMax / tan(Δ/2), eMax / (sec(Δ/2) − 1))`; `tMax` = half the shorter leg, `eMax` = half portal width → stays inside free triangles.
- [x] Start: Dubins-style arc tangent to current heading; both turn sides tried, shorter arc + leg kept. Largest clear radius closed form per nearby disk: `r ≤ (|d|² − Rᵢ²) / (2(n·d + Rᵢ))` when the denominator > 0; capped at `rTurn`. `r → 0` = turn in place, used only when no arc fits.
- [x] Start piece check: arc + tangent leg vs disk buckets and map bounds (leg may sit ≤ `2·rTurn` off the funnel's first leg). Hit → `r = 0`. Same check for the direct-route shortcut (today only the straight segment is checked) and turnaround.
- [x] Each tangent line: bucket query for non-corner disks closer than `R`; pierced disk → new corner circle, recompute neighbours.
- [x] Escape leg → next piece: tangent break at the entry point. Fillet when it fits, else turn in place at the entry point.
- [x] Replace `CubicBezierSegment` routes with `ShipRoute` of `Line`/`Arc` pieces: exact length, O(1) evaluate by distance, display samples generated on demand.

### Phase 3 — speed profile + follower
- [x] Per piece max speed: line `vMax`; arc `min(vMax, ω · R)` with `ω` = `rotationSpeed`.
- [x] Forward/backward pass with `SPEED_RAMP_TIME` acceleration → braking before tight arcs and destination.
- [x] Profile start = current speed `v0` (replan keeps speed). `v0 > ω · r` on the first arc → brake at ramp rate along the arc and let heading lag until `v ≤ ω·r`; never snap speed, never stop.
- [x] `ShipNavigationPlan.MovementDuration` = `Length / speed` today → integrate the profile instead (check consumers first).
- [x] `TickRoute`: `s += v(s)·dt`, position and heading from piece; `_angularVelocity = v·κ` (signed) for `SmoothBank`. Remove binary search, `GetCornerSpeedLimit`, `isTurnLimited` hold. Keep `BrakeForReplan` and replan-keeps-speed.

### Phase 4 — remove the rejection chain
- [x] `ShipRoutePlanner.Build` → one route. Delete candidates (1)–(3), `HANDLE_SCALE`, `IsRouteClear` re-check on graph routes, `PreservesTravelCost`.
- [x] Hazards: A* corridor already prices hazards. Remove funnel cost guard (`AppendLeg` midpoint insertion, `PreserveCorridorCost`) → no zig-zags on staircase outlines. Keep `IsUnpenalizedSegment` for the direct-route shortcut only.
- [x] Map bounds: corner arcs (radius `R`) lie inside the polygon path → inside the map when the funnel path is. Crossing vertices (`AddCrossings`) sit on the hull beside their disk → not expected as turning corners; confirm in the map-edge fixture. Real bounds risk = start piece → keep a bounds test there (replaces `ContainsRoute`).

### Phase 5 — online acceptance (pending user)
- [ ] Verify keyframe rate drops (count `DeadReckoningSender` sends per moving ship), including an 8-player Large map once archetype Phase 6 lands.
- Conditional follow-up, only if measurement warrants it: `ShipRouteMessage` → compact pieces (start, heading, length, signed curvature) instead of display samples; client tessellates the line. Do it if 8-player measurement shows route traffic matters.

### Phase 6 — verification (source/compile complete; execution pending user)
- [x] Tests authored, **not run** (categories `Navigation`, `Ships`): every piece keeps clearance from all inflated disks (arcs lie exactly on the boundary → compare with a small epsilon); start piece inside map bounds; G1 continuity at joins; arc speed ≤ `ω·R`; no zero-speed point except start-from-rest and goal; hazard-map route stays curved. Assert relationships, not tuned numbers.
- [x] Per-archetype route sweep authored, **not run** (`ShipArchetypeRouteSweepTests(false/true)`, both explicit `Navigation`; current archetype assets): random start/goal pairs per archetype × seed → clearance holds, no corner stop, route build time. Fixtures: overlapping-disk wall, huge central disk, staircase hazard wall, map-edge route.
- [ ] Profiler: `NavigationRouteBuild` and movement tick before/after on the same map; plan queue latency with 8 fleets.
- [ ] User in-game check: asteroid fields, hazard maps, large ships (Executor clearance), online client.

## Important Values
- Dead reckoning: tick 20 Hz, error 2 units / 5°, refresh 1 s moving.
- Disk samples `n = max(8, ceil(2πR / cellSize))` at `R/cos(π/n)` → tangent geometry deviates ≤ one chord sagitta from the mesh path.
- Raster cell = `max(4, mapSize/160)` → hazard staircase step size; larger maps → coarser steps.
- Plan queue: ≤ 4 plans/frame, 2 ms budget (`ShipNavigationService`).
- `rTurn = v / ω` (`ShipRotationKinematics.CalculateMinimumTurnRadius`).

## Edge Cases
- Ship inside clearance → keep existing escape leg (`allowEscape`) as a straight line to the entry point.
- Destination behind ship → start arc + tangent covers the turnaround; `BuildTurnaroundRoute` removed.
- Tiny obstacle + huge ship (`R < rTurn`) → slower arc, still continuous; no stop.
- Corner circle dropped by taut string → neighbours recomputed; never fall back to the whole polyline.
- Narrow archetype gaps (`MinLaneWidth`) → inner tangents; gap below clearance → no corridor (unchanged).
- Station destroyed → obstacle set changes → new layer (unchanged).

## Files
- `Services/ShipNavigation/{NavMeshPointsJob,NavMeshData,NavMeshFunnel,NavMeshRouteWorkspace,ShipNavMesh,ShipRoutePlanner,NavigationHazardRaster,IShipPathGraph}.cs`
- `Components/Ship/Movement/{ShipBezierPath,ShipBezierRoute,ShipMovementTweenPlayer,ShipAvoidancePlanner}.cs`
- `Services/Networking/{HostShipRouteSender,DeadReckoningSender}.cs`, `Messages/ShipRouteMessage.cs`
- `ShipBezierRoute` consumers (63 refs, 15 files): also `Components/Ship/Movement/ShipMoveComponent.cs`, `Services/ShipNavigation/{ShipNavigationService,ShipNavigationCache,NavMeshTriangulateJob}.cs`
- Tests: `Tests/Editor/Navigation/{ShipBezierRouteTests,ShipNavigationServiceTests,ShipAvoidancePlannerTests,ShipRotationKinematicsTests,ShipPathGrid}.cs`
- Obstacle inputs: `Entities/Map/{FieldVolume,BattleMap}.cs`, `Entities/Map/Generation/FieldVolumeCover.cs`, `Services/ShipNavigation/MapObstacleContactProvider.cs`

## TODO
- No in-game capture of a stalled route; not needed before implementation (redesign removes the whole fallback chain). Measure after: lowest speed per route, keyframes per moving ship.
