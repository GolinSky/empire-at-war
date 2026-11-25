---
category: Features
status: done
created: 2026-11-21
completed: 2026-11-21
acceptance: pending-user
---
# Map Satellite Pad Expansion

## Goal

- More satellite pads on Skirmish (Full) maps: near stations, in outer relay zones, in the main relay zone.
- New free pads on routes between zones, count by map size.
- Survival and Empty layouts unchanged.

## Target

| Place | Now | Target | Setting |
|---|---:|---:|---|
| Near each station (home) | 1 | 4 | new `MapGenerationSettings.HomeSatelliteCount` |
| Each outer relay zone | 2 | 4 | `AdditionalZoneSatelliteCount` |
| Main relay zone | 6 | 8 | `MainZoneSatelliteCount` |
| Free pads, Small / Medium / Large | 0 | 2 / 6 / 10 | new `MapSizeSettings.FreeSatelliteCount` |

## Current State (source, 2026-11-21)

- Radii: ordinary relay `270`, main relay `540`, mine footprint `120` (`CaptureSiteData` `60` × prefab scale), pad footprint `60`, `ZoneClearance` `20`, `HOME_SITE_GAP` `15`.
- Outer zone: `AdditionalZonePlacer.CreateGroup` → pads on a ring at `120 + 60 + 20` = `200`, evenly spaced.
- Main zone: `MainZonePlacer.CreateGroup` → mines at `270`, pads at `270 + 120 + 60 + 20` = `470`, phase `turn + 30°`; ring count must be even.
- Home: `PointOfInterestPlacer.TryAddHomeMining` → 1 mine at `−spread`, 1 pad at `+spread` (`30°` legacy, `60°` archetype); 2 sides × 3 orientations; `IsHomeSite`, `PointId` = station index.
- Site `PointId`: home = station index, main = `stations.Count`, outer = `stations.Count + 1 + n`.
- `BattleSaveValidator.ValidateMapCounts` expects `Slots × 2 + MainMining + MainSatellite + 2 × pairs × (1 + AdditionalSatellite)`.
- `EnemyDefenseSnapshotBuilder` groups sites by `PointId` (`_sitesByPoint`).
- [[Architecture/Map_Generation]] line "Main satellites stay outside capture range" is stale; source places them at `470`, inside `540`.

## Decision

### Relay ranges stay unchanged

- Outer zone, 4 pads at `200`: pad outer edge `260` < `270`; neighbour gap `283` ≥ `120`; pad↔mine `200` ≥ `180`. Fits.
- Main zone, 8 pads at `470`: pad outer edge `530` < `540`; neighbour gap `360` ≥ `120`. Fits.
- Main pad phase → `FULL_TURN / (2 × count)` (`22.5°`): pads sit symmetric between mines; nearest pad↔mine `243` ≥ `180`.
- Why: both rings already fit; a larger range would shift pockets, relay spacing and Small-map room.
- Avoid: growing `ReinforcementZone` / `MainReinforcementZone` prefab radius for this task.

### Home pads: 4 on the station ring

- Ring distance `station.Radius + 60 + 15`; mine stays where it is now.
- Candidate angles fan out from the current pad direction in `20°` steps (neighbour gap ≈`164` ≥ `120`), away from the mine.
- Accept the first `HomeSatelliteCount` candidates that pass `IsClear` + `StationAreaClearance.HasEnemyClearance`; fewer → try next side/orientation → layout attempt fails.
- All home pads keep `IsHomeSite = true`, `PointId` = station index.
- Border room: a pad at the border normal needs `R + 135`; `StationPlacer` already reserves ≈`R + 237` for the mine → no `StationPlacer` change expected. Verify in source during implementation.

### Free pads: lane corridors only

