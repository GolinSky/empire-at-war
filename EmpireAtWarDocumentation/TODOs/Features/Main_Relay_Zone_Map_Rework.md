---
category: Features
status: todo
created: 2026-10-29
---
# Main Relay Zone Map Rework

## Goal

- One contested **main relay zone** near the map center with 4 mines + 6 weapon satellites → everyone fights to take, hold and fortify it.
- Station home economy shrinks to 1 mine + 1 satellite.
- Additional relay zones (mine + 2 satellites each) split per map half; count scales with map size.

## Current State (verified 2026-10-29)

- `PointOfInterestPlacer.TryPlace` order: default zones → home mining → optional central `CapturableZone` (odd `CapturableZoneCount`) → `CentralMiningPairCount` mining pairs in `CentralRegionShare` box → capturable zone pairs → remaining mining pairs → battle site pairs.
- Home mining (`TryAddHomeMining`): per station a triangle of `SITES_PER_MINING_POINT` = `3` mines + rotated triangle of 3 `SatelliteSite`s, behind the station toward its corner.
- Contested mining points (`TryAddMiningPairs`): 3 mines + 3 satellites per point, mirrored pair; mines spaced at `MINING_SITE_SPACING_SHARE` `0.25` → tight cluster.
- Everything is point-symmetric (`MapNode.Mirror`, `PlanarGeometry.Reflect`).
- `MapGenerationSettings.asset` per size:

| Size | Bounds | `CapturableZoneCount` | `MiningPointCount` | `CentralMiningPairCount` | `BattleSiteCount` |
|---|---|---|---|---|---|
| Small | ±2000 | 2 | 6 | 1 | 2 |
| Medium | ±3000 | 3 | 8 | 1 | 2 |
| Large | ±4500 | 9 | 12 | 2 | 4 |

- `CentralRegionShare` = `0.15`.
- Relay mesh: `Assets/Prefabs/View/ReinforcementZones/ReinforcementZone.prefab` → child `RelayModel` `m_LocalScale` `500`. Capture sphere `SphereRenderer` `540` (unchanged).
- `MapLayoutView` spawns relays from `ZoneSpot` (`zonePrefab`) and sites from `SiteSpot` (mining/battle/satellite prefabs).
- `BattleSaveValidator.ValidateMapCounts` expects `size.CapturableZoneCount` reinforcement zones on Full maps.
- Supersedes the home/central layout of [[Done/Features/More_Mining_Sites|More Mining Sites]] (its remaining item is only an in-game check).

## Target Layout

| Element | Count | Placement |
|---|---|---|
| Home mine | 1 / station | near station (replaces 3-mine triangle) |
| Home satellite | 1 / station | near station, beside the home mine |
| Main relay | 1 | random point inside central region (half map width × half map height = ±¼ side around center) |
| Main mines | 4 | on the main relay edge, 90° apart → not clustered (user confirmed 4, 2026-10-29) |
| Main satellites | 6 | outside the capture radius, ring around its edge, 60° apart (user confirmed 2026-10-29) |
| Main battle sites | 4 | on the main relay edge, 45° between the mines (proposed placement) |
| Default relay | 1 / station | beside station, owned from start (kept, user 2026-10-29) |
| Additional relay | 2 / 4 / 6 (S/M/L) | mirrored pairs; one per side's half; far from the main zone |
| Additional mine | 1 / additional relay | at its relay |
| Additional satellites | 2 / additional mine | around that mine |
| Additional battle site | 1 / additional relay | at its relay, opposite the mine (proposed placement) |
| Free battle site pairs | 0 | removed (user 2026-10-29) |

- Totals for a duel: Small 8 mines / 12 satellites / 6 battle sites / 3 relays; Medium 10 / 16 / 8 / 5; Large 12 / 20 / 10 / 7 (+ 2 default relays).
- Central region for random main relay: Small ±1000, Medium ±1500, Large ±2250.

## Generation Order

