---
type: engineering-reference
system:
  - map-generation
verified: 2026-11-21
verification: "Satellite pad expansion: Unity compile 0 errors; scripted
  generation of 7 planets × every size/roster × seeds 0,7,19 (315 layouts)
  succeeded. EditMode tests not run (local override)."
---
# Map Generation

## Rules

- Full Skirmish passes `PlanetType` to `MapLayoutGenerator`; `PlanetMapProfile` selects the archetype. Missing profiles fail explicitly.
- Open Field with 2–4 players retains the legacy generator and same-seed geometry. Other archetypes and Open Field with 5–8 players use the archetype branch; see [[Architecture/Map_Archetypes]].
- Survival and Empty layouts retain their separate generators.
- `MapFeatureRadii` reads ordinary/main relay radii from separate prefab references on `MapLayoutView`; never infer the main radius from the ordinary relay.

### Legacy Open Field placement

- Main relay: exact `PlanarGeometry.GetCenter(size.Bounds)`, range `540`; ordinary/default/additional range `270`.
- Main mines: `MainZoneMiningCount` (4) at `MainZone × MainZoneMiningRadiusShare` = `270`. Main pads: `MainZoneSatelliteCount` (8) at `270 + 120 + 60 + 20` = `470`, inside the `540` capture range; phase `turn + 360 / (2 × count)` (`22.5°`) → nearest pad↔mine ≈`243`. Opposite ring nodes have real `Mirror` indices.
- Opposite stations share a mirrored random corner offset and the larger station footprint inset; each home mine/pad uses its own station radius + own footprint + `15` units.
- Outward jitter reserves `(pairedStationRadius + maxSiteRadius + 15) × cos(15°) + maxSiteRadius` from each border for the central behind orientation. With radius396/site120, maximum outward shift is ≈183 on Medium/Large; configured inward jitter remains240/360.
- Main mine/satellite ring counts must be even and ≥2; invalid values throw before an asymmetric ring is built.
- Home sites prefer the corner behind the station, then front only when behind candidates do not fit. `IsHomeSite` excludes asteroid pockets; bounds use footprint only. Field clearing includes one raster cell of spill margin.
- Home sites: 1 mine + `HomeSatelliteCount` (4) pads on the station ring `station.Radius + 60 + 15`. Pads fan from the old pad angle away from the mine in `max(20°, chord ≥ 2 × 60)` steps (Rebel radius `261` → ≈`21.6°`); first clear candidates win, else next side/orientation. All share `PointId` = station index, `IsHomeSite`.
- Additional relay groups: mirrored edge-band candidates, station exclusion, main-distance exclusion, opposite nearest-team halves. Group members share a `PointId`. Pads: `AdditionalZoneSatelliteCount` (4) evenly on a `200` ring → outer edge `260` < `270`.
- Site count (`BattleSaveValidator.ValidateMapCounts`): `Slots × (1 + HomeSatelliteCount) + MainMining + MainSatellite + 2 × pairs × (1 + AdditionalSatellite) + size.FreeSatelliteCount`. Count changes make old Full saves fail to load (accepted 2026-11-21).

### Free satellite pads

- `FreeSatellitePlacer`; count `MapSizeSettings.FreeSatelliteCount` = 2 / 6 / 10 (S/M/L); must be even (odd throws). Exact count or the layout attempt fails (`freePads=` in the failure message).
- Runs after lanes are final (`LaneObstacleRouter`, legacy `MainLineBlocker.TryReserve`), before `AsteroidFieldBuilder.Build` → fields, fills and decorations clear around the pads.
- Candidate: random lane, `25–75 %` progress, lateral offset ≤ `MinLaneWidth / 2 − 60 − ZoneClearance` (corridors pinch to `MinLaneWidth`, not `LaneWidth`). Best of 16 valid by distance to every node and station.
- Checks: `PointOfInterestPlacer.IsClear`, ≥ `AdditionalZoneStationDistance` from every station, reserved main-line walls, hazard `SignedDistance > radius + CellSize`, inside a lane corridor.
- Mirrored rosters (outer relays with a real `Mirror`) → point-mirrored pairs; else independent pads.
- Own RNG `hazardSeed ^ 0x46524545` → layout RNG unchanged. Unique `PointId` per pad after the last relay id; AI treats a free point as a mine-less relay point.
- Free pads are not lane endpoints; tests detect them as satellite points without a mine and check corridor containment instead.

