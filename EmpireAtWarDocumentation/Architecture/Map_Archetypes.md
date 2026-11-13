---
type: engineering-reference
system:
  - map-generation
verified: 2026-11-13
verification: "Archetypes: Map 83/83, MapGeneration 224/224, MapExhaustive
  164/164; 57 previews regenerated. Decorations: source/import/compile checks
  and seed-7 placement inspection only; tests not run. Runtime acceptance
  pending."
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

### Planet decorations
- `PlanetMapProfileEntry.Decorations` optionally selects a `PlanetDecorationSet`; null explicitly means no scenery.
- `MapDecorationPlacer` runs after final geometry with its own `System.Random(matchSeed ^ planetSeed)`; layout RNG, navigation and fairness remain unchanged.
- Clearance includes station/node radii, half lane width, field volumes (including central planet), hazard signed distance + one cell, prior props and map bounds. 128 attempts per requested prop; unplaceable props are skipped.
- `BattleMapLoader` must preserve `layout.Decorations` when rebuilding the layout to filter sites. Generator-only count checks miss dropped collections in later copies; loader regression and live battle visibility are separate checks.
- Decoration spawn rotation = world yaw × prefab local rotation; yaw alone discards Blender/FBX axis correction (dock root ≈−90° X). Spawn scale likewise multiplies the prefab scale.
- `MapDecorationView` spawns once beneath `MapLayoutView.fieldRoot`, using existing load-time frame slicing. No colliders, selection or minimap markers; URP/Lit scenery stays visible through gameplay fog; cast/receive shadows off.
- `PlanetDecorationSet.PlacementBoundsFraction` contracts each map half-extent around its center, then subtracts the scaled prop radius. Space junk uses 0.70 (entire footprint inside the central 70% of each dimension); docks/containers retain 1.0. Clearance checks and bounded placement attempts are unchanged.
- Kuat docks request 3–5 intact + 1–2 broken at scale 0.10–0.14; junk entries request 1–2 / 1–3 / 1–3 at 0.18–0.30; containers request 3–6 at 0.50–0.80. Height jitter ±15; clearance = (source origin-relative radius + 5) × scale.
- Implementation/import evidence and proposed planet assignments → [[Done/Features/Planet_Decorations_Plan]]. Source/asset checks and seed-7 layout inspection completed 2026-11-13; decoration tests and battle visuals remain unverified.

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
