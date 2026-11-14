---
category: Bugs
status: done
created: 2026-11-14
completed: 2026-11-14
acceptance: pending-user
---
# Reinforcement Spawn Near Station Fix

## Goal
- Allow reinforcement placement and movement above/below stations when the actual hulls clear vertically.
- Show selected-ship clearance and reachability in the spawn grid; sample ship previews and drops at the same cell centres.

## Decision
- Use the live station collider via `IStationHullFacade`, not a baked maximum height.
  - Why: `StationLevelView.ApplyLevel` changes collider bounds on upgrades.
  - Do not shrink real physics colliders to admit overlapping ships.
- 2026-11-14 follow-up (user-approved): navigation disk radius = live hull box planar half-diagonal + centre offset (`IStationHullFacade.GetHullRadius`), not the 396/261 u map keep-out.
  - Why: verification showed the old 396 u ring still blocked HighMid ships at every station and 61/68 ships at Rebellion stations.
  - `INavigationFootprintFacade.NavigationRadius` keeps the map radius (AI defense rings, engagement stand-off, guard spacing unchanged).
- `BattleMapLoader` creates owner/position descriptors. `MapObstacleContactProvider` supplies current radius/bottom/top.
- Share placement validity with the overlay. Batch navigation through one parallel Burst job; restrict physics predicates to collider/reservation candidate bounds.

## Implementation
- [x] Station spans use the current world-space hull collider, including level changes.
- [x] All four faction station prefabs inspected through Unity: one enabled, non-trigger root BoxCollider; explicit StationLevelView binding; no extra selection collider.
- [x] Station origin retains navigation's existing escape-leg behavior. Ships clearing station height filter the disk out; overlapping ships keep it.
- [x] Dead main stations drop their map obstacle, including when the registry falls back to a surviving second station.
- [x] Second stations/defense platforms have no separate radius-as-height StationObstacle registration; their colliders still participate in physics clearance.
- [x] Overlay input carries the canonical UnitRequest and placement predicate; structures use that predicate in collision candidates, squadrons retain existing rules.
- [x] Ship physics candidates include rotated/offset hull bounds, all solid colliders at hull height, map-edge colliders and landing reservations.
- [x] Bulk reachability shares graph preparation and locator logic, preserving disconnected-region and exit-leg checks; IgnoresMapObstacles fills the mask open.
- [x] Ship preview/drop positions snap to grid cell centres. Batch spacing adds one maximum cell width so rounding cannot shrink the original minimum separation.
- [x] Ship previews use placement validity alone; legacy trigger veto remains for structures/squadrons.
- [x] Navigation, Map and Economy regressions authored; interface fakes updated in Economy/EnemyAi/BattleInput coverage.
- [x] Unity script import/compilation completed with 0 errors; new script metadata imported. Source diagnostics and scoped `git diff --check` passed.
- [x] Reusable rules extracted to [[GameDesign/Capture_And_Structures#Reinforcement placement grid]].

## Important Values
- Previous StationObstacle span = position.y ± planar radius (396 u for three factions, 261 u for the fourth), creating a much taller obstacle than the actual hull.
- Station Y = -40 u. Inspected level-art world spans:
  - Republic: [-115.986, 35.986] u.
  - Empire: bottom -111.833 to -145.887 u; top 31.833 u.
  - Rebellion: bottom -229.731 u; top 149.731 u (levels 1–3), 218.511 u (levels 4–5).
  - Separatist: bottom approximately -146.768 u; top 66.768 to 75.776 u.
- Hull nav radius (root BoxCollider, half-diagonal): Empire ≈ 62 u, Republic ≈ 110 u, Separatist ≈ 124 u, Rebellion ≈ 117 u (was 396/261 u); ship nav clearance is added on top.
- Ships clearing station height (hull wholly above/below collider): Empire 42/68, Republic 41/68, Separatist 28/68, Rebellion 7/68. The rest now meet the hull-sized disk, not the 396 u ring.
- Default overlay resolution = 256 × 256; cell width = map extent / 256; snap displacement ≤ half a cell on each axis.
- Dynamic physics/vision/blocker redraw = existing 0.1 s default; reachability refresh = Show + every 1 s.
- Profiling markers: `Battle.SpawnArea.Redraw`, `Battle.SpawnArea.Reachability`.

## Edge Cases
- A station hull genuinely reaching a ship's altitude still blocks it; removing the old oversized span does not remove real overlap.
- Dynamic movement/reservations may take one redraw interval to change grid colour; station/topology changes may take up to 1 s. Preview and authority recheck the live rule before deployment.
- Out-of-map cursor positions remain invalid; snapping never clamps them onto a valid edge cell.
- Large-map runtime budget is unmeasured. The active Editor was at MainMenuScene outside Play Mode; no runtime performance claim.
- SpawnAreaOverlay now exceeds 200 lines. A focused raster-state extraction is a possible follow-up; no unrelated view refactor was included.

## TODO
- User in-game acceptance: small/large ships near, above and below all station levels; preview/grid parity after refresh; batch drops, main-station destruction and landing reservations.
- 2026-11-14: user reports FPS collapse during ship placement → [[Done/Optimization/Spawn_Grid_Overlay_Performance_Plan]].
- Large-map profile: compare steady redraw and 1 s reachability refresh against [[Rules/BATTLE_PERFORMANCE_GUIDELINES]], recording source/workload and frame-time distribution.
- Tests were not run under the local override. Run authored Navigation/Map/Economy regressions and affected BattleInput/EnemyAi compatibility checks only on explicit request.
- Gates transferred to [[TODOs/User_Acceptance_Queue]]; implementation is complete.

## Files
- `Assets/Scripts/Components/Obstacles/StationObstacle.cs`; `Services/ShipNavigation/MapObstacleContactProvider.cs`.
- `Assets/Scripts/Entities/SpaceStation/{IStationHullFacade,StationFootprintFacade,SpaceStation,SpaceStationInstaller}.cs`; `Components/ViewComponents/Station/StationLevelView.cs`.
- `Assets/Scripts/Entities/SpawnArea/{ISpawnAreaOverlay,SpawnAreaOverlay,SpawnAreaGridModel}.cs`.
- `Assets/Scripts/Services/ShipSpawning/{ShipSpawnClearance,ShipHullFootprint,IShipSpawnReachability,ShipSpawnReachability}.cs`.
- `Assets/Scripts/Services/ShipNavigation/{ShipNavigationService,IShipPathGraph,ShipNavMesh,NavMeshData,NavMeshLocator,NavMeshReachabilityJob}.cs`.
- `Assets/Scripts/Services/Reinforcement/{ReinforcementService,ShipReinforcementPlacement}.cs`; `Entities/Reinforcement/UnitSpawnView.cs`.
- Regression fixtures: `StationAreaClearanceTests`, `ShipNavigationServiceTests`, `ShipNavMeshTests`, `ShipSpawnClearanceGridTests`, `ShipHullFootprintTests`, `SpawnAreaGridModelTests`, `UnitSpawnViewTests`.
