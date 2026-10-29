---
category: Features
status: done
created: 2026-10-29
completed: 2026-10-29
acceptance: pending-user
---
# Main Relay Map Layout Adjustments

Follow-up to [[Done/Features/Main_Relay_Zone_Map_Rework|Main Relay Zone Map Rework]] (commit `0da1f8e73`) after the user's in-game review on 2026-10-29.

## Goal

- Main relay at the exact map center with a larger range (main relay only); its 4 mines inside the range at half radius.
- Additional zones on map edges without players, far from the main relay, using space that is now wasted on asteroids.
- No straight line between the main relay and any additional zone; home mine + satellite close behind a randomly offset station.

## Current State Before Implementation (2026-10-29, source)

- `MainZonePlacer.TryPlace`: random point in ±`MainZoneRegionShare` (`0.25`) × side, fairness tolerance `300` with fallback; main nodes self-mirrored (`Mirror = i`).
- Relay range: `ReinforcementZoneView.radius` = `270` in `ReinforcementZone.prefab`; `SphereRenderer` scale `540` (= diameter). One prefab for every relay.
- `radius` drives: generator `MapFeatureRadii.Zone`, capture/spawn area (`ReinforcementZonesSystem` `ContainsCircle`, `GetCircleBounds`), minimap marker (`Radius × 2`).
- Main mines: ring at `Zone` (`270`, on the range edge); pads at `470`.
- `StationPlacer`: fixed corner position, `halfSize − (stationRadius + StationEdgeDistance 420)` per axis; no randomness.
- `TryAddHomeMining`: mine on the inward Z side (toward center), pad beside it on inward X. Why inward: behind-station room is `420` per axis and the mine's asteroid pocket (`240`/`410`/`410` S/M/L) must fit inside bounds.
- `AdditionalZonePlacer`: candidates anywhere in bounds; rejects `< AdditionalZoneMainDistance` (S/M/L `900`/`1400`/`2000`) and same-team halves. No edge preference.
- Fields (`FieldDensity`, threshold `0.56`) fill free space by noise, including map edges → wasted edge space. Hazards (`HazardPlacer`) ignore zone positions.
- Lanes: 3-point polyline, bend `±0.2` × length with random magnitude → near-straight lanes possible.

## Target

| Item | Now | Target |
|---|---|---|
| Main relay position | random in ±¼ side | exact map center |
| Main relay range | `270` (shared) | `540`, main relay only; other relays keep `270` |
| Main mines | 4 on range edge | 4 inside range at half radius (`270`), 90° apart |
| Main satellites | outside range | unchanged |
| Station position | fixed corner spot | random offset inside its corner area |
| Home mine + pad | inward, toward center | close to station, behind it (toward corner) preferred |
| Additional zones | anywhere ≥ min distance | free map edges, far from main |
| Edge asteroids | noise fill | less fill where zones now sit |
| Main ↔ additional | straight line possible | always blocked by asteroids / hazards |

## Decision (user-confirmed 2026-10-29)

- **Main relay at center**: `PlanarGeometry.GetCenter(size.Bounds)`; delete random sampling, `MainZoneFairnessTolerance`, `MainZoneRegionShare`. Rotate rings so point reflection maps mine→mine, pad→pad; set real `Mirror` pairs.
- **Main relay range only**: main-relay prefab variant (`radius` `540`, `SphereRenderer` `1080`) or per-spot radius in `ZoneSpot`; `MapFeatureRadii` gains `MainZone`. Default and additional relays stay `270`.
  - Mines at `270`: gap `√2 × 270` = `382` > footprint `240`; clearance to relay center `150`. Minimum workable range `362`.
- **Main mines at half radius**: `MainZoneMiningRadiusShare` = `0.5`.
- **Station position**: random offset inside its corner area (new `StationCornerJitter`, per size). Mirror the offset for the opposite station so both sides stay equal.
- **Home mine + pad**: placed close to the station (gap ≈ `HOME_SITE_GAP` `15`), behind it toward the corner (preferred; front allowed when behind does not fit). No asteroid pocket around home sites; bounds check uses footprint only.
- **Additional zones on free edges**:
  - Candidates in an edge band: distance to nearest border ≤ `AdditionalZoneEdgeBand` (per size), pocket inside bounds.
  - Excluded near stations: ≥ `AdditionalZoneStationDistance` (per size).
  - Keep ≥ `AdditionalZoneMainDistance` from main, mirrored pairs, one per team half.
  - Exact side depends on layout (e.g. middle-right / middle-left in a duel).
