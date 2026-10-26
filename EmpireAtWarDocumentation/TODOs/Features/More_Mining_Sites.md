---
category: Features
status: in-progress
created: 2026-10-26
---
# More Mining Sites

## Status (2026-10-26)

- **Implemented**: 2 home mines per station, central pairs, counts Small `8`/`1`, Medium `10`/`1`, Large `14`/`2`; validator + test helper updated; Survival follows `MiningSiteCount`; old saves rejected (count mismatch).
- **Deviation: second home mine** starts at the same gap but steps outward `20` units at a time until it clears the first mine and default zone (`TryFindHomeMiningPosition`). Why: at 45° and the same gap, Medium Easy needs ~820 between mines but gets ~650, so it can't fit.
- **Tuning to fit 3–4 player maps** (counts failed 11/20 Small, 20/20 Large otherwise): Small `PocketThickness` `132` → `100`; Large `PointSpacing` `420` → `300`. Both pass 20/20 seeds for 3p and 4p.
- `CentralRegionShare` = `0.15` × map side (= ±600/±900/±1350 as planned).
- **Verified**: compile clean; EditMode `MapLayoutGeneratorTests` 23/23 (2 new), `BattleSaveServiceTests` 21/21, `SurvivalSpawnTests` 2/2; console has no new errors.
- **Not run**: `RealEntityCompositionTests`, full suite, Play Mode.
- **Remaining**: user in-game check of generated maps (all sizes, 2p/4p), economy/AI feel with more mines.

## Goal

- More mining capture sites on generated skirmish maps.
- 2 home mining sites beside each station (currently 1).
- Contested mining sites in the map center on every size, Small included.
- Keep point symmetry and lane/field passability.

## Current State (verified 2026-10-26)

- Maps are procedural; no hand-placed sites in `Battle.unity`.
- `PointOfInterestPlacer.TryAddHomeMining` places 1 mine per station along Z, turned ≤ `HomeMiningAngle` (asset: 5°) toward center; distance = station radius + outer + `HomeMiningGap` lerp by hardest AI difficulty.
- Default zone takes the X side of the station → home mine goes along Z.
- Remaining mines: `TryAddPairs(MiningSite, (MiningSiteCount - 2) / 2)` → point-symmetric pairs.
- `MapSizeSettings.MiningSiteCount` includes home mines; tooltip says "home mining site of each side".
- `MapGenerationSettings.asset` mining counts: Small `2`, Medium `4`, Large `8`.
- `BattleSaveValidator.ValidateMapCounts` hardcodes the same rule: `Slots.Count + ((MiningSiteCount - 2) / 2) * 2`.
- `SurvivalLayoutGenerator` rings `MiningSiteCount` mines around the single station.
- Random pairs use best-candidate sampling that maximizes distance from everything placed → pairs drift away from the center; nothing guarantees a central mine.
- Odd `CapturableZoneCount` puts a zone at the exact center (Medium `3`, Large `9`); Small (`2`) leaves the center empty.

## Decision (proposed — confirm before implementing)

- Add `HomeMiningSitesPerStation` (`int`, `Min(1)`, value `2`) to `MapGenerationSettings`.
  - Why: placer, validator and tests share one source of truth.
  - Avoid: hardcoding `2` in three places again.
- Add `CentralMiningPairCount` (`int`, `Min(0)`) to `MapSizeSettings`: mirrored pairs placed inside the central region.
- Add `CentralRegionShare` (`float`, `Range(0.1, 0.5)`, value `0.3`) to `MapGenerationSettings`: central region half-size = share × map half-side (Small ±600, Medium ±900, Large ±1350).
  - Why: the existing picker pushes pairs outward; a restricted box guarantees contested center mines.
  - Avoid: a single unmirrored mine at the exact center — Medium/Large already hold a central zone there.
- New `MiningSiteCount` (includes 2 × `HomeMiningSitesPerStation` home mines):

| Size | Now | Proposed | Home | Central pairs | Other pairs |
|---|---|---|---|---|---|
| Small | 2 | 8 | 4 | 1 | 1 |
| Medium | 4 | 10 | 4 | 1 | 2 |
| Large | 8 | 14 | 4 | 2 | 3 |

- Fit check (outer = mine radius `60` + clearance `30` + pocket): a pair needs each mine ≥ outer + `PointSpacing`/2 from center → Small ≥ `222` (region `600`), Medium ≥ `420` (`900`), Large ≥ `570` (`1350`). All fit; Medium/Large also clear the central zone via `IsClear`.

- Second home mine direction: diagonal toward map center, ~`45°` from Z toward X, same gap as the first mine.
  - Shares the existing random `turn` for both sides → stays point-symmetric.
  - Lies between the Z-side mine and the X-side default zone; `IsClear` rejects overlaps → layout retries.

## Implementation

