---
category: Optimization
status: done
created: 2026-11-14
completed: 2026-11-14
acceptance: pending-user
---
# Spawn Grid Overlay Performance Plan

## Goal
- Restore battle FPS while ship reinforcement placement is open, after [[Done/Bugs/Reinforcement_Spawn_Near_Station_Fix]].
- Keep preview/drop/authority validity identical; only the overlay's raster work changes.

## Symptom
- User report 2026-11-14: fix works, but FPS collapses while placing ships.
- Scope: all hot paths below run only while `SpawnAreaOverlay` is enabled (`Show` → `Hide`). FPS still low after cancelling placement → different cause; reopen investigation.

## Verified Causes (source, 2026-11-14, commit `a434704f4`)
No profiler capture yet; ranking is from source cost, not frame-time attribution.

1. **Full spawn rule per candidate cell, 10 Hz** — `SpawnAreaOverlay.Redraw` → `ShipSpawnClearance.BlockObstructedCells(..., _isObstructed)`; `_isObstructed` = `!ShipReinforcementPlacement.IsPositionValid` = `ReinforcementSpawnRule.CanSpawnShip`.
   - Per open candidate cell: `IsOpen` (vision, blockers, hazards — already in grid/hazard mask) → `Physics.CheckBox` + landing loop → `ShipSpawnReachability.IsReachable`.
   - `IsReachable` → `ShipNavigationService.IsReachable` → `BuildNavigationContacts` (`CopyContacts` O(contacts) + `RemoveAll` lambda capturing `hullSpan` → **closure + delegate GC allocation per cell**) → `ShipNavigationCache.GetGraph` (`Matches` O(graphs × contacts)) → `FindReachableGoal` (O(contacts) disk test + locator + `CanConnect`).
   - Redundant: the bulk reachability mask was already applied by `BlockUnreachable` before this pass.
2. **Repeated checks on valid cells** — `BlockWhere` re-evaluates every still-`OPEN` cell inside each obstacle's hull-expanded bounds.
   - `OverlapBoxNonAlloc` spans the whole map with `DefaultRaycastLayers`: every solid collider (stations, structures, ships, map edges) plus each landing reservation adds a region.
   - Valid cells in overlapping regions are checked once per region. Large hulls (Executor class) expand every region.
   - `GetFootprint` re-runs the hull dictionary lookup and `IStationFacingService.GetRotation` per cell.
3. **1 Hz synchronous reachability spike** — `ShipNavMesh.FillReachability` schedules `NavMeshReachabilityJob` over 256 × 256 = 65,536 cells and calls `.Complete()` immediately.
   - Per cell: linear scan of all obstacle disks; `NavMeshLocator.Locate` falls back to scanning every triangle when the bucket lookup misses.
   - Graph itself is cached (`ShipNavigationCache`); this is not a re-triangulation each second. Likely a periodic hitch rather than the steady drop.
4. **Per-frame preview validation (secondary)** — `ReinforcementService.LateTick` calls `IsPositionValid` per preview every frame; deploy-all multiplies it. Each call pays cause 1's per-call navigation cost and allocation.

## Decision
- Overlay uses a clearance-only predicate for ships; vision, blockers, hazards and reachability come from the masks it already builds.
  - Why: removes navigation from the per-cell loop entirely.
  - Previews, drop and authority `Deploy` keep full `CanSpawnShip` (single source of truth for spawning).
  - Avoid: changing `ReinforcementSpawnRule` semantics or caching its answers.
- 2026-11-14 user instruction: implement all four phases now; Phases 3–4 no longer conditional.

## Implementation
### Phase 1 — Clearance-only overlay predicate
- [x] `SpawnAreaOverlay` calls clearance-owned `BlockObstructedCells` without the full-rule callback. Grid and authoritative `IsClear` share the footprint clearance implementation; structures keep `_isPositionValid`.
- [x] Keep `Show(team, request, isPositionValid)` contract for structures; ship branch needs no navigation calls.
- [x] Remove the `RemoveAll` closure in `ShipNavigationService.BuildNavigationContacts` (stable forward compaction + one `RemoveRange`, O(contacts), no captured delegate). Benefits previews (cause 4) and AI/ship callers too.

### Phase 2 — One check per cell per redraw
- [x] `ShipSpawnClearance.BlockObstructedCells`: collect candidate cells into a reusable per-redraw mask (or generation-stamped `int[]`) from all collider/landing regions, then test each `OPEN` candidate once.
- [x] Snapshot hull + station rotation once per redraw; per-cell footprint = position + precomputed offset.
- Keep friendly moving-ship colliders: excluding them would change gameplay validity and is outside the four approved optimizations.