- **Use wasted edge space**: lower field fill in edge bands that now hold zones; keep fields as obstacles between zones, not as edge filler.
- **No straight line main ↔ additional** (only these pairs):
  - After fields/hazards, test the straight segment from the main relay to each additional relay.
  - If nothing blocks it, add an asteroid field (or bias a hazard) on that segment, outside every lane and pocket.
  - Lanes between them must bend enough to go around the blocker: minimum bend (`LaneBendMin`) or a 2-bend S-curve.

## Implementation

1. `MainZonePlacer`: center placement, mirrored rings, mines at `MainZone × 0.5`; remove fairness/region settings + asset fields.
2. Main relay prefab variant with `radius` `540` (Unity-aware: `PrefabUtility.LoadPrefabContents` / `SaveAsPrefabAsset`); `ZoneSpot`/`MapLayoutView` choose prefab for the main relay; `MapFeatureRadii.MainZone`.
3. `StationPlacer`: mirrored random corner offset (`StationCornerJitter`).
4. `PointOfInterestPlacer.TryAddHomeMining`: close, behind-first placement; no pocket for home sites (`MapNode` home flag).
5. `AdditionalZonePlacer`: edge-band candidates, station exclusion, existing min-distance/team-half/mirror rules; new `MapSizeSettings` fields `AdditionalZoneEdgeBand`, `AdditionalZoneStationDistance`.
6. `FieldDensity`: reduce edge fill around additional zones.
7. New `MainLineBlocker` (in `Entities/Map/Generation`): checks main ↔ additional segments, adds blocking field; `HazardPlacer` prefers those segments; `LaneNetworkBuilder` minimum bend / S-curve.
8. `BattleSaveValidator`: counts unchanged.
9. Persist `MapGenerationSettings.asset` and prefabs per `AGENTS.md` (Refresh → `ForceReserializeAssets` → `SaveAssets` → console).

## Tests (EditMode; `Map`, `MapGeneration` + `MapExhaustive` explicit; `Capture` for relay prefab) — run only on explicit user request

- Main relay center == map center; main nodes mirror through center; main range `540`, other relays `270`.
- Main mines at `270` ± tolerance, footprints not overlapping each other or the relay center.
- Stations stay in their corner area; opposite stations mirrored.
- Home mine + pad within station radius + footprint + small gap; behind unless behind does not fit.
- Additional zones: in edge band, away from stations, ≥ min distance from main, opposite team halves.
- Main → each additional relay: straight segment crosses a field or hazard; lanes passable; every zone reachable.
- All sizes × rosters (2–4 players) × difficulties generate.

## Edge Cases

- Main pocket grows (`540 + 20 + PocketThickness`) → less room on Small (±2000); tune edge band / min distance before dropping content.
- 4-player maps: every corner has a station → edge zones only between stations; Large (6 zones) may need narrower station exclusion.
- Station jitter + home sites must stay inside bounds and clear of default relay.
- Blocker must never cut a lane or pocket → otherwise a zone becomes unreachable; reject and retry.
- Home sites without pocket → station clearing must keep asteroids off them.
- Layout changes for the same seed → old saves rebuild a different map; counts unchanged, so the validator does not catch it.

## Open Questions

- None.

## Answered (2026-10-29)

- Main relay at exact map center; larger range for the main relay only.
- Main mines inside range at half radius.
- Additional zones: free map edges, far from main, not center, not player corners; use space now wasted on asteroids.
- Stations: random position inside their corner; home mine + satellite close to the station, behind preferred.
- Straight-line blocking only between the main relay and each additional zone, using asteroids / hazards.

## Files