1. `MapGenerationSettings`: add `HomeMiningSitesPerStation` + tooltip; set `2` in `Assets/Settings/Data/Models/Map/MapGenerationSettings.asset`.
2. `MapSizeSettings.MiningSiteCount`: `Min(2)` → tooltip "Includes `HomeMiningSitesPerStation` home mines for each of two sides"; update counts per table.
3. `PointOfInterestPlacer.TryAddHomeMining`: loop `HomeMiningSitesPerStation` directions per station.
   - Direction k: angle from Z = `turn + k * HOME_MINING_SPREAD` (k=0 keeps current behavior).
   - Add each ring via `PairWithMirroredStations(stations, center, firstIndex, nodes)` per k so `Mirror` indices stay valid.
   - Replace `(size.MiningSiteCount - 2) / 2` with `(size.MiningSiteCount - 2 * HomeMiningSitesPerStation) / 2` total pairs.
4. `PointOfInterestPlacer` central pairs: after home mines and the central zone, before battle sites/zones.
   - `TryAddPairs(MiningSite, size.CentralMiningPairCount, …)` with the candidate box limited to center ± region half-size (pass the box into `TryPickPairPosition` instead of always using full bounds).
   - Remaining mining pairs = total pairs − `CentralMiningPairCount`, placed with the full-bounds picker as today.
5. `BattleSaveValidator.ValidateMapCounts`: `Slots.Count * HomeMiningSitesPerStation + pairs * 2` (central pairs are part of `pairs`; no separate term).
6. `SurvivalLayoutGenerator`: no code change; it rings the new `MiningSiteCount` (Small survival 2 → 8 mines). Confirm this is wanted.
7. Asset persistence per `AGENTS.md`: Refresh → `ForceReserializeAssets` on the settings asset → `SaveAssets` → console check.

## Tests (EditMode only; no agent Play Mode)

- `MapLayoutGeneratorTests`: existing count assertions already read `size.MiningSiteCount`.
- New: each station has exactly `HomeMiningSitesPerStation` mining sites nearer to it than to any other station, within `radius + outer + HomeMiningGap.Max` (+ tolerance).
- New: home mines are point-symmetric (mirror of each exists).
- New: every size has ≥ 2 × `CentralMiningPairCount` mining sites inside the central region.
- New: `CentralMiningPairCount` ≤ total mining pairs for every size in the settings asset.
- Keep `HomeMining_IsFartherOnHarderDifficulty`, `Fields_LeaveLanesPassableAndEveryPointOfInterestConnected`, all sizes × difficulties × seeds generate without `InvalidOperationException`.
- `BattleSaveServiceTests` / validator tests: update expected site counts.

## Edge Cases

- Large + UltraHard: `HomeMiningGap.Max` `1350` + second mine may not fit beside the default zone → generation throws after `MAX_LAYOUT_ATTEMPTS` (60). Fix by tuning spread angle or gap, not by skipping the mine.
- 3-player layouts: station without twin mirrors itself (existing `FindMirroredStation` rule) — applies per home ring.
- Saves made before the change fail `ValidateMapCounts` (site count mismatch) → rejected on load. Decide: accept, or bump save version.
- AI picks Battle Asteroid when battle-site count < mining-site count → more mines shift AI build choice; `EnemyProductionStrategy` mining targets unchanged.
- Economy: Small 1 → 4 mines per side, Medium 2 → 5, Large 4 → 7 → higher income ceiling; review with `GameDesign/Economy`.
- Small map: 4 zones/default zones + 2 battle sites + 8 mines in 4000 × 4000 → more retries; if generation fails, shrink `PocketThickness` or region share before dropping mines.
- Lanes: central mines become lane hubs; `Fields_LeaveLanesPassable…` must stay green on all sizes.

## Open Questions

- Final counts per size (table above is a proposal; central vs other pairs adjustable per size).
- Second home mine always at the same gap as the first, or always at `HomeMiningGap.Min` (easy access regardless of difficulty)?
- Survival: keep ringing `MiningSiteCount`, or keep its old count?
- Old saves: reject or migrate?

## Files

- `Assets/Scripts/Entities/Map/Generation/PointOfInterestPlacer.cs`
- `Assets/Scripts/Entities/Map/MapGenerationSettings.cs`
- `Assets/Scripts/Entities/Map/MapSizeSettings.cs`
- `Assets/Settings/Data/Models/Map/MapGenerationSettings.asset`
- `Assets/Scripts/Services/Saves/BattleSaveValidator.cs`
- `Assets/Scripts/Entities/Map/Generation/SurvivalLayoutGenerator.cs` (review only)
- `Assets/Scripts/Tests/Editor/MapLayoutGeneratorTests.cs`

## TODO

- [x] User confirms decisions and open questions (2026-10-26: 8/10/14, 1/1/2 central, same gap, Survival follows count, old saves rejected)
- [x] Settings field + asset values
- [x] Placer: 2 home mines per station
- [x] Placer: central mining pairs
- [x] Validator rule
- [x] EditMode tests green (affected fixtures)
- [ ] User in-game check of generated maps (all sizes)
