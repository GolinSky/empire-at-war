---
category: Features
status: done
created: 2026-10-29
completed: 2026-10-29
acceptance: pending-user
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
| Default relay | 1 / station | beside station, owned from start (kept, user 2026-10-29) |
| Additional relay | 2 / 4 / 6 (S/M/L) | mirrored pairs; one per side's half; far from the main zone |
| Additional mine | 1 / additional relay | at its relay |
| Additional satellites | 2 / additional mine | around that mine |
- Rule: one asteroid capture structure = mining capture site. Capture → build Asteroid Mine **or** Battle Asteroid. Build options unchanged; only count + location of mining capture sites change.
- Duplicate today: `CaptureSiteKind.Battle` / `MapNodeKind.BattleSite` / `BattleAsteroidCaptureSite.prefab` is a second capture prefab with identical build options (`CaptureSiteData.allowedFacilities`). → Merge into the mining capture site: one capture prefab, two build options (user 2026-10-29).
- Totals for a duel: Small 8 mines / 12 satellites / 3 relays; Medium 10 / 16 / 5; Large 12 / 20 / 7 (+ 2 default relays).
- Central region for random main relay: Small ±1000, Medium ±1500, Large ±2250.

## Generation Order

1. `StationPlacer.Place` (corners; unchanged).
2. **Main zone first**: pick main relay position in central region → 4 edge mines → 6 satellites. Validate each with `IsClear`.
3. Level design around it: default zones, home mine + satellite.
4. Additional zones: mirrored pairs via best-candidate picker, rejecting candidates within `AdditionalZoneMainDistance` of the main relay.
5. Lanes → planet → hazards → asteroid fields → decor (unchanged). No free-standing battle site pairs.

## Decision (implemented 2026-10-29)

- `MapSizeSettings`: replace `CapturableZoneCount`, `MiningPointCount`, `CentralMiningPairCount`, `BattleSiteCount` with `AdditionalZonePairCount` (`1`/`2`/`3`).
  - Why: one source of truth; relay/mine/satellite counts all derive from it.
  - Avoid: three counts that must agree with each other and the validator.
- `MapGenerationSettings`: add `MainZoneRegionShare` (`0.25` = half width/height), `MainZoneMiningCount` (`4`), `MainZoneSatelliteCount` (`6`), `AdditionalZoneSatelliteCount` (`2`), `MainZoneFairnessTolerance` (world units).
- `MapSizeSettings`: add `AdditionalZoneMainDistance` (world units, per size; user OK 2026-10-29).
  - Replace `CentralRegionShare` (`0.15`) with `MainZoneRegionShare`.
- New `MapNodeKind.MainZone`? → No: main relay stays `CapturableZone`; mines/satellites get its `PointId`. Add only if view/AI needs to tell it apart.
- Additional zone = `CapturableZone` node + `MiningSite` + 2 `SatelliteSite` sharing one `PointId`; mirrored as a unit (reuse `TryAddMiningPairs` shape).
- Default relays (`DefaultZone` beside each station) stay unchanged.
- Merge the duplicate capture kind: remove `MapNodeKind.BattleSite`, `CaptureSiteKind.Battle`, `BattleAsteroidCaptureSite.prefab`, `MapLayoutView.battleSitePrefab` and the separate `TryAddPairs(MapNodeKind.BattleSite, …)` step; all asteroid capture sites use the mining capture prefab.
  - Why: one structure, one prefab; Battle Asteroid remains a build option on it.
  - Check before removing: enum values serialized in `CaptureSiteData.asset` (`siteSettings`, `allowedFacilities` keys), saves, AI, minimap, editor tools (`CaptureSiteEditorTool`, `BattleAsteroidAssetBuilder` → keep Battle Asteroid facility assets), `BattleMapLoader` site filter.
- Main relay mesh: `RelayModel` scale `500` → `250`. Capture radius (`SphereRenderer`, `MapFeatureRadii.Zone`) unchanged.
- Random main position breaks point symmetry → `Mirror` = own index for main nodes (same rule as the old central zone). Fairness is best-effort (user 2026-10-29: "try, not mandatory"): prefer candidates where nearest-station distances per team differ ≤ `MainZoneFairnessTolerance`; if none found within the attempt budget, accept any valid spot.

## Implementation

1. `MapSizeSettings.cs` / `MapGenerationSettings.cs`: fields above; `FormerlySerializedAs` only where a field is renamed with the same meaning.
2. `MapGenerationSettings.asset`: S/M/L `AdditionalZonePairCount` `1`/`2`/`3`; new globals. Persist per `AGENTS.md` (Refresh → `ForceReserializeAssets` on this path → `SaveAssets` → console).
3. `PointOfInterestPlacer`:
   - New `TryAddMainZone` first in `TryPlace`.
   - `TryAddHomeMining`: 1 mine + 1 satellite per station; keep `PairWithMirroredStations`.
   - New/adapted `TryAddAdditionalZones(pairCount)`; `TryPickPairPosition` gets main-distance rejection.
   - Remove odd-count central zone branch, central mining pairs and the separate `BattleSite` pair step.
   - Split class if it grows past 200 lines (e.g. `MainZonePlacer`, `HomeMiningPlacer`).