- `Assets/Scripts/Entities/Map/Generation/MainZonePlacer.cs`
- `Assets/Scripts/Entities/Map/Generation/AdditionalZonePlacer.cs`
- `Assets/Scripts/Entities/Map/Generation/StationPlacer.cs`
- `Assets/Scripts/Entities/Map/Generation/PointOfInterestPlacer.cs`
- `Assets/Scripts/Entities/Map/Generation/LaneNetworkBuilder.cs`
- `Assets/Scripts/Entities/Map/Generation/FieldDensity.cs`, `AsteroidFieldBuilder.cs`, `HazardPlacer.cs`
- `Assets/Scripts/Entities/Map/MapLayoutView.cs`, `MapGenerationSettings.cs`, `MapSizeSettings.cs`
- `Assets/Settings/Data/Models/Map/MapGenerationSettings.asset`
- `Assets/Prefabs/View/ReinforcementZones/ReinforcementZone.prefab` + new main relay variant
- `Assets/Scripts/Tests/Editor/MapLayoutGeneratorTests.cs`

## TODO

- [x] User answers open questions (2026-10-29)
- [x] Main relay at exact center, mirrored rings
- [x] Main relay prefab variant (range `540`); main mines at half radius
- [x] Random station corner offset (mirrored)
- [x] Home mine + pad close behind station, no pocket
- [x] Additional zones on free map edges
- [x] Less edge asteroid fill near zones
- [x] Main ↔ additional straight-line blocker + lane bend
- [x] Settings + asset cleanup
- [x] EditMode tests updated (run on user request)
- [x] User-only in-game and test gates moved to [[TODOs/User_Acceptance_Queue]].

## Implementation Outcome

- Completed 2026-10-29: exact-center main relay; separate `MainReinforcementZone.prefab` variant (`radius` 540, `SphereRenderer` X/Y 1080), selected through `ZoneSpot.IsMain`. Ordinary/default/additional radii remain 270.
- Main mines at 270 (`MainZoneMiningRadiusShare` 0.5); main satellite ring remains outside capture range at 740. Ring nodes use real point-reflected `Mirror` pairs.
- Per-size `StationCornerJitter` S/M/L 180/240/360; opposite station centers mirror even for different faction footprints. Home sites use own station radius + site footprint + 15, behind-first; no home asteroid pocket.
- Per-size `AdditionalZoneEdgeBand` 800/1100/1400 and `AdditionalZoneStationDistance` 1100/1300/1500; existing main distances 900/1400/2000 and relay-pair counts 1/2/3 retained. Edge noise thresholds rise near additional relays.
- `MainLineBlocker` checks only main↔additional segments outside endpoint pockets. Mirrored reservations precede field generation; `LaneObstacleRouter` reroutes crossing lanes around reserved circles with full lane-width clearance. Existing hazards can satisfy blocking; asteroid fields are added only when the intervening line stays clear.
- Guaranteed blockers use radius = half the raster cell (45/60/60), independent of natural `MinFieldArea`. `LaneBendMin` 0.15 removes nearly straight random bends. Impossible reservation/routing retries the existing 60-attempt layout loop.
- Removed obsolete main-region and fairness fields. Save validator counts and Survival/Empty generation unchanged; previous saved seeds rebuild changed geometry.
- Verification: final Unity `recompile_status` completed, `failed=false`, errors empty; Console since the final compile has no new errors. Main prefab reported `Variant`, main/ordinary radii 540/270. Prefab/settings imported, only changed assets reserialized and saved; `git diff --check` passed.
- Updated `MapLayoutGeneratorTests` and required `mainZonePrefab` binding in `SerializedReferenceTests`. Affected categories: Map, MapGeneration, MapExhaustive, Capture and Core. Tests and Play Mode not run under the local override; no all-seed success claim.
- Reusable geometry, prefab-binding and shortcut-blocking rules extracted to [[Architecture/Map_Generation]].

## Verification Review Before Fixes (2026-10-29, source only, uncommitted working tree)

- Matches plan: center main relay + point-mirrored rings (`Mirror = first + (i + count/2) % count`); `MainReinforcementZone.prefab` variant overrides only `radius` 540 + `SphereRenderer` 1080, bound in `SceneContext.prefab`; mines at 270; mirrored station jitter; home sites behind-first, no pocket; edge-band additional zones with station exclusion; edge field thinning near additional relays; `LaneBendMin` 0.15; obsolete fields removed from code and asset.
- Finding: guaranteed blocker = one field volume of radius `FieldCellSize × 0.5` (45/60/60) with 5 medium rocks → 90–120 wide vs lanes 720–1000 wide. Straight line counts as blocked, but ships pass with a small detour; it does not hide additional zones.
- Deviation: `HazardPlacer` unchanged → no hazard bias toward main ↔ additional lines (only existing hazards count).
- Note: jitter up to `+StationCornerJitter` toward the border (Large 360 → 60 left of the 420 edge gap) → behind slot often fails and home sites fall back to the front for those rolls.
- Ring mirror formula assumes even `MainZoneMiningCount` / `MainZoneSatelliteCount` (4/6 now); odd values would mirror wrongly.
- Not run: EditMode tests (local override), Play Mode.

