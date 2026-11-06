---
category: Optimization
status: done
created: 2026-11-06
updated: 2026-11-06
completed: 2026-11-06
acceptance: pending-user
---
# Graph-based ship pathfinding (triangle navmesh)

## Goal
- Replace the 161×161 raster in `ShipPathGrid` with a Burst triangle graph: same planner contract, fewer search nodes, no cell-rounding blockages.
- Keep `ShipRoutePlanner` trajectory building (Bezier candidates, turn-in-place, polyline fallback) unchanged; it only consumes waypoints.
- All runtime data unmanaged (`NativeArray`/`NativeList`, `float2`); all build/search work in `[BurstCompile]` jobs.

## Raster baseline (verified 2026-11-06)
- `ShipNavigationService.PlanRoute` → `BuildNavigationContacts` drops ships and obstacles outside `NavigationHullSpan` → `ShipNavigationCache.GetGrid` → `ShipRoutePlanner.Build`.
- Obstacles are disks: `RadarContact` (x, z, radius, bottom, top). Clearance = `ShipMoveComponent.NavigationRadius`.
- Grid: cell = max(4, mapSize/160); inflation = clearance + 0.7072·cell; 8-neighbour A* (`NavigationRouteJob`), greedy LOS string-pull O(obstacles) per test, regions by `NavigationRegionJob`.
- Cache key: exact clearance + every height-filtered obstacle + hazard refs + map bounds. Grids never evicted during a battle.
- ~22 B/cell × 25,921 cells ≈ 0.57 MB per grid. 63 ships → 60 distinct radii (5.76–1059.44); 9 flight heights → up to ~60+ grids in a mixed battle.
- Hazards: non-blocking `IHazardSource.Contains` + `Data.PathCost` → `_extraCost` raster; independent of clearance.
- Planner-facing API to preserve: `Matches`, `SetOrigin`, `ContainsRoute`, `IsUnpenalizedSegment`, `PreservesTravelCost`, `TryFindPath`, `IsKnownUnreachable`, `TryGetNearestReachable`, `Dispose`.
- Profiling history: navigation max 81 ms (35 plans) → fixed by plan cap ≤4/frame + 2 ms budget; later captures max 2.63–18 ms; fallback-heavy sample 8.89 ms/frame inclusive. No new capture for this plan (user: local profiling imprecise; agents don't run Play Mode).

## Decision
- **Library:** BurstTriangulator `3.10.0` (MIT; Burst ≥1.8.7, Collections ≥2.2.0; project has Burst 1.8.29, Collections 6.4.0, Unity 6000.4.7f1). Call `UnsafeTriangulator<float2>.Triangulate(NativeInputData, NativeOutputData, Args, Allocator)` inside our own job.
  - Why: free, Burst-native, unmanaged output with `Halfedges` adjacency.
  - Avoid: DotsNav (Entities 0.51-preview, Unity 2021.3 → port); Recast/Detour (C++ native plugin); A* Pathfinding Project (paid, managed graphs).
- **Mesh = unconstrained Delaunay + exact walkability classification.** No constraint edges.
  - Why: BurstTriangulator rejects intersecting constraints ("ConstraintEdges cannot intersect with each other"); overlapping inflated disks would need a polygon union (Clipper2 is managed). Unconstrained input has no such rule.
  - Points: map rect corners + boundary samples; for each inflated disk, `n` samples on the circumscribed circle `R/cos(π/n)` (chords tangent to the disk, never inside it); drop samples strictly inside another inflated disk; hazard boundary samples (Phase 3).
  - Triangle walkable ⇔ triangle does not intersect any inflated disk (exact triangle–disk test, spatial-hash candidates).
  - Correct by construction: a funnel path stays inside its corridor triangles → keeps clearance from every disk.
  - Conservatism: triangles cut at disk-overlap seams are blocked; bounded by sample spacing (target chord ≤ current cell size so it is never worse than the grid's half-diagonal inflation).
- **Layer key (Phase 1):** same as today (clearance + filtered obstacles + hazards + bounds) for behaviour parity. Radius-aware single mesh per height band is Phase 5, only if layer count/memory warrants it.
- **Hazards:** keep one shared hazard cost raster per (hazard set, bounds) instead of per grid; triangle cost = raster sample at centroid; funnel validated against raster like the current `PullString` cost check.
- **Interface seam:** `IShipPathGraph` with the API above; `ShipPathGrid` and `ShipNavMesh` both implement it until acceptance, then the grid is deleted.

## Outcome
- Production uses `ShipNavMesh` behind `IShipPathGraph`. The Bezier, turn-in-place, reservation and straight-leg planning stages still consume height-preserving waypoints.
- Disks are classified against triangles. A* prices portal-midpoint crossings and the funnel checks the result against the shared hazard raster. Embedded ships validate their exit against all disks, including perpendicular exits from overlapping clearances.
- Unity 6000.4.7f1 compiled the gameplay and Editor assemblies with Collections 6.4.0. Eight navigation jobs compiled to native code through Burst Inspector; zero errors, one upstream triangulator warning `BC1370` in its unused invalid-preprocessor branch.
- The Editor console reported zero errors. The `Navigation` category was not run under the checkout override. Acceptance → [[TODOs/User_Acceptance_Queue]]. Frame time and memory savings are unmeasured.
- Guidance → [[Rules/BATTLE_PERFORMANCE_GUIDELINES#Ship pathfinding]]. Keep the Editor grid oracle until its agent-authored parity fixtures have run successfully.

### Review 2026-11-06 (source, compile, console)
- Verified: walkable ⇔ no inflated-disk contact; corridor rejects region 0; funnel signs match Mononen SSF with clockwise portals; A* heuristic admissible (weight ≥ 1); start node = `Halfedges.Length`, buffers `3T + 2`.
- Compile current: `Assembly-CSharp` 20:41:35, `Assembly-CSharp-Editor` 20:42:20, newer than every edited source; console 0 errors. Grid oracle `.meta` GUIDs preserved after the move to `Tests/Editor`.
- Behaviour change: a goal inside true clearance → unreachable (`FindReachableGoal` requires a clear exit→goal leg); the grid appended the raw goal. Destination rings/fallback cover it.
- Deviation: disk samples capped at 64 → chord ≈ 2πR/64; inflated disks with R > 64·cell/2π ≈ 10.2 cells (e.g. > 204 units at 20-unit cells: Titan, Executor clearances) get chords above the planned ≤ cell-size target → larger seam conservatism for large ships. **Fixed 2026-11-06:** cap removed in `NavMeshPointsJob`; chord ≤ one raster cell at every clearance. Recompile clean, console 0 errors; tests not run. Cost: more points per large-ship layer (e.g. R 1,360 at 20-unit cells → ~428 samples before covered-point removal).
- Match-start measurement 2026-11-06 (Editor eval on generated maps, Burst warm, no Play Mode): Large 9000², 216 field volumes, 6 hazards → hazard raster 8–16 ms once; mesh 2.5–3.8 ms/layer (4,200–6,000 points, 8,300–11,800 tris; Executor clearance 743 points); old grid 7.5–14 ms/layer; search 0.01–0.03 ms; blocked-destination check ~0.17 ms each (full triangle scan); embedded Executor plan 0.75–1 ms. Medium 6000²: 2.3–3.7 ms/layer, raster ~4 ms.
- Fixes 2026-11-06: `ShipNavigationMapPreparer` (observes `BattleMap`) builds the hazard raster during map load via `ShipNavigationService.PrepareMap` → `ShipNavigationCache.GetRaster`; `ShipNavMesh.FindReachableGoal` rejects goals strictly inside an inflated disk before the locator (same strict test as `CanConnect`, answer unchanged). Tests added: `PreparedHazardRasterIsReusedByFirstPlanningLayer`, `GoalInsideInflatedDiskIsUnreachableWhileGoalJustOutsideIsReachable` (not run). Not done: per-roster mesh prewarm — layer key needs runtime hull spans.
- Editor Play Mode skirmish capture 2026-11-06 (user-requested; 8 × 10 s windows from Battle activation, gaps between windows; ≤ 12 ships, human idle): navigation `GridBuild` once 5.4 ms during the loading tail (hazard raster prep), 52 plans total, no navigation marker in any > 25 ms frame. Hitches: first ~3 s after activation (474 ms activation frame, 25–67 ms frames, main thread mostly 2–12 ms → Editor/scene-integration time outside PlayerLoop) and one window at ~210 s (26–49 ms frames, main 4–6 ms). Captures: `LocalLow/GolinSkyCompany/EmpireAtWar/BattleCaptures/battle_20261106_2024*–2028*`.
- Cost risks (remaining): 5 hazard-boundary points per raster cost transition join every layer; builds run synchronously (`.Run()`) on the first plan per layer; triangulation failure throws inside planning; layers/rasters never evicted (same as grid).

## Implementation

### Phase 0 — package + seam
- [x] User approved BurstTriangulator v3.10.0. Unity Package Manager installed the Git tag and locked `ecb14bf2d2c413c87af0363efbce6565a9303d2d`.
- [x] Collections 6.4.0: the pinned package compiled. Unity’s compiler response files for `Assembly-CSharp` and `Assembly-CSharp-Editor` reference its DLL. Navigation fixtures compile in the latter; custom benchmark asmdefs do not consume the package.
- [x] Extract `IShipPathGraph` from `ShipPathGrid`; `ShipRoutePlanner`, `ShipNavigationService`, `ShipNavigationCache` depend on the interface. No behaviour change.

### Phase 1 — mesh build (Burst)
- [x] `NavMeshPointsJob`: disks (x, z, r+clearance) → sample points; drop covered samples; map boundary points.
- [x] `NavMeshTriangulateJob`: `UnsafeTriangulator<float2>` with `Allocator.Temp` scratch, persistent `Triangles`/`Halfedges`/`Positions` output; `Status` checked (fail fast on non-OK).
- [x] `NavMeshClassifyJob`: walkable flag per triangle; `NavMeshRegionJob`: BFS over `Halfedges` → region id per triangle.
- [x] `NavMeshLocator`: uniform bucket grid (≈32×32) of triangle ids for point → triangle lookup; nearest walkable triangle for points inside clearance (escape case).

### Phase 2 — search + waypoints (Burst)
- [x] `NavMeshRouteJob`: A* over triangles; heap/generation stamping reused from `NavigationRouteJob`; edge cost = portal-midpoint distance × (1 + hazard); heuristic = Euclidean to goal.
- [x] Simple stupid funnel over corridor portals → `NativeList<float2>` waypoints; hazard cost guard splits a funnel segment that raises raster cost > 0.01.
- [x] Unreachable/region logic: `IsKnownUnreachable` via region ids; `TryGetNearestReachable` = closest point on walkable triangles of the origin region (Burst scan).
- [x] `ShipNavMesh : IShipPathGraph`; waypoints → `(x, NavigationHeight, z)` exactly as `ShipPathGrid.TryFindPath`.

### Phase 3 — hazards
- [x] Shared hazard cost raster owned by the cache (one per hazard set), used by `IsUnpenalizedSegment`/`PreservesTravelCost`.
- [x] Hazard boundary samples inserted into the point set so triangles do not straddle hazard edges.

### Phase 4 — switch + tests
- [x] `ShipNavigationCache.GetGraph` creates `ShipNavMesh`. `ShipPathGrid` and its four jobs are Editor-only test oracles. Production shares only the raster coordinate/index operations used for hazard costs. Retire the Editor oracle after the parity acceptance run.
- [x] EditMode tests (`Navigation`): every waypoint segment clears all inflated disks; region connectivity ⊇ grid connectivity on random layouts; path length ≤ grid path + tolerance; escape-from-inside; unreachable click → nearest reachable on ship side; hazard detour preserved; Quasar Fire/Titan/Executor case (narrow gap, Titan-only obstacle at Y −270, contested destination).
- [x] Profiler markers: reuse `NavigationGridBuild`/`NavigationPathSearch`/`NavigationNearestReachable` names for comparable captures.
- [x] Tests not run locally (override); user runs `Navigation` category.

### Phase 5 — optional follow-up after measurement
- [ ] Log layers built per battle, triangles per layer, bytes per layer.
- [ ] If layers or memory are high: radius-aware search on one mesh per height band (Demyen TA*-style edge width + radius funnel), or conservative clearance buckets (rounding up shrinks gaps — needs user decision).

## Important Values
- Disk samples: `n = max(8, ceil(2π·R / cellSize))` (uncapped since 2026-11-06); cell ≥ map/160 bounds `n ≤ 2π·160·R/mapSize`.
- Circumscribed radius `R/cos(π/n)`: n=16 → +2.0%, n=32 → +0.5%.
- Grid baseline: 25,921 nodes/layer; mesh target: ≤ 2·points triangles.

## Edge Cases
- Ship starts inside clearance → nearest walkable triangle; route allowed to escape (matches `allowEscape`).
- Goal inside an obstacle or another region → nearest reachable point in origin region, then existing destination rings/reservations.
- Station obstacles disappear when a station dies → obstacle set changes → new layer (same as grid).
- Executor radius 1059 → most gaps close; must still find escape/nearest reachable.
- Huge disks cover the whole map → zero walkable triangles → stationary plan, no exception.
- Duplicate/near-duplicate sample points → dedupe before triangulation (BurstTriangulator rejects duplicates).

## Files
- `Assets/Scripts/Services/ShipNavigation/{ShipPathGrid,ShipNavigationCache,ShipNavigationService,ShipRoutePlanner,NavigationRouteJob,NavigationTravelCost}.cs`
- New: `IShipPathGraph`, `ShipNavMesh`, `NavMesh*Job`, `NavMeshLocator` in the same folder.
- Tests: `Assets/Scripts/Tests/Editor/{ShipNavigationServiceTests,NavigationOptimizationParityTests,NavigationGridCellsTests}.cs`

## TODO
- Unmeasured: real obstacle counts, triangles and persistent bytes per clearance layer, layer build time and frame costs. Record with a representative mixed battle before deciding on Phase 5.
- Research: [[Done/Optimization/Graph_Pathfinding_Plan - Research|Graph Pathfinding Research]]
