---
category: Features
status: done
created: 2026-11-13
updated: 2026-11-13
completed: 2026-11-13
acceptance: pending-user
---
# Map Archetypes Plan

Builds on [[Architecture/Map_Generation]]. Reference model: AoE-style rule-based procedural generation (archetype + biome + rules + seed + constraints).

## Goal
- Each planet has its own recognizable layout pattern; replays of the same planet vary in detail but read as the same map.
- Station positions are no longer fixed corners: anywhere on the perimeter or inner ring, allies may start together, enemies never too close.
- Skirmish supports 6–8 players on larger maps.

## Baseline Before Implementation (2026-11-13)
- `PlanetType` (50+ values) only selects the planet prefab (`MapGenerationSettings.planetPrefabs`); structure is global + per `MapSize`.
- `MapLayoutGenerator` fixed skeleton: 4 corner stations (`StationPlacer.CORNER_SIGNS`), main relay at exact center, `AdditionalZonePairCount` mirrored pairs in edge band, Gabriel spanning-tree lanes + `ExtraLaneChance`, noise fields, `HazardCount` hazards on main→additional segments.
- Seed: `SkirmishPreset.NewMapSeed()` = new GUID hash per match; no planet influence.
- Players: `MatchRules.MAX_PLAYERS = 4`; `MAX_TEAM_COLORS = 8` already. `MAX_PLAYERS` sized arrays in `CaptureStrengthBuilder`, `BattleVictoryService`, `SkirmishModel`, `TutorialSetupModel`. `SkirmishUi.slotRows` is a serialized row array.
- Layout is point-symmetric (mirror pairs) → fair only for 2 opposing sides.

## Decision
- **Two-layer seed.** Planet seed (stable hash of `PlanetType`) → macro: archetype variant, skeleton orientation, gap positions, main relay offset, station slot set. Run seed (current per-match seed) → micro: jitter, noise offset, extra lanes, hazard shapes, station slot pick within allowed set.
- **Structure ≠ visuals.** `MapArchetype` owns topology; biome (rock material, hazard tint, dust) is separate and optional.
- **Open Field archetype = today's generator.** Must reproduce current output for the same seed → regression baseline.
- Why: AoE recognizability comes from fixed rules, not fixed geometry. Avoid: hand-authored per-planet layouts (50+ maps to maintain).

## Data
- `MapArchetype` (ScriptableObject, `Assets/Settings/Data/Models/Map/Archetypes/`): relay strategy + params, field skeleton stamps, noise overrides (threshold, feature size, ridge), hazard rule (count, placement, types), lane rule (extra chance, bend), planet role (backdrop / central obstacle), station placement rule, variance ranges, supported player counts.
- `PlanetMapProfile` (per planet, or one table asset): archetype, planet seed override, per-planet overrides, biome (later).
- Missing profile → fail fast; no silent Open Field fallback.

## Archetypes (initial)
| Archetype | Pattern | Example planets |
|---|---|---|
| Open Field | current layout | Coruscant, Kuat |
| Asteroid Belt | thick central belt, 2–3 chokepoint gaps | Hoth, Geonosis |
| Twin Pass | walled center, two flank lanes | Mandalore, Corellia |
| Orbital Ring | planet blocks center, relays ring it | Endor, Bespin |
| Dense Field | heavy fields, narrow lanes, pocketed sites | Kessel, Ilum |
| Nebula Corridors | hazards form walls, sparse rock | Dagobah, Mustafar |
| Islands | relay clusters separated by voids | Kamino, Manaan |

## Station Placement
- Replace fixed corners with **candidate slots**: perimeter band (corners, edge midpoints, along edges) + optional inner slots, defined per archetype.
- **Team grouping:** a team gets a contiguous sector; allies start together in it when possible (adjacent slots, min ally gap = footprints + home sites), else nearest valid separate slots.
- **Enemy constraint:** min enemy station distance per `MapSize` (path distance along lanes preferred over straight line), plus min distance from enemy home sites and default zones.
- **Fairness checks:** per player compare lane-path distance to main relay, nearest neutral mines, nearest enemy; reject layout if spread exceeds tolerance.
- 2 teams → keep point symmetry (mirror slots). 3+ teams / FFA → n-fold rotational symmetry where map shape allows, else fairness-metric acceptance.
- Home mining/satellites follow station facing (toward map center) instead of the corner-behind rule.

## 6–8 Players
- `MAX_PLAYERS` → 8; allowed count per `MapSize`: Small ≤4, Medium ≤6, Large ≤8 (decided; no Huge size).
- Scale per player count: additional relay groups, neutral mines, map bounds, hazard count.
- Touchpoints: `MatchRules`, fixed arrays (`CaptureStrengthBuilder`, `BattleVictoryService`), skirmish/tutorial setup models, `SkirmishUi` slot rows (8 rows, scroll/compact layout), team-color palette (8 ok), saves (`SkirmishSetupSave`), networking indexes (`NetIndexMap`), minimap, AI per-player cost, battle telemetry.
- Performance: 8 fleets → CPU/GPU budget check against [[Rules/BATTLE_PERFORMANCE_GUIDELINES]].