## TODO — Review Fixes

- [x] Meaningful lane-scale asteroid blocker and matching rocks; preserve passable detours.
- [x] Prefer hazards between main and additional relays.
- [x] Preserve behind-station room when sampling corner jitter.
- [x] Reject odd main ring counts explicitly.
- [x] Update regression coverage, source review and Unity compilation; no test execution.

## Review Fixes Outcome (2026-10-29)

- Reopened and fixed all four recorded findings; original completion above is historical.
- Replaced 90–120-wide/five-rock fallback with one connected bowed field per ray: 7 overlapping circles, radius `MinLaneWidth / 6` = 120; transverse width `LaneWidth × 1.25` = 1250/1275/1250 (Small/Medium/Large), depth ≈392–395 including bow. The center of the field is 240 thick.
- `CreateField` uses standard area-based `RockScatter`, all rock layers and the size's density. Raster cell size ≤60 keeps jittered rock positions inside the reserved volumes; mirrored fields copy actual volumes and rock positions.
- `IsBlocked` now checks a continuous local cross-section ≥`LaneWidth` across the intervening line: exact interval union for asteroid circles; half-hazard-cell transverse samples for hazards. Tiny clusters or a narrow filament connected to a distant wide blob no longer suppress the broad field.
- `LaneObstacleRouter` preserves a full `MinLaneWidth` corridor (720 units) around the larger walls; discards vertices inside other expanded volumes. Endpoint-pocket/station/hazard avoidance and deterministic mirroring remain.
- `HazardPlacer` spends its first half of candidate attempts on main↔additional segments, 35–65% progress, oriented perpendicular to the ray. Existing bounds, site-clearance, mirrored-pair/count checks, backtracking and original placement fallback remain.
- `StationPlacer` now reads `MapFeatureRadii` and reserves the largest site's behind-orientation footprint before outward jitter. With station radius396/site120, Medium/Large outward jitter caps at ≈183 (border footprint gap ≈237); inward jitter retains configured240/360. Front fallback remains when other placements obstruct the behind candidates.
- `MainZonePlacer.AddRing` rejects counts <2 or odd with `InvalidOperationException`; current4/6 unchanged.
- Regression source updated: broad wall geometry/connectedness/rocks/mirrors/720 corridor across all sizes, tiny and distant-blob rejection, local full-width coverage, hazard preference in an open fixture, behind-station footprint room, invalid rings and constructor callers.
- Verification: final Unity `recompile_status` completed with `failed=false`, errors empty; no new Console errors. `git diff --check` passed. No serialized asset changes in this follow-up. Tests and Play Mode not run under local override; all-seed generation, visual obstacle scale and gameplay remain user-only gates.
- Reusable rules updated in [[Architecture/Map_Generation]]. Changes remain uncommitted.

## Verification of Review Fixes (2026-10-29, source only)

- Confirmed in source: 7-circle bowed wall (radius 120, width ≈1250), area-based `RockScatter`, `IsBlocked` full-`LaneWidth` cross-section, router corridor `MinLaneWidth` 720, hazard attempts on main↔additional rays (35–65%), outward jitter capped for behind room, odd ring counts rejected.
- Compile: `Assembly-CSharp.dll` / `Assembly-CSharp-Editor.dll` rebuilt 18:32:50 after last source edit 18:32:37; Console has 0 errors.
- Risk (unverified, Small): each wall circle must clear main satellites (ring 740) by `120 + 360 + 2` and the additional relay pocket (`270 + 20 + 100 + 120` = 510) → wall needs ≈1000–1200 from center → additional relay ≥ ≈1550–1700 from center. Small edge-band relays sit ≈1200–2270 away; nearer picks fail `TryReserve` → layout retry (60 attempts) → possible `InvalidOperationException`. `MapGeneration` Small 2p/4p runs would confirm.