- New class `FreeSatellitePlacer` in `Entities/Map/Generation`; one responsibility: place `size.FreeSatelliteCount` pads.
- Candidates: random lane, `25–75 %` progress, lateral offset within `LaneWidth / 2 − 60 − ZoneClearance`.
- Pick best of N by largest distance to every other node and station → pads spread into the empty parts of the map.
- Why lanes only: fields are generated later and are impassable; lane corridors are the only space guaranteed open. A pad in off-lane open space can end up sealed in asteroids.
- Run after lanes are final (after `LaneObstacleRouter` and legacy `MainLineBlocker.TryReserve`), before `AsteroidFieldBuilder.Build` → fields, sealed-space fill and decorations clear around them.
- Checks: bounds, stations, `IsClear` vs nodes, central obstacle / structural walls, reserved main-line walls, hazard signed distance + one cell (same rule as `MapDecorationPlacer`), enemy station area.
- Mirrored layouts (outer relays have a real `Mirror` partner) → place point-mirrored pairs (2/6/10 are even). Unmirrored rosters → independent pads.
- Own `System.Random(hazardSeed ^ SALT)` → layout RNG, fields and hazards stay identical to a run without free pads.
- Each free pad gets a unique `PointId` after the last outer relay id. `−1` would merge all free pads into one AI defense point.
- Exact count is mandatory (save validator). Fewer placed → layout attempt fails; add `freePads=` to the failure message.
- `FreeSatelliteCount` must be even; throw on odd, like `MainZonePlacer.AddRing`.

### Saves

- Old Skirmish saves with capture sites fail `ValidateMapCounts` ("Saved N capture sites; the map has M") and do not load.
- Avoid: a legacy layout path for Full maps (Survival has one; Full would need the whole old generator).
- **Needs user confirmation** (see Open Questions).

## Implementation

1. `MapGenerationSettings`: add `HomeSatelliteCount` (`4`); `MapSizeSettings`: add `FreeSatelliteCount`. `ForArchetype` uses `MemberwiseClone` → copied automatically.
2. `MapGenerationSettings.asset`: `MainZoneSatelliteCount` `8`, `AdditionalZoneSatelliteCount` `4`, `HomeSatelliteCount` `4`, `FreeSatelliteCount` `2` / `6` / `10`. Persist per `AGENTS.md` (Refresh → `ForceReserializeAssets` on that path → `SaveAssets` → console).
3. `MainZonePlacer.CreateGroup`: pad phase `FULL_TURN / (2 × MainZoneSatelliteCount)`.
4. `PointOfInterestPlacer.TryAddHomeMining`: mine + `HomeSatelliteCount` pad fan; keep mirror pairing loop.
5. New `FreeSatellitePlacer`; call from `MapLayoutGenerator.GenerateLayout` between lane finalisation and field build; add failure counter.
6. `BattleSaveValidator.ValidateMapCounts`: `Slots × (1 + HomeSatelliteCount) + MainMining + MainSatellite + 2 × pairs × (1 + AdditionalSatellite) + size.FreeSatelliteCount`.
7. Review, change only if broken: `MainLineBlocker.Fits` (main sites need `MinLaneWidth` clearance; 8 pads widen the exclusion), `HazardPlacer` site clearance, `EnemySiteObjectives` / `EnemySiteConstructionController` / `EnemySquadronCaptureAssignments` (≈2.5× more pad targets), `CaptureSiteMiniMapPresenter`, tutorial hints (`HintTargets`).
8. Update [[Architecture/Map_Generation]]: new counts, free-pad rules, fix the stale "outside capture range" line.
9. Optional: regenerate 57 gallery previews with `MapArchetypePreviewBuilder`.

## Important Values

- Pads per Large 8-player map, 3 base pairs: home `32` + main `8` + outer `≥ 40` + free `10` ≈ `90`; before ≈ `36`. Each pad = view + presenter + minimap marker + `OrderSnap` circle + `StructureExclusion` footprint.
- Satellite capture range `500` (prefab) → the 4 home pads overlap and can be captured together.

## Tests (review + edit; run only on explicit user request)

- `MapArchetypeGenerationTests` line 79: sites per home `== 2` → `1 + settings.HomeSatelliteCount`.
- `MapLayoutGeneratorTests` line 633 and `BattleSaveServiceTests` line 623: count formula → new formula, values read from settings.
- `MapLayoutGeneratorTests` line 267: invalid-ring case still valid (odd count throws); add odd `FreeSatelliteCount` case.
- New assertions, no pinned counts: home pads within `station.Radius + footprint + gap`; outer / main pads inside their relay radius; free pads inside a lane corridor, outside fields and hazards, unique `PointId`, count == `size.FreeSatelliteCount`; mirrored partner exists when the layout is mirrored.
- Categories: `Map`, `MapGeneration` (explicit), `GameFlow`, `EnemyAi`, `Capture`. `MapExhaustive` only on separate request.