## Phases
- [x] 0. Design: seven archetypes, explicit 57-planet table, player limits and grouped-first ally starts implemented; table approval queued.
- [x] 1. Infrastructure: `MapArchetype`, `PlanetMapProfile`, seed split; route `MapLayoutGenerator` through archetype; Open Field reproduces current output.
- [x] 2. Station placement: slot candidates, team sectors, enemy min distance, fairness checks (still ≤4 players).
- [x] 3. Field skeleton stamps (belt, ring, walls, clusters) in `FieldDensity` with guaranteed gaps respected by `MainLineBlocker` / `LaneObstacleRouter`.
- [x] 4. Relay strategies: edge band (current), ring, flank, clusters, off-center main relay; counts scale with players.
- [x] 5. Hazard strategies: corridor walls, chokepoint plugs, central storm.
- [x] 6. 6–8 players: `MAX_PLAYERS`, arrays, setup UI, saves, networking, AI, n-fold layout symmetry.
- [x] 7. Assign all planets; skirmish gallery shows archetype name + top-down layout preview.
- [x] 8. Verification: quick/exhaustive seed sweeps passed; 57 editor-generated top-down layout schematics saved. Final battlefield visual approval queued.
- [ ] 9. Optional, deferred: biome visuals. No biome art direction or new visual asset set selected in this implementation.

## Edge Cases
- Saved skirmish setups (planet + seed) regenerate different geometry after Phase 1.
- Narrow gaps must keep `MinLaneWidth` for capital ships.
- Allies starting together on a point-symmetric 2-team map → teams occupy mirrored sectors.
- Orbital Ring: planet as obstacle needs navigation blocking, not just visuals.
- Ship routing contract ([[Done/Bugs/Ship_Route_Curvature_Plan]]): every blocking obstacle is a disk or union of disks (`FieldVolume`, planet as disk `MapObstacle`); hazard walls steer by cost only and ships may clip their edges. Impassable walls must be disks, not hazards.
- Tests disabled locally ([[AGENTS]] override); compile/source checks only unless user requests runs.

## Phase 0 Decisions (2026-11-13, user)
- Player limits: Small ≤4, Medium ≤6, Large ≤8. No new Huge size; Large scales to 8.
- Allies start together when the layout allows (adjacent slots in one team sector); fall back to nearest valid separate slots only when no together layout passes constraints. No lobby toggle.
- Planet always decides the archetype. No "Random archetype" lobby option.

## TODO
- User-only acceptance → [[TODOs/User_Acceptance_Queue]]: proposed planet assignments/layouts, final battlefield visuals, eight-fleet CPU/GPU budget and live multiplayer.

## Execution · 2026-11-13
- Status: implementation and automated verification complete. User authorized implementation, test execution and commit; runtime/visual acceptance remains pending.
- Parallel ownership: archetype generation; station/home placement; player setup/gallery. Integration, seed sweeps and persisted assets verified centrally.
- Planet assignments beyond the listed examples are explicit proposed defaults; visual/gameplay acceptance remains with the user.
- Verification target: Open Field same-seed regression; Map / MapGeneration / MapExhaustive; affected setup/UI/core categories.

## Implementation Status
- Phases 0–8 implemented with the scoped decisions below; optional biome visuals deferred. User-only gates do not block closure.
- Station fallback verified by 24 targeted generation diagnostics: grouped attempts precede nearest-boundary slot exchanges; Mandalore Small4 seed0 exercises the fallback with all enemy/home clearances retained.
- Phase 0: all 57 assignments exist as explicit proposed defaults; table in [[Done/Features/Map_Archetypes_Plan - Research]].
- Passing categories: Map 83/83, Ui 278/278, GameFlow 187/187, Capture 111/111, Networking 31/31, Core 151/151. Tests explicitly authorized by the user.
- Final quick generation sweep: MapGeneration 224/224 passed (255.34 s). Covers seven archetypes × supported size/player counts × seeds 0/7/19, all 57 planets at Large8, uneven/multiple teams, final physical-gap checks and nine legacy fingerprints. Earlier failures fixed.
- MapExhaustive: 164/164 passed, 1120.01 s. Archetype roster cases cover seeds 0–19; legacy generator sweeps and nine original-geometry fingerprints passed.
- Scoped deviations: Orbital Ring uses a near-main exclusion arc instead of strict two-team point symmetry; enemy-access spread compares team means. See research decisions and canonical [[Architecture/Map_Archetypes]].
- Runtime acceptance remains: eight-fleet CPU/GPU budget, live multiplayer, and final battlefield visuals. Gallery previews are generated-layout schematics.
- Preview artifacts: all 57 regenerated after final geometry changes; gallery and eight-player setup inspected at 1920×1080. Schematics show actual layout data, not rendered battlefield materials.
- Reusable guidance extracted to [[Architecture/Map_Archetypes]]; [[Architecture/Map_Generation]] now distinguishes legacy rules. No missing Unity metadata; scoped diffs clean.
- Optional Phase 9 biome visuals remain outside this implementation.

## Re-verification · 2026-11-13 (source only, tests not run)
- Commit `b3a7e3674` holds all plan work; no uncommitted archetype changes.
- Accepted by user: older builds cannot join the eight-row lobby (matching versions required); old non-Open-Field setups regenerate new geometry.
- `PlanetMapProfiles.asset`: 57/57 `PlanetType` values, no duplicates; all seven archetype groups match the research table; 57 previews linked and present.
- `MatchRules.MAX_PLAYERS = 8`; `GetMaxPlayers` Small 4 / Medium 6 / Large 8. Fixed arrays, `OnlineLobby.MAX_MEMBERS`, 8 `SkirmishUi.slotRows`, 8 default slots follow it.
- `MapLayoutGenerator.Generate(..., planet)` fails fast on missing table/profile; Open Field ≤4 players keeps the legacy `CORNER_SIGNS` path. `BattleMapLoader` and `BattleSaveValidator` use the same branch.
- Unity Editor idle, console 0 errors.