1. `StationPlacer.Place` (corners; unchanged).
2. **Main zone first**: pick main relay position in central region → 4 edge mines → 4 battle sites → 6 satellites. Validate each with `IsClear`.
3. Level design around it: default zones, home mine + satellite.
4. Additional zones: mirrored pairs via best-candidate picker, rejecting candidates within `AdditionalZoneMainDistance` of the main relay.
5. Lanes → planet → hazards → asteroid fields → decor (unchanged). No free-standing battle site pairs.

## Decision (proposed — confirm before implementing)

- `MapSizeSettings`: replace `CapturableZoneCount`, `MiningPointCount`, `CentralMiningPairCount`, `BattleSiteCount` with `AdditionalZonePairCount` (`1`/`2`/`3`).
  - Why: one source of truth; relay/mine/satellite counts all derive from it.
  - Avoid: three counts that must agree with each other and the validator.
- `MapGenerationSettings`: add `MainZoneRegionShare` (`0.25` = half width/height), `MainZoneMiningCount` (`4`), `MainZoneSatelliteCount` (`6`), `MainZoneBattleSiteCount` (`4`), `AdditionalZoneSatelliteCount` (`2`), `AdditionalZoneBattleSiteCount` (`1`), `MainZoneFairnessTolerance` (world units).
- `MapSizeSettings`: add `AdditionalZoneMainDistance` (world units, per size; user OK 2026-10-29).
  - Replace `CentralRegionShare` (`0.15`) with `MainZoneRegionShare`.
- New `MapNodeKind.MainZone`? → No: main relay stays `CapturableZone`; mines/satellites get its `PointId`. Add only if view/AI needs to tell it apart.
- Additional zone = `CapturableZone` node + `MiningSite` + `BattleSite` + 2 `SatelliteSite` sharing one `PointId`; mirrored as a unit (reuse `TryAddMiningPairs` shape).
- Default relays (`DefaultZone` beside each station) stay unchanged.
- Battle sites only exist inside main/additional zones; remove the `TryAddPairs(MapNodeKind.BattleSite, …)` step.
- Main relay mesh: `RelayModel` scale `500` → `250`. Capture radius (`SphereRenderer`, `MapFeatureRadii.Zone`) unchanged.
- Random main position breaks point symmetry → `Mirror` = own index for main nodes (same rule as the old central zone). Fairness is best-effort (user 2026-10-29: "try, not mandatory"): prefer candidates where nearest-station distances per team differ ≤ `MainZoneFairnessTolerance`; if none found within the attempt budget, accept any valid spot.

## Implementation

1. `MapSizeSettings.cs` / `MapGenerationSettings.cs`: fields above; `FormerlySerializedAs` only where a field is renamed with the same meaning.
2. `MapGenerationSettings.asset`: S/M/L `AdditionalZonePairCount` `1`/`2`/`3`; new globals. Persist per `AGENTS.md` (Refresh → `ForceReserializeAssets` on this path → `SaveAssets` → console).
3. `PointOfInterestPlacer`:
   - New `TryAddMainZone` first in `TryPlace`.
   - `TryAddHomeMining`: 1 mine + 1 satellite per station; keep `PairWithMirroredStations`.
   - New/adapted `TryAddAdditionalZones(pairCount)`; `TryPickPairPosition` gets main-distance rejection.
   - Remove odd-count central zone branch, central mining pairs and free battle site pairs.
   - Split class if it grows past 200 lines (e.g. `MainZonePlacer`, `HomeMiningPlacer`).
4. `MapLayoutGenerator.CreateZones` / `CreateSites`: no change expected (kinds unchanged).
5. `ReinforcementZone.prefab` `RelayModel` scale → `250` via `PrefabUtility.LoadPrefabContents` / `SaveAsPrefabAsset`; verify capture/selection colliders still sensible.
6. `BattleSaveValidator.ValidateMapCounts`: expected zones = `1 + 2 × AdditionalZonePairCount` on Full maps.
7. Review consumers of `CapturableZoneCount` / `MiningPointCount` / `CentralMiningPairCount` / `BattleSiteCount` (Serena references): `SurvivalLayoutGenerator`, `EmptyLayoutGenerator`, AI (`EnemyStructurePlacementService`, `EnemyStrategicDecisionModel`), tests.
8. Update `GameDesign` map/economy notes after acceptance.

