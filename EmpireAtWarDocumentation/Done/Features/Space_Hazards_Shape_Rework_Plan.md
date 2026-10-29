---
category: Features
status: done
created: 2026-10-28
completed: 2026-10-29
disposition: merged
merged_into: "[[TODOs/Features/Space_Hazards_Plan]]"
---
# Space Hazards Shape Rework (Organic Clouds + SDF Geometry)

> Merged 2026-10-29 into [[TODOs/Features/Space_Hazards_Plan]]: open items transferred there; this note is history.

Follow-up to [[Done/Features/Space_Hazards_Rework_Plan|Space Hazards Rework Plan]] after a code + capture review on 2026-10-28. User feedback: the band shape did not look like a nebula or an ion storm.

## Goal

- Hazards read as natural gas clouds / storms: irregular lobed outline, wisps, filaments along a flow, volume in cinematic view.
- Keep one geometry source: visible dense edge == gameplay edge.
- Keep the level-design role: large territory-claiming clouds, fair placement, size scaled with map.

## Review Findings (pre-rework, `Logs/SpaceHazardsRework/*.png`)

- Spindle profile: `widths = width·sin(t·π)`, end widths `0` → lens with pointed tips.
- Spine bend `±0.08 × length`; Small used `bend = 0` → straight, collinear bands = dashed wall between teams.
- Shader feathered only `across > 0.9` + `rim` line → drawn border; 2-octave `sin`-hash value noise; ion veins = contour lines.
- 3 identical ribbon sheets 12 m apart at `y = -60` → flat plate with doubled edges in cinematic.
- Asteroid fringe = continuous rock wall tracing the outline.
- `HazardBand` rebuilt in 6 places; `FieldDensity(..., hazards = null)` optional default; normal-offset polygon could self-intersect on sharp bends.

## Decisions (confirmed 2026-10-28)

- **Small map = islands across the axis** (option b). Each cloud sits on the axis equidistant to the teams, long along the team axis, folded so it is its own mirror image across that axis; the two islands differ in type. Avoid: collinear bands (dashed wall).
- **Shape = lobe chain.** `HazardCloudGenerator`: 7–10 elliptical lobes on a Catmull-Rom spine (stream bend `±0.15 × length`; island bow `±0.15`), 2–3 side puffs; profile `0.45 + 0.55·sin` → rounded ends; lobe reach `≥ 0.65 × spacing / cos(slope)` keeps neighbours overlapping.
- **Edge = smooth union + warp.** Polynomial smooth max (`blend = 0.3 × roughness`, capped at `+blend/4`), domain warp `0.22 × roughness`, detail `0.06 × roughness`; `HazardSpot.Spread` bounds how far the outline passes the lobe capsules.
- **Geometry = signed-distance grid.** `HazardShape : IHazardSource`; cell `side/400` clamped `8–20 m`; grid margin `400 m` (largest padded query ≈ 375 m); edge samples project onto the outline with one Newton step, others take the distance to the nearest projection (exact nearest-target transform, `SquaredDistanceTransform`). `Contains` = bilinear lookup.
- **Placement uses lobe capsules + Spread.** Conservative clearance vs stations, nodes, map bounds and other clouds; the grid is baked only for chosen spots. Small keeps the 128-candidate deterministic sweep (offset `0.15–0.42 × side`).
- **Cloud height.** Sheets stack downward from `y = -60` (5 sheets, `25 m` apart, inset `14 m`/sheet, softer edges lower) → ships at usual tiers draw above the gas; Deep/Lowest tiers sit under it. Why: ships span `-570..+30 m`; sheets above the ship tinted it at min zoom.
- **No billboard puffs** for now; sheets read as volume in captures.
- **Fringe = clumps.** Fringe cells only where symmetric noise (`0.35 × FieldFeatureSize`) `> 0.55`; offset derived from the ridge offset (no extra random draw).

## Implementation

