---
category: Features
status: done
created: 2026-11-13
updated: 2026-11-13
completed: 2026-11-13
acceptance: pending-user
---
# Planet Decorations Plan

Simplified replacement for the deferred biome phase of [[Done/Features/Map_Archetypes_Plan]]. Rules → [[Architecture/Map_Archetypes]].

## Goal
- Selected planets spawn original EaW space props on the battle map so they read as that place.
- Visual only: no effect on navigation, lanes, fairness or archetype generation.

## Decision (2026-11-13, user)
- **Placement:** inside the playable map, away from relays, lanes, stations and hazards. No collision; ships never route around props.
- **Kuat:** mix `EB_DOCK00` (intact) and `EB_DOCK_BREAK` (broken); mostly intact, 1–2 broken, picked by match seed.
- **Planet lists:** agent-proposed defaults below; user approves via [[TODOs/User_Acceptance_Queue]].
- Avoid: full biomes (rock materials, hazard tints, dust). Not in scope.

## Proposed Assignments
| Set | Models | Planets |
|---|---|---|
| Shipyard docks | `EB_DOCK00`, `EB_DOCK_BREAK` | Kuat |
| Space junk | `W_SPACEJUNK`, `W_SPACEJUNK_MED`, `W_SPACEJUNK_SML` | Tatooine, NalHutta, Ryloth, Saleucami, Taris, Atzerri |
| Resource container | `W_ORBITAL_RESOURCE_CONTAINER` | Kessel, Bonadan, Thyferra, Muunilinst, Sullust, Bespin |
| None | — | all other planets |

## Sources
- Base game `D:\SteamLibrary\steamapps\common\Star Wars Empire at War\GameData\Data\models.meg` → `DATA\ART\MODELS\<NAME>.ALO`.
- Already extracted with previews: `F:\CodexArtifacts\EawDecorationPreview\` (`index.html`, `space-scenery.jpg`).
- The three junk ALOs are distinct files (different hashes), same 505,484 B size.

## Implementation
1. Import 6 models through the Blender route (`Tools/Blender/<Model>/`, like `ConstructionPod`); read `AssetPipeline/Guides/ADDRESSABLES_BUNDLE_GUIDE` first. Prefabs: static mesh, no colliders, shadows per [[Rules/BATTLE_PERFORMANCE_GUIDELINES]].
2. `PlanetDecorationSet` ScriptableObject (`Assets/Settings/Data/Models/Map/Decorations/`): entries = prefab, count range, scale range, height jitter, clearance radius.
3. `PlanetMapProfileEntry.Decorations` — optional reference; empty = planet has no decorations (explicit, not a fallback).
4. `MapDecorationPlacer` (generation): own `System.Random` from match seed ⊕ planet seed. Must not consume the layout RNG → legacy Open Field fingerprints unchanged.
5. Reject spots overlapping nodes (zones, stations, relays), lane corridors (`MinLaneWidth/2` + prop radius), hazards, field volumes, central planet obstacle. Fewer spots than requested → place what fits; never fail the layout.
6. `MapLayout.Decorations` → `MapDecorationView` under `MapLayoutView` instantiates prefabs once at load.
7. Tests (authored; run only on request): determinism per seed, clearance rules, legacy fingerprints unaffected, every set references a prefab. Categories: `Map`.

## Edge Cases
- Visual only → ships can visually pass through large docks; clearance from lanes keeps this rare.
- Not networked or saved: regenerated identically from seed + planet on every client.
- Fog of war / minimap: props remain visible through gameplay fog (URP/Lit); no minimap markers.
- Kuat on Small maps: docks are large; clamp count by free area.

## Implementation Result
- [x] Imported all six ALO models through Blender; FBX, textures and materials under `Assets/Art/{Models,Textures,Materials}/`, with exact paths in `Tools/Blender/PlanetDecorations/ImportEvidence.json`.
- [x] Six prefabs saved in `Assets/Prefabs/Models/Other/PlanetDecorations/`; no colliders, cast/receive shadows off.
- [x] Three sets saved in `Assets/Settings/Data/Models/Map/Decorations/`; 13 planet assignments in `PlanetMapProfiles.asset`, remaining planets explicitly null.
- [x] Data/profile field, independent seeded placer and load-time view integrated. Geometry generation and its RNG unchanged.
- [x] `MapDecorationTests` authored, category `Map`: deterministic spots, bounds/overlap, lane/field/height, station/node/prop, hazard clearance, full-map skip and prefab references. Tests not run under the local override.
- [x] Unity compilation completed: `failed:false`, `errors:[]`; recent Console check empty. Assets imported and saved.
- Non-test Editor layout inspection: two players, seed 7, Small/Medium/Large → Kuat 5/4/4, Tatooine 7/7/7, Kessel 3/3/3. This establishes sampled generator placement availability only; it bypassed `BattleMapLoader` and missed the later layout-copy drop.
- Evidence: `Tools/Blender/PlanetDecorations/{README.md,ImportEvidence.json,PlacementEvidence.json}`.
- Reusable implementation rules and exact counts/scales → [[Architecture/Map_Archetypes#Planet decorations]].

## TODO
- User-only gates → [[TODOs/User_Acceptance_Queue]]: proposed planet list approval, in-battle visuals/scale/fog, authored Map tests on explicit request.

## Follow-up · 2026-11-13
- User battle check failed: no decorations visible on Kuat/Tatooine. `BattleMapLoader` discarded generated decorations when reconstructing the layout to filter sites.
- Narrow fix and renewed visibility acceptance → [[Done/Bugs/Planet_Decorations_Loading_Fix]]. Original generator-count evidence did not verify delivery to the view.