## Tests (EditMode; categories `Map`, `MapGeneration` + `MapExhaustive` explicit, `GameFlow` for validator, `Capture` for prefab)

- Main relay center inside central region (±`MainZoneRegionShare` × side).
- Exactly 4 mines + 4 battle sites + 6 satellites share the main `PointId`; satellites outside the capture radius; each main mine at ≈ zone-edge distance; neighbouring mine distance ≈ 90° edge chord (no clustering).
- Per station: exactly 1 home mine + 1 home satellite + 1 default relay.
- Additional zones: count `2 × AdditionalZonePairCount`; each has 1 mine + 1 battle site + 2 satellites; no battle site outside main/additional zones; each ≥ `AdditionalZoneMainDistance` from main relay; one of each pair per map half (nearer to its station).
- Existing: all sizes × difficulties × seeds generate; lanes passable; every POI connected.
- 3–4 player layouts still generate.
- Local test execution is disabled for agents → run only on explicit user request.

## Edge Cases

- Main relay random offset toward one corner → one side closer → fairness preference, then fallback to any valid spot (never fail generation for fairness).
- Main zone near a hazard/planet → hazards are placed after nodes and avoid them; verify.
- Main satellites ring + pocket may block lanes into the zone → keep `PocketEntranceWidth` entrances; `Fields_LeaveLanesPassable…` must pass.
- Small map ±2000: main region ±1000 plus additional zones far from main may leave little room → retries up to `MAX_LAYOUT_ATTEMPTS` `60`; tune `AdditionalZoneMainDistance` before dropping content.
- 3–4 players: "player half / enemy half" = team halves; additional pairs mirror through center, so each team side gets one per pair.
- Old saves → zone/site counts mismatch → rejected by validator.
- Economy drop at home (3 → 1 mine) → AI must expand earlier; review AI mining targets.
- Halving relay mesh does not shrink capture radius; may look detached from the sphere → visual check.

## Open Questions

- None. Battle site exact placement inside zones (45° between main mines; opposite the mine in additional zones) is proposed → confirm in-game.

## Answered (2026-10-29)

- Free battle site pairs: removed → 4 in main zone, 1 per additional zone.
- Default relay beside each station: kept.
- Main zone mines: 4.
- Main relay fairness: try, not mandatory.
- Main satellites: outside capture radius, around the edge.
- Additional zone min distance: per map size.
- Survival (`SurvivalLayoutGenerator`): unchanged.

## Files

- `Assets/Scripts/Entities/Map/Generation/PointOfInterestPlacer.cs`
- `Assets/Scripts/Entities/Map/Generation/MapLayoutGenerator.cs`
- `Assets/Scripts/Entities/Map/MapSizeSettings.cs`
- `Assets/Scripts/Entities/Map/MapGenerationSettings.cs`
- `Assets/Settings/Data/Models/Map/MapGenerationSettings.asset`
- `Assets/Prefabs/View/ReinforcementZones/ReinforcementZone.prefab` (`RelayModel`)
- `Assets/Scripts/Services/Saves/BattleSaveValidator.cs`
- `Assets/Scripts/Tests/Editor/MapLayoutGeneratorTests.cs`

## TODO

- [x] User answers open questions (2026-10-29)
- [ ] Settings fields + asset values
- [ ] Main zone placement (first)
- [ ] Home: 1 mine + 1 satellite
- [ ] Additional zones (S2/M4/L6) away from main
- [ ] Relay mesh scale 500 → 250
- [ ] Save validator rule
- [ ] Consumer review (AI, Survival, Empty layout)
- [ ] EditMode tests updated (run on user request)
- [ ] User in-game check all sizes, 2p/4p
