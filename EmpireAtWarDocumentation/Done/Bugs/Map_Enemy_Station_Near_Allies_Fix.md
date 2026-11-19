---
category: Bugs
status: done
created: 2026-11-19
updated: 2026-11-19
completed: 2026-11-19
acceptance: pending-user
---
# Map: Enemy Station Near Allied Stations Fix

Builds on [[Architecture/Map_Archetypes]] and [[Done/Features/Map_Archetypes_Plan]].

## Goal
- Team games (2v2, 3v3, 4v4) on Medium/Large: allied stations sit together, enemy stations sit clearly farther away.
- Target: nearest enemy station ≥ 1.5 × nearest allied station distance for every player with an ally.
- Duel, FFA and Small-map output unchanged where possible.

## Cause (source-verified 2026-11-19, no generation run)
- `StationSlotGeometry.CreateRing` spaces slots at equal angles (`step = 2π / count`). Teams are contiguous, so the enemy at a team boundary is exactly as close as the nearest ally.
- `StationPlacer.PlaceSlots(..., separateTeams: true)` (attempts 60–119) swaps the two slots at the first team boundary → enemy lands inside the allied arc.
  - 2v2: `A A B B` → `A B A B` (every neighbour is an enemy).
  - 3v3: `A A A B B B` → `A A B A B B`.
- Orbital Ring (`count > 2`): 240° arc, `step = 240° / (count - 1)`. The 120° gap separates only one team boundary; the other boundary is one normal step.
- Legacy Open Field, 2–4 players (`StationPlacer.Place`): 2v2 uses four corners; ally and one enemy are both edge-adjacent → equal distance.
- `HasStationClearance` only enforces an absolute enemy minimum (`AdditionalZoneStationDistance`: Medium 1300, Large 1500). No ally-relative rule; `MapLayoutFairness` compares team means only.

## Important Values
| Case | Half-size | Ring radius `R` | Ally neighbour | Boundary enemy |
|---|---|---|---|---|
| Medium 2v2 ring | 3450 | 2904–4107 | 1.41 R (90°) | 1.41 R (90°) |
| Medium 3v3 ring | 4225 | 3679–4248 | 1.00 R (60°) | 1.00 R (60°) |
| Medium 3v3 Orbital | 4225 | 3679–4248 | 0.81 R (48°) | 0.81 R (48°) |
| Large 4v4 ring | 7319 | 6773–7330 | 0.77 R (45°) | 0.77 R (45°) |

- `available = half − (396 station + 120 home + 30 margin)`; mining radius 120 assumed from test fixtures.
- Ally minimum clearance today: `r1 + r2 + 4·home + 2·HOME_SITE_GAP` ≈ 1302.
- Twin Pass 2v2/3v3 already separates teams by flank (enemy/ally ≈ 1.4) → not the main offender.

## Decision
- Chosen: weighted angular gaps. Gap between allies = 1 unit, gap across a team boundary = `TEAM_GAP_WEIGHT` units (const, 2.0); units normalised to the full arc.
- Why: one rule fixes ring and Orbital Ring; equal teams keep point symmetry; FFA (all gaps are boundaries) and duel stay identical.
- Avoid: raising `AdditionalZoneStationDistance` (absolute, does not order ally vs enemy); a hard runtime ratio rejection (uneven rosters could exhaust all attempts).

| Case with weight 2.0 | Ally | Boundary enemy | Ratio |
|---|---|---|---|
| 2v2 ring | 60° → 1.00 R | 120° → 1.73 R | 1.73 |
| 3v3 ring | 45° → 0.77 R | 90° → 1.41 R | 1.85 |
| 4v4 ring | 36° → 0.62 R | 72° → 1.18 R | 1.90 |
| 3v3 Orbital (240° arc) | 40° → 0.68 R | 80° → 1.29 R | 1.88 |

- Smallest ally chord (4v4 Large) ≈ 0.62 × 6773 = 4186 > 1302 clearance → fits.

## Implementation
1. [x] Diagnose first (editor eval, not a test run): before fix, every ring archetype Small/Medium/Large → min enemy/ally = 1.00; legacy Open Field 0.92–0.97. Twin Pass 2v2 1.43, 3v3 2.72.
2. [x] `StationPlacer.PlaceSlots(..., groupTeams, separateTeams)`: team-run sizes passed to `StationSlotGeometry.Create`; all-ones → original equal-step path (float-identical).
3. [x] `StationSlotGeometry.WeightTeamGaps`: allied gap 1, boundary gap 2. Orbital open arc: allied gap capped at `120° / TEAM_GAP_WEIGHT` = 60° (else 2v1 = 1.35); boundaries share the remainder; arc endpoints unchanged.
4. [x] `MapLayoutGenerator`: `groupTeams = mapSize != Small`; swap fallback only on Small.
5. [x] `StationPlacer.PullAlliedCorners` (Medium/Large): allied edge-sharing corners slide along the shared edge in 5 % steps (min 30 %) until enemy ≥ 1.5 × ally. 3v1 skipped — any asymmetric pull made every Medium/Large 3v1 Open Field seed fail mirrored relay pairs (`additional relays: pair`). No-ally rosters untouched.
6. [x] Tests (authored + reviewed, not run):
   - `MapStationSlotTests`: new case — balanced 2v2/3v3/4v4, every archetype path plus legacy Open Field 2v2: nearest enemy ≥ 1.5 × nearest ally; no enemy between two allies in slot order.
   - Existing reflection, equal-centre-distance, Orbital 58° and Twin Pass assertions must still hold; review `MapArchetypeGenerationTests`, `MapLayoutGeneratorTests` (Open Field fingerprints), `MapPlanetRosterTests`, `BattleSaveServiceTests`.