### Phase 3 — Reachability cache
- [x] `TryFillReachability` reuses the completed mask for graph instance + captured planar station origin + immutable cell-array identity. Graph matching incorporates clearance, height-filtered contacts and map/hazard identity; changed topology selects a different graph.
- [x] `Show` forces one fresh batch and clears the old selection's mask. Until completion cells remain blocked; `IgnoresMapObstacles` fills open immediately.

### Phase 4 — Async / spatial
- [x] Schedule once and poll `JobHandle.IsCompleted` on later 0.1 s redraws; call `Complete` before publishing. A pending mismatched/forced request is discarded before replacement; buffers are reused/resized only after completion. Graph disposal completes pending work before freeing native data.
- [x] Use existing disk buckets for in-map point checks in `NavMeshReachabilityJob` and scalar `IsInsideObstacle`. Preserve exact strict overlap math; outside-map queries retain a full scan because buckets omit fully external disks.

## Verification
- [x] Phases 1–2: Unity `6000.4.7f1` recompile completed 2026-11-14 with 0 errors.
- [x] All phases: scoped `git diff --check` and independent source review passed. Unity recompile reported no spawn/navigation errors, but full compilation is blocked by 3 unrelated `CS1501` errors in `ShipSelectionReuseTests.cs:43,49,53` (changed `ShipSelectionGroupUi.Configure` signature).
- [x] Navigation regressions updated/authored: offset/rotated hull and reservation clearance parity, one facing lookup/redraw, landing release, overlapping candidate deduplication, hidden/blocked-cell exclusion and candidate reset. Compiled; tests not run under local override.
- [x] Added async regressions for pending-output preservation, cache reuse, forced refresh, scalar origin changes, resized/replaced grids, pending disposal and disk-edge/outside-map parity; updated Economy/EnemyAi interface fakes. Tests not run.
- Full-rule composition source-reviewed: existing vision/blocker/reachability masks plus final hazard mask remain; preview/drop/authority rules unchanged. Integrated runtime parity remains unmeasured.
- User acceptance queue: `Battle.SpawnArea.Redraw` and `Battle.SpawnArea.Reachability` before/after, large map, Executor-class and small ship, deploy-all held. Record ms/frame and GC alloc per redraw against [[Rules/BATTLE_PERFORMANCE_GUIDELINES]].

## Edge Cases
- Dynamic clearance/vision redraw interval = 0.1 s. Navigation signature checks = 1 s; a changed mask adds worker completion + next redraw latency. Retain the last mask during same-selection refresh; preview/drop re-check live.
- `IgnoresMapObstacles` ships: reachability mask fills open; clearance predicate still applies physics.
- Hazard blocking stays in `_hazardMask` (combined at texture write); clearance predicate must not re-add it.
- Main-station destruction changes `GetStationPosition` → captured origin invalidates the mask. Shared graph movement-origin changes cannot relabel a pending job's captured origin/region.
- Initial graph construction/triangulation remains synchronous. Nearest-triangle fallback and nonzero exit-leg checks keep existing semantics; runtime cost remains unmeasured.

## Files
- `Assets/Scripts/Entities/SpawnArea/SpawnAreaOverlay.cs` (≈243 lines; exceeds 200 — raster-state extraction remains a separate suggestion).
- `Assets/Scripts/Services/ShipSpawning/{ShipSpawnClearance,IShipSpawnClearance,ShipSpawnReachability}.cs`.
- `Assets/Scripts/Services/ShipNavigation/{ShipNavigationService,ShipNavMesh,NavMeshReachabilityJob,NavMeshLocator}.cs`.
- `Assets/Scripts/Services/Reinforcement/{ReinforcementSpawnRule,ShipReinforcementPlacement,ReinforcementService}.cs`.

## Guidance
- Reusable rules → [[GameDesign/Capture_And_Structures#Reinforcement placement grid]] and [[Rules/BATTLE_PERFORMANCE_GUIDELINES#Ship pathfinding]].

## TODO
- All four phases implemented and source-reviewed 2026-11-14; no FPS improvement claim.
- In-game/profile checks, tests under the local override and full compilation after unrelated UI errors are resolved → [[TODOs/User_Acceptance_Queue]].
