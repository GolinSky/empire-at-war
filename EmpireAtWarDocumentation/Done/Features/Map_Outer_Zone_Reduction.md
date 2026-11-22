---
category: Features
status: done
completed: 2026-11-22
acceptance: pending-user
created: 2026-11-22
---
# Map Outer Zone Reduction

## Goal

- Fewer outer relay zones on Skirmish (Full) maps.
- Outer zones sit far from each other and far from the main relay.
- Survival and Empty layouts unchanged.

## Target (user-confirmed 2026-11-22)

| Size | Main | Outer now | Outer target | Close-station (per station) |
|---|---:|---:|---:|---:|
| Small | 1 | 2 | 2 | 1 |
| Medium | 1 | 4 | 2 | 1 |
| Large | 1 | 6 | 4 | 1 |

- "Close station" = `DefaultZone`, one per station → 2 in a 1v1. Unchanged.
- Extra players add no outer zones: counts hold for every roster (Large 8 players `10 → 4`).
- Each outer zone keeps 1 mine + `AdditionalZoneSatelliteCount` (4) pads.

## Current State (source, 2026-11-22)

- `MapSizeSettings.AdditionalZonePairCount` = `1 / 2 / 3` → `2 / 4 / 6` zones.
- `MapSizeSettings.ForArchetype` adds `max(0, (playerCount − 3) / 2)` + archetype + profile pairs. All archetype/profile `AdditionalRelayPairs` are `0` in assets.
- `AdditionalZonePlacer.TryPlace`: mirrored pairs, best of 12 valid by largest minimum distance to every node/station. `TryPlaceUnpaired`: single zones when the roster cannot mirror (archetype layouts only).
- Hard distance rules: `AdditionalZoneMainDistance` `900 / 1400 / 2000`, `AdditionalZoneStationDistance` `1100 / 1300 / 1500`. No minimum distance between outer zones (only pocket overlap + `PointSpacing` `0 / 120 / 300`).
- Legacy Open Field (2–4 players): candidates only inside `AdditionalZoneEdgeBand` `800 / 1100 / 1400` from a border.
- Bounds half-extent: `2000 / 3000 / 4500`.

## Decision

### Counts stay in pairs

- `AdditionalZonePairCount` → `1 / 1 / 2`. All targets are even → no field rename, no unpaired neutral zone.
- `ForArchetype`: remove the player-count term; keep archetype + profile pairs (all `0`, explicit per-planet opt-in).
- Avoid: a third Medium zone (dropped by the user; no mirror partner exists for a single zone).

### Far placement

- New `MapSizeSettings.AdditionalZoneSpacing`: minimum centre distance between two outer zones (mirror partner included).
- Raise `AdditionalZoneMainDistance`.
- Keep best-of-N scoring (largest minimum distance to every node/station); raise `CANDIDATES_PER_PICK` `12 → 24`.
- Start values, **not verified** — tune in step 6:

| Setting | Small | Medium | Large |
|---|---:|---:|---:|
| `AdditionalZoneMainDistance` | 900 → 1400 | 1400 → 2100 | 2000 → 3200 |
| `AdditionalZoneSpacing` (new) | 2800 | 4200 | 2800 |

- A mirrored pair is `2 ×` its main distance apart → Small/Medium spacing is implied by main distance; the setting matters on Large (2 pairs) and on unmirrored rosters.
- Limits: Small legacy edge band forces ≥ `1200` from centre; farthest edge-midpoint spot ≈ `1610` → Small main distance cannot exceed ≈ `1500`.
- Archetype candidates: `CreateArchetypeCandidate` radius share `0.4–0.88` of half-extent; the low end is always rejected by the new main distance → raise the lower share to cut wasted attempts.

### Economy needs no change (verified in source)

- `MiningFacilityFactory.GetIncome`: capture-site mine income = `MiningPool.GetSpotIncome(playerCount, spots)`; `spots` = live count of mining sites on the map.
- No `MapSize` dependency in `Entities/Economy`, `Entities/MiningFacility`, `Services/Economy`.
- Fewer mines → each mine pays a larger share; total pool per player unchanged.

### Saves

- Medium/Large Skirmish saves stop loading (count mismatch) → accepted by the user 2026-11-22.

## Implementation

1. `MapSizeSettings`: add `AdditionalZoneSpacing`; `ForArchetype` drops the player-count term.
2. `MapGenerationSettings.asset`: pairs `1 / 1 / 2`, new distances. Persist per `AGENTS.md` (Refresh → `ForceReserializeAssets` on that path → `SaveAssets` → console).
3. `AdditionalZonePlacer`: spacing check in mirrored and unpaired paths; `CANDIDATES_PER_PICK` `24`; add `spacing=` to `LastFailure`; raise archetype radius floor.
4. `BattleSaveValidator.ValidateMapCounts`: formula unchanged (reads the pair count). Verify it uses the archetype-adjusted size for 5–8 players.
5. Review, change only if broken: `HazardPlacer` (main→outer segments), `MainLineBlocker`, `LaneNetworkBuilder` (fewer, longer lanes), `FreeSatellitePlacer` (`2 / 6 / 10` pads on fewer lanes), `MapLayoutFairness` (nearest neutral mine per player), `FieldDensity` (edge band), AI readers in [[Architecture/Enemy_AI_Objectives]] (one outer group per outer zone).
6. Tune: scripted generation, 7 planets × every size/roster × seeds `0, 7, 19` (315 layouts, same sweep as [[Done/Features/Map_Satellite_Pad_Expansion]]). Raise distances until failures appear, then back off one step. Record outer↔outer and outer↔main minimum distances.
7. Update [[Architecture/Map_Generation]]: counts, spacing, value table, player-count rule.
8. Optional: regenerate gallery previews with `MapArchetypePreviewBuilder`.

