---
type: engineering-reference
system:
  - map-generation
verified: 2026-11-13
verification: Map 83/83, MapGeneration 224/224, MapExhaustive 164/164; all 57
  previews regenerated; runtime acceptance remains pending.
---
# Map Archetypes

## Rules
- `BattleMapLoader` passes the selected planet to `MapLayoutGenerator.Generate`; `PlanetMapProfiles.asset` contains one explicit assignment for each of 57 planets.
- Seven kinds: Open Field, Asteroid Belt, Twin Pass, Orbital Ring, Dense Field, Nebula Corridors, Islands. Planet decides the kind; no lobby archetype override.
- Planet seed fixes macro orientation and placement rules. Match seed controls detail, candidate selection, fields and hazards.
- Open Field with 2–4 players uses the unchanged legacy generation branch. Archetype overrides apply to other kinds and Open Field with 5–8 players.
- Limits: Small 4, Medium 6, Large 8. Shrinking the setup map preserves occupied slots and disables Start until the roster fits.
- Bounds scale: base half-size × `BoundsMultiplier` × `sqrt(max(1, players / 4))`; authored multiplier 1.15. Large8 half-size ≈7319 m; network positions must remain within −8192..8191.75 m.
- Additional relay pairs: base + `max(0, (players - 3) / 2)` + archetype/profile additions. Hazard pairs scale by the same count; Orbital Ring adds one central storm.

## Implementation
- `MapSizeSettings.ForArchetype` copies settings per call; never mutate shared serialized settings during generation.
- `ArchetypeFieldPattern` stamps macro fields; lane and feature clearings retain priority. Belt gaps override pockets/fringes and remain excluded from `SealedSpaceFiller`, including raster spill margin.
- Twin Pass reserves physical wall disks before placement and routes lanes around them. Curved flank columns keep station radius equal; relay candidates use an inner-to-outer flank band to avoid crowding stations.
- Orbital Ring inserts a full-height navigation field disk. `PlanetView.FitObstacle` fits explicitly bound surface/cloud renderers to the same center and radius. Its central storm rotates with the skeleton, perpendicular to the main-relay direction.
- Lane routes through Belt/Twin Pass choose the shortest permitted crossing. A main-relay endpoint at the origin must not bias every route toward one flank.
- Candidate placement checks central-planet clearance before accepting relays; late rejection alone can exhaust all attempts without trying valid relay orientations.
- Hazard plug candidates must leave candidate capacity for broader placements; concentrating the entire budget at two gaps cannot satisfy larger hazard counts.
- Grouped allies get 60 layout attempts before 60 attempts with a nearest-boundary slot exchange; every candidate retains enemy, footprint and fairness checks. Legacy and FFA keep their original attempt budget.
- Mirrored additional groups require balanced teams and actual opposing reflected station partners. Odd, uneven or asymmetric station layouts use independent groups with the same total relay count.
- `BattleSaveValidator` computes counts through the same legacy/archetype branch as generation.

## Edge Cases
- Old planet+seed setups can regenerate different geometry. No migration reproduces pre-archetype non-Open-Field maps.
- Eight-player lobby support requires matching game versions; compatibility with old four-player clients is not established.
- Deterministic seed tests establish sampled layout invariants, not eight-fleet CPU/GPU performance or multiplayer gameplay acceptance.
- Gallery previews are generated top-down layout schematics (Medium duel, seed 137), not screenshots of final battlefield materials or the selected match seed.

## Files
- `Assets/Scripts/Entities/Map/Generation/` — station, relay, lane, field and hazard rules.
- `Assets/Settings/Data/Models/Map/Archetypes/` and `PlanetMapProfiles.asset` — authored defaults and assignments.
- `Assets/Scripts/Editor/MapArchetypePreviewBuilder.cs` → `Assets/Art/Textures/Ui/MapPreviews/`.
- `Assets/Scripts/Tests/Editor/Map/MapArchetypeGenerationTests.cs` — explicit `MapGeneration` and `MapExhaustive` sweeps.
- [[Done/Features/Map_Archetypes_Plan]] — scope, current verification and remaining acceptance.