### Battle area access

- Register each generated relay footprint and scaled capture site footprint in `BattleAreaLayer.StructureExclusion`. Free structures must clear every footprint, regardless of team. Construction on a captured site uses its own rules.
- Use `SpawnBlock` for neutral or enemy deployment blockers. Fog-disabled battles grant sight across the map. Hazards, footprints and physical clearance still apply at placement and commit.
- Capture sites publish `OrderSnap` circles in layout order. On map replacement, unregister the old transforms and rebuild presenters in the new layout order used by save indexes.
- Capture-disabled composition retains `CaptureSiteData` for generator radii. Capture-enabled modules contribute tooltip, survival, announcement and AI behavior where their player and battle features allow it.

## Important Values

| Setting | Small | Medium | Large |
|---|---:|---:|---:|
| Additional relay pairs | 1 | 2 | 3 |
| Main distance minimum | 900 | 1400 | 2000 |
| Edge band | 800 | 1100 | 1400 |
| Station distance minimum | 1100 | 1300 | 1500 |
| Station corner jitter | 180 | 240 | 360 |

## Edge Cases

- Mandatory main-prefab reference belongs in `SceneContext.prefab`; `SerializedReferenceTests` covers the binding. Main variant overrides radius to `540` and `SphereRenderer` X/Y scale to `1080`.
- Legacy Open Field only: `MainLineBlocker` reserves mirrored bowed fields: 7 overlapping circles, radius `MinLaneWidth / 6` = 120; transverse width `LaneWidth × 1.25` = 1250/1275/1250 (S/M/L). Standard area-based `RockScatter` populates their actual volumes; five-rock fallback removed.
- Existing fields/hazards satisfy blocking only with a continuous local cross-section at least `LaneWidth` wide across the intervening line. Distant wide blobs and tiny intersections cannot bypass the field.
- `LaneObstacleRouter` detours crossing lanes with `MinLaneWidth / 2` clearance (360; full 720-unit corridor); obstacle vertices inside other expanded volumes are discarded. Natural fields are generated after detours; broad fallback fields added only where needed.
- Legacy Open Field `HazardPlacer` first searches main→additional segments at 35–65% progress, with hazard long axis perpendicular to the segment. Existing mirrored clearance/count checks and original fallback remain.
- Endpoint relay asteroid pockets must not satisfy main↔additional shortcut blocking. Inspect the intervening segment and preserve the lane corridor around any inserted blocker.
- Mirrored station positions can coexist with different home-site positions when faction station radii differ; closeness to each station uses its own footprint.
- Tests are disabled under repository `AGENTS.md` unless explicitly requested. Compilation/source inspection does not establish all-seed generation success or gameplay acceptance.

## Files

- `Assets/Scripts/Entities/Map/Generation/` — placement, lane network, fields and main-line blocking.
- `Assets/Scripts/Entities/Map/{MapLayoutView,MapGenerationSettings,MapSizeSettings,ZoneSpot}.cs`.
- `Assets/Prefabs/View/ReinforcementZones/{ReinforcementZone,MainReinforcementZone}.prefab`.
- `Assets/Settings/Data/Models/Map/MapGenerationSettings.asset`.
- `Assets/Scripts/Tests/Editor/Map/MapLayoutGeneratorTests.cs` and `Assets/Scripts/Tests/Editor/Core/SerializedReferenceTests.cs`.
- [[Done/Features/Main_Relay_Map_Layout_Adjustments]] — implementation and acceptance owner.