4. `MapLayoutGenerator.CreateZones` / `CreateSites`: no change expected (kinds unchanged).
5. `ReinforcementZone.prefab` `RelayModel` scale → `250` via `PrefabUtility.LoadPrefabContents` / `SaveAsPrefabAsset`; verify capture/selection colliders still sensible.
6. `BattleSaveValidator.ValidateMapCounts`: expected zones = `1 + 2 × AdditionalZonePairCount` on Full maps.
7. Review consumers of `CapturableZoneCount` / `MiningPointCount` / `CentralMiningPairCount` / `BattleSiteCount` (Serena references): `SurvivalLayoutGenerator`, `EmptyLayoutGenerator`, AI (`EnemyStructurePlacementService`, `EnemyStrategicDecisionModel`), tests.
8. Update `GameDesign` map/economy notes after acceptance.

## Tests (EditMode; categories `Map`, `MapGeneration` + `MapExhaustive` explicit, `GameFlow` for validator, `Capture` for prefab)

- Main relay center inside central region (±`MainZoneRegionShare` × side).
- Exactly 4 mines + 6 satellites share the main `PointId`; satellites outside the capture radius; each main mine at ≈ zone-edge distance; neighbouring mine distance ≈ 90° edge chord (no clustering).
- Per station: exactly 1 home mine + 1 home satellite + 1 default relay.
- Additional zones: count `2 × AdditionalZonePairCount`; each has 1 mining capture site + 2 satellites; only one asteroid capture kind exists; each ≥ `AdditionalZoneMainDistance` from main relay; one of each pair per map half (nearer to its station).
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

- None.

## Answered (2026-10-29)

- One asteroid capture structure (mining capture site) offering Asteroid Mine + Battle Asteroid; unchanged. Duplicate `Battle` capture kind/prefab merged into it.
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
- [x] Settings fields + asset values
- [x] Main zone placement (first)
- [x] Home: 1 mine + 1 satellite
- [x] Additional zones (S2/M4/L6) away from main
- [x] Relay mesh scale 500 → 250
- [x] Save validator rule
- [x] Merge duplicate `Battle` capture kind/prefab into the mining capture site
- [x] Consumer review (AI, Survival, Empty layout)
- [x] EditMode tests updated (run on user request)
- [x] User-only acceptance moved to [[TODOs/User_Acceptance_Queue]].

## Implementation Outcome

- Completed 2026-10-29: main-first relay group, 1 home mine + pad, S/M/L additional relay pairs 1/2/3; one asteroid capture kind with both facility options.
- `MainZoneRegionShare` = `0.25`; main mines/pads = `4`/`6`; additional pads = `2`; fairness tolerance = `300` world units (best effort).
- `AdditionalZoneMainDistance` S/M/L = `900`/`1400`/`2000` world units. `SurvivalMiningSiteCount` preserves `6`/`8`/`12` points and existing Survival generation.
- Home sites sit inward beside stations to keep their asteroid pockets inside the map. Main mining radius uses the current prefab footprint (`60 × 2 = 120` world units).
- Removed duplicate Battle prefab and settings keys; migrated four legacy Kamino/Coruscant prefab instances without changing their transforms. Battle Asteroid facility assets/build option retained.
- Relay mesh scale `250`; capture sphere scale and capture/selection colliders unchanged.
- AI consumers use live sites/relays and existing expansion/construction priorities; no count-dependent changes needed. Empty layout unchanged.
- Verification: source/reference review and Unity compilation/import; EditMode tests updated, not run under local override. In-game all-size 2p/4p acceptance and requested test runs are user-only gates.
- After user acceptance: update GameDesign map/economy reference notes.

## Verification Review (2026-10-29, source only)

- Commit `0da1f8e73` matches the plan: `MainZonePlacer` (first, region ±0.25 side, 4 mines on relay edge 90° apart, 6 pads outside capture radius, fairness tolerance 300 with fallback), home 1 mine + 1 pad per station, `AdditionalZonePlacer` (S/M/L 1/2/3 pairs, `AdditionalZoneMainDistance` 900/1400/2000, opposite team halves).
- Settings/asset values match; `CaptureSiteKind.Satellite` keeps explicit value `2` → `CaptureSiteData.asset` keys stay valid.
- Duplicate Battle kind removed: no remaining code/asset references to `CaptureSiteKind.Battle`, `MapNodeKind.BattleSite`, `battleSitePrefab` or prefab GUID `069e7efc…`. Battle Asteroid build option kept (`allowedFacilities` Mining → `0,1`).
- Validator Full: `2 × slots + 4 + 6 + 2 × pairs × 3` sites, `1 + 2 × pairs` zones → matches generator.
- Self-mirrored off-center main nodes are safe for `LaneNetworkBuilder` (`AddLane` skips `from == to`; lanes use real node centers).
- Additional relay + mine share one center → zero-length spanning lane; `DistanceToSegmentSquared` handles it.
- `BattleMapLoader` now drops all sites without `MiningSites`; no current preset has `CaptureSites` without `MiningSites` (All / None only).
- `CaptureSiteEditorTool.SITE_SCALE` 5 → 2 matches `CaptureSite.prefab` root scale `2`.
- `RelayGroups_HaveExpectedCountsGeometryAndMirrorsAcrossRosters` covers counts, geometry, mirrors, halves, home sites for 2–4 players.
- Not run: EditMode tests (local override), Play Mode. Gates remain in [[TODOs/User_Acceptance_Queue]].

## In-Game Review (2026-10-29)

- Gate failed on layout: main relay must be at exact center, additional zones at corners, no straight shortcuts, home sites behind station, main mines at half relay radius.
- Follow-up: [[Done/Features/Main_Relay_Map_Layout_Adjustments|Main Relay Map Layout Adjustments]]. In-game queue line moved there.
