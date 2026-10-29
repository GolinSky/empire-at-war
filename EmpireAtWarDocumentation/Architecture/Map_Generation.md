---
type: engineering-reference
system:
  - map-generation
verified: 2026-10-29
verification: Review fixes verified through live source and Unity compilation;
  no automated generation tests or Play Mode. Saved main relay prefab/settings
  from original implementation unchanged by review fixes.
---
# Map Generation

## Rules

- Full Skirmish uses `MapLayoutGenerator`; Survival and Empty layouts retain their separate generators.
- `MapFeatureRadii` reads ordinary/main relay radii from separate prefab references on `MapLayoutView`; never infer the main radius from the ordinary relay.
- Main relay: exact `PlanarGeometry.GetCenter(size.Bounds)`, range `540`; ordinary/default/additional range `270`.
- Main mines: 4 at `MainZone × MainZoneMiningRadiusShare` = `270`, 90° apart. Main satellites stay outside capture range; opposite ring nodes have real `Mirror` indices.
- Opposite stations share a mirrored random corner offset and the larger station footprint inset; each home mine/pad uses its own station radius + own footprint + `15` units.
- Outward jitter reserves `(pairedStationRadius + maxSiteRadius + 15) × cos(15°) + maxSiteRadius` from each border for the central behind orientation. With radius396/site120, maximum outward shift is ≈183 on Medium/Large; configured inward jitter remains240/360.
- Main mine/satellite ring counts must be even and ≥2; invalid values throw before an asymmetric ring is built.
- Home sites prefer the corner behind the station, then front only when behind candidates do not fit. `IsHomeSite` excludes asteroid pockets; bounds use footprint only. Field clearing includes one raster cell of spill margin.
- Additional relay groups: mirrored edge-band candidates, station exclusion, main-distance exclusion, opposite nearest-team halves. Group members share a `PointId`.
- Counts stay unchanged; changing generation changes old saved-seed geometry without triggering the count validator. Fresh Skirmish acceptance is required.

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
- `MainLineBlocker` reserves mirrored bowed fields: 7 overlapping circles, radius `MinLaneWidth / 6` = 120; transverse width `LaneWidth × 1.25` = 1250/1275/1250 (S/M/L). Standard area-based `RockScatter` populates their actual volumes; five-rock fallback removed.
- Existing fields/hazards satisfy blocking only with a continuous local cross-section at least `LaneWidth` wide across the intervening line. Distant wide blobs and tiny intersections cannot bypass the field.
- `LaneObstacleRouter` detours crossing lanes with `MinLaneWidth / 2` clearance (360; full 720-unit corridor); obstacle vertices inside other expanded volumes are discarded. Natural fields are generated after detours; broad fallback fields added only where needed.
- `HazardPlacer` first searches main→additional segments at 35–65% progress, with hazard long axis perpendicular to the segment. Existing mirrored clearance/count checks and original fallback remain.
- Endpoint relay asteroid pockets must not satisfy main↔additional shortcut blocking. Inspect the intervening segment and preserve the lane corridor around any inserted blocker.
- Mirrored station positions can coexist with different home-site positions when faction station radii differ; closeness to each station uses its own footprint.
- Tests are disabled under repository `AGENTS.md` unless explicitly requested. Compilation/source inspection does not establish all-seed generation success or gameplay acceptance.

## Files

- `Assets/Scripts/Entities/Map/Generation/` — placement, lane network, fields and main-line blocking.
- `Assets/Scripts/Entities/Map/{MapLayoutView,MapGenerationSettings,MapSizeSettings,ZoneSpot}.cs`.
- `Assets/Prefabs/View/ReinforcementZones/{ReinforcementZone,MainReinforcementZone}.prefab`.
- `Assets/Settings/Data/Models/Map/MapGenerationSettings.asset`.
- `Assets/Scripts/Tests/Editor/{MapLayoutGeneratorTests,SerializedReferenceTests}.cs`.
- [[Done/Features/Main_Relay_Map_Layout_Adjustments]] — implementation and acceptance owner.
