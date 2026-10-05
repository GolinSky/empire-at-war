---
category: Features
status: done
created: 2026-10-05
completed: 2026-10-05
---
# Spawn Blockers and Relays

## Goal
- Reinforcements spawn anywhere the owner's team sees, unless a hostile spawn blocker covers the point.
- Reinforcement zones stop being spawn areas; they become capturable relays that block spawning.
- One rule for player, ally and AI.

## Rules
- `CanSpawn(team, pos, unit) = IsVisible(team, pos) && !IsBlocked(team, pos) && clear && map.Contains(pos)`
- `IsBlocked(team, pos)` → any blocker `B` with `!IsAllied(B.Owner, team)` and XZ distance ≤ `B.Radius`.
- `PlayerId.None` is never allied → neutral relays and asteroids block everyone.
- Station, defend platform, mining facility → block hostiles only.
- Captured relay → owner team may spawn in range; hostiles may not. Relay gives no vision.
- Squadrons use the same rule as ships (player placement). AI squadrons still launch from the station.
- Vision and spawn checks ignore height (XZ). Drawn fog keeps camera projection; spawn overlay is flat.

## Decision
- Two layers: `IVisionService` (per-unit vision) + `ISpawnBlockerService` (spawn-block circles).
  - Why: a revealed point may still be closed to deployment.
  - Avoid: spawn checks reading `FogOfWarSystem` (local-player only, camera-dependent, faded).
- Keep `ReinforcementZone*` class names; the zone view now represents a relay capture ring + blocker.
- Home (non-capturable) zones are no longer instantiated; their layout spots remain as home anchors (rally, AI exit, cheat spawn).

## Important Values (provisional, tune in play)
- Relay capture radius `270` (prefab `radius`), relay spawn block radius `900`.
- Station block `1200`; defend platform `500`; mining facility `400`.
- Asteroid block = obstacle XZ bounds radius + `60`.

## Implementation
- [x] Phase 0 — `IVisionService` (`Services/Vision/`); all units register vision for their owner; `FogOfWarSystem` draws only.
- [x] Phase 1 — `ISpawnBlockerService` + blocker sources (station, platform, mining, relay, asteroid obstacles) + tests.
  - Shared `OwnedCircleSet` (`Services/OwnedAreas/`) backs vision and blockers. `SpawnBlockRadius` on `EntityComponentData`; relay/asteroid values on `ReinforcementZoneData`.
  - EditMode 997: 961 pass / 36 fail. New `VisionServiceTests` + `SpawnBlockerServiceTests` pass. Failures pre-existing (prefab, wreck, tooltip, installer, AI production, `ShipSpawnPointsTests` `_radius`).
- [x] Phase 2 — `IReinforcementSpawnRule` (`IsOpen`, `CanSpawnShip`) used by player ship/squadron/structure placement and AI structure placement + tests.
  - `IsPositionInAlliedZone` removed. EditMode 1006: 968 pass / 38 fail; new failures only C-9979 wreck (parallel import) + random `MapLayoutGeneratorTests`.
- [x] Phase 3 — Zone → relay: placeholder relay mesh (`Relay` child: base/mast/dish, `UnitDefaultLit.mat`), home zones not spawned (`_homes` from `BattleMap.Layout`, `HomeAreaRadius 270`), AI ship spawn points + cheat spawn via spawn rule + tests.
  - AI ships: random open point within `RelaySpawnBlockRadius` of a team-held relay → else within `HomeAreaRadius` of home. EditMode 1008: 973 pass / 35 fail (no new).
- [x] Phase 4 — Spawn overlay: `SpawnAreaOverlay` (`Entities/SpawnArea/`) + `SpawnAreaGridModel` + `Custom/URP_SpawnArea` shader + `SpawnArea.mat`; `SceneContext.prefab` child `SpawnAreaOverlay`; `ReinforcementService` shows/hides it per placement.
  - Mask `r`: 0 hidden, 0.5 blocked (red), 1 open (green); 256² grid, redraw every 0.1 s while shown. EditMode 1013: 978 pass / 35 fail (no new).

## Files
- New: `Services/Vision/`, `Services/OwnedAreas/`, `Services/SpawnBlocking/`, `Entities/SpawnArea/`, `Components/FogOfWar/SpawnBlockerSource.cs`, `Services/Reinforcement/ReinforcementSpawnRule.cs`
- `Services/Reinforcement/*Placement*.cs`, `StructurePlacementArea.cs`, `ReinforcementService.cs`
- `Services/ReinforcementZones/ReinforcementZonesSystem.cs`, `Services/ShipSpawning/ShipSpawnPoints.cs`
- `Entities/Map/MapLayoutView.cs`, `Prefabs/View/ReinforcementZones/ReinforcementZone.prefab`, `Prefabs/View/ZenjectContext/SceneContext.prefab`

## Edge Cases
- Vision/blocker sources must unregister before their transform is destroyed (no auto-cleanup) → else `MissingReferenceException`.
- Station blocker + vision drop immediately on destruction, not after the death delay.
- `HuntState` / `SquadronTargetSelector`: only the human's units are fog-limited (unchanged), now by own-side vision.
- Overlay is raster (≈35 units/cell on the largest map); the rule check is exact.

- [x] Phase 5 — Structure collision unified: `IStructureSpawnClearance` / `StructureSpawnClearance` (radius from `DefendPlatformView` + `MiningFacilityView` colliders, `CheckSphere` on Unit+Obstacle); `IReinforcementSpawnRule.CanSpawnStructure` = open + outside relay rings/capture sites (with clearance) + clear. Player structure placements and `EnemyStructurePlacementService` both use it; `StructurePlacementArea` removed.
- [x] Phase 6 — Relay art: `Relay/RelayModel` uses unused `SpaceStationModular.fbx` + its 6 slot materials, ×5 (≈90 units wide) inside the 270 capture ring.
- [x] Phase 7 — Play Mode acceptance (Coruscant, Small, Human vs AI Medium):
  - Blockers live: stations 1200 (owners P0/P1), 2 neutral relays 900, 124 small + 9 large asteroid blockers, AI mining facilities 400.
  - Relays captured by AI ships → owner + blocker flipped to P1; human blocked there; AI open only where it has vision.
  - AI deployed ships and built mining through the rule; `TryGetRandomSpawnPosition` / home spawn 50/50 for both sides.
  - Overlay: green open area + red relay block circle; alpha lowered to open `0.05` / blocked `0.08` (0.18 was too bright in linear space).
  - Human visible ≈11.6% / open ≈9.0% of the small map at start.
  - No errors from this feature; exit showed pre-existing UI teardown exceptions (`EconomyUiController`, `CoreGameUiController` → see Scene Teardown bug).
- Final EditMode 1019: 982 pass / 37 fail; only new failures are `DispatcherShipView` wreck tests (parallel import).

## Notes
- Human home anchor (906 from station) sits outside station vision 900; home spawn still succeeds by sampling the visible part. Large maps (`DefaultZoneGap 360`) shrink that overlap — watch AI fallback if no relay is held.
- Radii remain provisional; tune with real matches.