## Edge Cases

- Small 4-player: home ring + default relay + 4 pads per station is the tightest case → most likely source of `points=` failures.
- Legacy Small: more main pads → fewer valid `MainLineBlocker` wall positions → possible `TryReserve` retries.
- Different faction station radii → mirrored stations, non-mirrored home pads → node keeps `Mirror = i` (existing behaviour).
- Orbital Ring: main relay is offset from the centre → free-pad mirror partner must clear the central obstacle.
- Islands / Twin Pass: few lanes → free-pad candidates concentrate; best-of-N distance score spreads them.
- Same seed → different geometry than before; previews and saved seeds change.
- AI may spend more on satellites or over-commit squadrons to pads → balance check in-game. AI-side work: [[Done/Bugs/Enemy_AI_Opening_And_Task_Groups]] W22 (and W16c: one repair satellite per pad group, never on a free pad).

## Open Questions (resolved 2026-11-21)

- Old Skirmish saves stop loading after this change → accepted (recommended default, taken on the user's "execute" request; user to confirm in acceptance queue).
- Free pads restricted to lane corridors → accepted (same).

## Outcome (2026-11-21)

- Implemented as planned. Deviations:
  - Home pad step = `max(20°, 2·asin(60 / d) + 1°)`: Rebel station radius `261` gives a `117` chord at `20°` < `120`.
  - Free pad lateral offset uses `MinLaneWidth`, not `LaneWidth`: corridors pinch to `MinLaneWidth`.
  - Neutral free pads keep `AdditionalZoneStationDistance` from every station (no team to apply `HasEnemyClearance`).
- Review, no change needed: `MainLineBlocker.Fits`, `HazardPlacer`, `EnemyDefenseSnapshotBuilder` (free point → mine-less relay point), `HintTargets` (kind-based), `CaptureSitesSystem`.
- Verification: Unity compile `0` errors; asset reserialized (`4/8/4`, free `2/6/10`); scripted generation of 7 planets × every size/roster × seeds `0,7,19` = `315` layouts, `0` failures, max `112` sites. EditMode tests not run (local override).
- Tests updated: `MapLayoutGeneratorTests` (counts from settings, ring chord formulas, free-pad corridor in lane-connectivity test, odd `FreeSatelliteCount` case, 9 Open Field baseline hashes regenerated), `MapArchetypeGenerationTests` (home count, free-pad corridor/station/hazard/field/count checks), `BattleSaveServiceTests` (count formula).
- Reusable guidance → [[Architecture/Map_Generation]] (counts, home fan, free-pad rules, save count formula).
- Not done (optional): gallery preview regeneration (`MapArchetypePreviewBuilder`).

## Files

- `Assets/Scripts/Entities/Map/Generation/{MainZonePlacer,PointOfInterestPlacer,MapLayoutGenerator}.cs`, new `FreeSatellitePlacer.cs`
- `Assets/Scripts/Entities/Map/{MapGenerationSettings,MapSizeSettings}.cs`
- `Assets/Settings/Data/Models/Map/MapGenerationSettings.asset`
- `Assets/Scripts/Services/Saves/BattleSaveValidator.cs`
- `Assets/Scripts/Tests/Editor/Map/{MapLayoutGeneratorTests,MapArchetypeGenerationTests}.cs`, `Assets/Scripts/Tests/Editor/GameFlow/BattleSaveServiceTests.cs`

## TODO

- [x] Open questions → recommended defaults (user confirmation in acceptance queue)
- [x] Settings fields + asset values
- [x] Main zone 8 pads, outer zone 4 pads
- [x] Home pads 1 → 4
- [x] `FreeSatellitePlacer` + generator wiring
- [x] Save validator count formula
- [x] AI / blocker / hazard review
- [x] Tests updated
- [x] [[Architecture/Map_Generation]] updated
- [ ] In-game acceptance: pad placement, AI behaviour, performance on Large 8-player → [[TODOs/User_Acceptance_Queue]]