7. [x] Update [[Architecture/Map_Archetypes]] (team gap rule, fallback change, Twin Pass known gap).
8. [x] Mandatory final report delivered in the 2026-11-19 session response.

## Outcome (2026-11-19)
- After-fix diagnostic (Medium/Large, 7 archetypes, 3 seeds): ring archetypes 2v2 1.73, 2v1 1.62 (Orbital 1.73), 3v3 1.85–1.88, 4v4 1.90–1.93, 2v2v2 1.88–1.91; Open Field 2v2 1.52/1.57, 2v1 1.61/1.52. `PlaceSlots` grouped: 0 clearance failures over 20 seeds.
- Small: unchanged (Open Field baselines Small 0/7/19 hash-identical).
- Tests: `MapStationSlotTests` (+`GroupedRingSlots_KeepEnemiesFartherThanAllies`, `GroupedCorners_KeepEnemiesFartherAndOnlyMoveInward`, `Grouping_LeavesRostersWithoutAlliesUnchanged`); `MapLayoutGeneratorTests.OpenField_MatchesGeometryBaseline` Medium/Large hashes recomputed via editor eval of the test's `Fingerprint`. Category `Map`. Not run.
- Not fixed / follow-up: Twin Pass 2v2 1.43, 3v1 0.70, 2v2v2 0.37; Open Field 3v1 0.92–0.96; Small Twin Pass 4-player generation fails seeds 0–2 (pre-existing, points=120).
- Reusable guidance → [[Architecture/Map_Archetypes]].

## Mandatory Final Report
- Agents do not run tests for this plan. After implementation the agent's final response must contain this report so the user can run the tests and share results.
- Report contents:
  1. Changed files and one line per behaviour change.
  2. Test fixtures added/updated, with category.
  3. Exact runs for the user, in order, each with mode, filter, explicit setting and quick/exhaustive label:
     - `run_tests --mode editor --filter_type category --filter Map --include_explicit false` (quick).
     - `run_tests --mode editor --filter_type category --filter MapGeneration --include_explicit true` (explicit, 3 seeds, ≈4–5 min).
     - `GameFlow` category only if `BattleSaveValidator` or save tests changed.
     - `MapExhaustive` listed as optional, never required.
  4. Expected-to-change tests (e.g. fingerprints for rosters with allies) vs tests that must stay green unchanged (duel/FFA Open Field fingerprints).
  5. Where failure names appear: LocalLow `TestResults.xml`.
  6. In-game check: Medium and Large, 2v2 and 3v3, one Open Field planet and one Orbital Ring planet.
- After the report: add the gates to [[TODOs/User_Acceptance_Queue]], set `acceptance: pending-user`, state tests as not run.
- User returns with results → failures reopen this plan; all green + in-game OK → `acceptance: passed`.

## Edge Cases
- Uneven teams (3v1, 2v1v1): weights apply per boundary; lone players get boundary gaps on both sides.
- 3+ teams with allies (2v2v2, Large): three boundaries → gaps shrink (2v2v2: ally 40°, enemy 80°); still ratio > 1.5.
- Twin Pass uneven rosters: flank split is by count, not team → an enemy can share a flank. Out of scope unless confirmed as a seen case.
- Compressed allies → home mining/satellite and default-relay candidates sit closer; `PointOfInterestPlacer` rejections may rise. Watch `points=` failure counts in the diagnose step.
- Fairness: all ring slots keep equal centre distance; per-player neutral-mine spread may change → needs a `MapGeneration` sweep.
- Saved skirmish setups (planet + seed) with allies regenerate different station positions.
- Gallery previews are Medium duel → unchanged.

## User Decisions (2026-11-19)
- Open Field with allies: agent's choice → pull allied corners together inside `StationPlacer.Place`. Why: small change, legacy generator and duel/FFA output kept. Avoid: routing these rosters through the archetype path (changes bounds, relays, lanes and the `BattleSaveValidator` branch).
- Separation: ratio 1.5 / `TEAM_GAP_WEIGHT` 2.0 accepted.
- Tests: agent does not run them; agent delivers the Mandatory Final Report, user runs and returns results.

## Files
- `Assets/Scripts/Entities/Map/Generation/StationSlotGeometry.cs`
- `Assets/Scripts/Entities/Map/Generation/StationPlacer.cs`
- `Assets/Scripts/Entities/Map/Generation/MapLayoutGenerator.cs`
- `Assets/Scripts/Tests/Editor/Map/MapStationSlotTests.cs`