1. [x] **Shape model.** `HazardLobe`, `HazardFrame` (noise frame, mirror/fold), `HazardSpot` (lobes, roughness, cell size, `Spread`, `GetClearance`, `GetReach`, `Mirror`, `Translate`), `HazardCloudGenerator.CreateStream/CreateIsland`.
2. [x] **SDF bake.** `HazardShape` (renamed from `HazardBand`, meta kept) + `SquaredDistanceTransform.FindNearest`.
3. [x] **Build once.** `MapLayoutGenerator` bakes shapes; `MapLayout.Hazards : IReadOnlyList<HazardShape>`; `HazardService`, `FieldDensity` (required param), `MapLayoutView`, `MiniMapHazard` consume them.
4. [x] **Placer.** Capsule-proxy fitting, mirrored pairs, Small islands with mixed types.
5. [x] **Field fringe.** Noise-gated clumps; SDF clearing + `2 × FieldCellSize`.
6. [x] **Cloud visuals.** New `SpaceHazard.shader` (SDF edge, 4–5 octave gradient fBm + domain warp + flow stretch, wisps ≤ 15%, per-sheet `_Inset`/`_Softness`, ridged ion filaments, local `_Flash`); `HazardCloudView` builds an `RHalf` distance texture + quad over `Bounds + 90 m`; 20-point jagged arcs between core points; `SpaceHazard.prefab` now 5 sheets; materials retuned via `SpaceHazardVisualBuilder.CloudMaterial`.
7. [x] **Minimap.** `MiniMapHazardView` fills row runs of inside samples (~2 px) + brighter edge band.
8. [ ] **Performance.** Bake time per map and transparent overdraw of 5 sheets not measured.
9. [ ] **Final.** `Map` category run + `MapExhaustive` (explicit) + user in-game acceptance.

## Verification (2026-10-28)

- Compile clean (`recompile_status` failed=false, no console errors).
- EditMode fixtures: `HazardZoneTests` 7/7 (ellipse outline/padding/hysteresis, generated clouds one piece + conservative clearance, island fold symmetry, mirrored-pair symmetry), `HazardServiceTests` 8/8, `HazardHeightTests` 1/1, `SpaceHazardPresentationTests` 4/4, `ShipNavigationServiceTests` 27/27, `EnemyUnitCommanderTests` 18/18, `DecorAsteroidBuilderTests` 4/4 (rerun; first run failed only on a bridge "main thread timed out" log).
- Not verified: `MapLayoutGeneratorTests` / `Map` category (runs lost to bridge disconnects), `MapExhaustive`, `BattleMapLoaderHazardTests` (enters Play Mode; skipped per agent policy).
- Captures: `Logs/SpaceHazardsShapeRework/` (both types min/medium/max zoom + cinematic; `*-boundary.png` overlays show grid zero-crossing marks on the visible dense edge).
- Fixed during tests: detached side puffs and broken chains on steep bends (seed 2) → puffs reach past the spine, bend cap `0.25 → 0.15`, reach `/ cos(slope)`.

## Edge Cases

- Warp pushing the outline outside `Bounds` → `Spread` inflation in `GetReach`/`Fits`.
- Queries padded beyond `400 m` outside the grid → conservative lower bound (`margin + distance to grid`).
- Folded island seam: warp across the mirror line fades over `FOLD_RAMP = 60 m`.
- Chained smooth unions capped at one union's lift so `Spread` stays conservative.

## TODO

- Run `Map` category and `MapExhaustive` (`--include_explicit true`); fix any Small/Medium/Large placement failures.
- Measure bake time (target `< 20 ms` total, Large) and 5-sheet overdraw at max zoom.
- Move remaining `HazardType.IonStorm` branches (`HazardCloudView`, `HazardEntryCue`, editor builders) onto data/material.
- User in-game review of colours/opacity, island layout and asteroid clumps.

## Files

- New: `Entities/Map/Hazards/{HazardLobe,HazardFrame,HazardShape,SquaredDistanceTransform}.cs`, `Entities/Map/Generation/HazardCloudGenerator.cs`.
- Rewritten: `HazardSpot`, `HazardPlacer`, `HazardCloudView`, `SpaceHazard.shader`, `MiniMapHazard(View)`, `SpaceHazardCapture`, `HazardZoneTests`.
- Touched: `FieldDensity`, `AsteroidFieldBuilder`, `MapLayout`, `MapLayoutGenerator`, `MapLayoutView`, `HazardService`, `SpaceHazardVisualBuilder`, `SpaceHazard.prefab`, hazard materials, hazard-related tests.