## Tests (review + edit; run only on explicit user request)

- Count formulas already read `AdditionalZonePairCount` (`MapLayoutGeneratorTests` 126, 638, 642, 864; `MapPlanetRosterTests` 64; `BattleSaveServiceTests` 624, 635) → review, expect no edit.
- New assertions, values read from settings: outer↔outer ≥ `AdditionalZoneSpacing`; outer↔main ≥ `AdditionalZoneMainDistance` on archetype layouts too.
- New: 5–8 player roster has the same outer zone count as the base size.
- Open Field baseline hashes (9) regenerate.
- Categories: `Map`, `MapGeneration` (explicit), `GameFlow`, `EnemyAi`. `MapExhaustive` only on separate request.

## Edge Cases

- Large 8 players: 8 stations, only 4 outer zones → `MapLayoutFairness` mine-distance spread (`MaximumFairnessRatio`) is the likely failure source (`fairness=`).
- Unmirrored rosters (2v1, 3 players): single zones, spacing check is the only spread guard.
- Fewer relays → fewer lanes → free pads cluster or fail (`freePads=`).
- Fewer mines per player on big rosters → more contested economy; AI and balance check in-game.
- Same seed → different geometry; previews and saved seeds change.
- Small saves keep the old site count, so `ValidateMapCounts` passes on a different layout → verify how saves bind to regenerated geometry before shipping.

## Files

- `Assets/Scripts/Entities/Map/MapSizeSettings.cs`
- `Assets/Scripts/Entities/Map/Generation/AdditionalZonePlacer.cs`
- `Assets/Settings/Data/Models/Map/MapGenerationSettings.asset`
- `Assets/Scripts/Tests/Editor/Map/{MapLayoutGeneratorTests,MapPlanetRosterTests,MapArchetypeGenerationTests}.cs`, `Assets/Scripts/Tests/Editor/GameFlow/BattleSaveServiceTests.cs`

## Outcome (2026-11-22)

- Implemented as planned. Shipped values (tuned, see step 6):

| Setting | Small | Medium | Large |
|---|---:|---:|---:|
| `AdditionalZonePairCount` | 1 | 1 | 2 |
| `AdditionalZoneMainDistance` | 1500 | 1700 | 2900 |
| `AdditionalZoneSpacing` | 3000 | 3400 | 3400 |

- Failure edges (one step up): Small main `1700` (legacy edge band, station rejects), Medium main `1900` (Endor Orbital Ring 6p FFA `fairness=`), Large main `3200` (Endor 7p `fairness=`), Large spacing `4000` (3-player unmirrored `spacing=`). Small kept at `1500`, not `1600`, for edge-band margin.
- Measured minimums (asset values): outer↔outer `3004 / 3401 / 3402`, outer↔main `1502 / 1753 / 2901`. Before (old distances, spacing `0`): outer↔outer `1525` Medium, `1664` Large.
- Archetype radial candidates: floor `clamp(MainDistance / half, 0.4, 0.8)` instead of `0.4`.
- Review, no change needed: `BattleSaveValidator.ValidateMapCounts` (uses `ForArchetype` for >4 players, same as generator), `HazardPlacer`, `MainLineBlocker`, `LaneNetworkBuilder`, `FreeSatellitePlacer`, `MapLayoutFairness`, `FieldDensity` (0 generation failures), `EnemySquadronCommander` (one outer wing group per outer zone, built from live zones).
- Saves have no geometry version. Old Small saves pass the count check and load onto the new geometry (zone/site state by index, units at saved positions) → accepted by the user 2026-11-22; no save version bump.
- Verification: Unity compile `0` errors from this change; asset reserialized and read back. Scripted generation 7 planets × every size/roster × seeds `0,7,19` × both team assignments = `630` layouts, `0` failures, `0` outer-count errors. EditMode tests not run (local override). Test files compiled by dry run.
- Tests updated: `MapLayoutGeneratorTests` (outer spacing assertion, 9 Open Field baseline hashes regenerated), `MapArchetypeGenerationTests` (outer count, outer↔main and outer↔outer on every layout; new `LargeRosters_KeepBaseOuterRelayCount`). Reviewed, no edit: `MapPlanetRosterTests`, `BattleSaveServiceTests`.
- Reusable guidance → [[Architecture/Map_Generation]] (values, spacing rule, roster rule, tuning edges), [[Architecture/Map_Archetypes]] (relay pair formula).
- Not done (optional): gallery preview regeneration (`MapArchetypePreviewBuilder`).

## TODO

- [x] Open questions answered (2026-11-22)
- [x] `AdditionalZoneSpacing` field, `ForArchetype` player term removed, asset values
- [x] `AdditionalZonePlacer`: spacing + candidate count
- [x] Save validator check for 5–8 players
- [x] Lane / hazard / free-pad / fairness / AI review
- [x] Distance tuning sweep (630 layouts)
- [x] Tests updated
- [x] [[Architecture/Map_Generation]] updated
- [ ] In-game acceptance: zone spread, AI → [[TODOs/User_Acceptance_Queue]]
