---
category: Features
status: done
updated: 2026-11-13
completed: 2026-11-13
acceptance: pending-user
---
# Map Archetypes Research

## Implementation
- Parent → [[Done/Features/Map_Archetypes_Plan]].
- Explicit planet assignments below are implementation defaults; user visual/gameplay acceptance remains open.
- Open Field with 2–4 players preserves the original generation path and RNG consumption. Nine pre-change geometry fingerprints cover Small/Medium/Large × seeds 0/7/19.
- Other layouts use a stable planet seed for macro orientation and the match seed for detail. Full Skirmish passes `MapSpec.Planet`; Survival/Empty retain their own generators.
- Eight-row lobby uses the existing row style in a vertical scroll view. Gallery cards show archetype names and complete square layout previews.

## Important Values
| Archetype | Planets |
|---|---|
| Open Field | Coruscant, Kuat, Anaxes, Corulag, Fondor, Balmorra, Ralltiir, Eriadu |
| Asteroid Belt | Hoth, Geonosis, AetenII, AlzocIII, Hypori, Polus, Sullust, Shola |
| Twin Pass | Mandalore, Corellia, Carida, AbregadoRae, Ryloth, Utapau, Tatooine, Korriban |
| Orbital Ring | Endor, Bespin, Byss, Alderaan, Yavin, Hapes, Naboo, Dantooine |
| Dense Field | Kessel, Ilum, Kashyyyk, Myrkr, Wayland, Honoghr, Bonadan, Muunilinst |
| Nebula Corridors | Dagobah, Mustafar, NalHutta, Felucia, Dathomir, Atzerri, Jabiim, Lwhekk |
| Islands | Kamino, Manaan, MonCalamari, Bestine, Bothawui, Fresia, Saleucami, Taris, Thyferra |

## Edge Cases
- Contiguous allies naturally have different enemy distances inside a sector. Compare mean enemy lane distance between teams; compare neutral-objective access per player. FFA teams each contain one player.
- Coincident relay/mine nodes must share zero-cost graph access; otherwise fairness silently ignores those mines.
- Odd, uneven-team or asymmetric station rosters cannot require point-mirrored additional relay groups. Require actual reflected opposing station partners before choosing mirrors; independent groups retain counts/footprint checks and fairness acceptance.
- Lane raster clearings can erase a nominal field stamp. Verify actual generated obstacles, not only the pattern function.
- Orbital planet rendering must fit the explicit generated obstacle radius at the gameplay plane; the old backdrop transform does not match navigation.
- New eight-row lobby snapshots require matching game builds; older four-row clients with the same application version are incompatible.

## Files
- `Assets/Scripts/Entities/Map/` — profile data and generation.
- `Assets/Settings/Data/Models/Map/Archetypes/` and `PlanetMapProfiles.asset` — authored assets.
- `Assets/Art/Textures/Ui/MapPreviews/` — 57 Medium-duel, seed-137 layout schematics.
- `Logs/MapArchetypes/` — test responses, probes and isolated UI renders; local verification evidence.

## TODO
- User visual/gameplay acceptance and optional biome follow-up are recorded in the parent plan and acceptance queue.

## Decision
- Orbital Ring with 3–8 players uses an arc excluding the near-main pole. This intentionally replaces strict two-team point symmetry: the central planet and offset main relay require fair navigable approaches, validated by lane-distance limits.
- Neutral-objective fairness remains per player; enemy-access fairness compares team means. Interior allies in a contiguous sector otherwise fail solely because teammates shield them from enemies. FFA remains per-player because each team has one member.
- Gallery outputs are 57 generated-layout schematics. Final battlefield materials, eight-fleet performance and live online play require separate runtime acceptance.
- Reusable rules extracted to [[Architecture/Map_Archetypes]]; legacy-only scope clarified in [[Architecture/Map_Generation]].

## Verification
- MapGeneration: 224/224 passed, 255.34 s; final Map category: 83/83 passed, 18.76 s.
- All 57 previews regenerated successfully from the final quick-sweep source; gallery and eight-player setup renders inspected at 1920×1080.
- New Unity assets/scripts: metadata check found no missing `.meta` files. Scoped source/asset diff checks passed.
- MapExhaustive: 164/164 passed, 1120.01 s; archetype cases cover seeds 0–19. No failed, skipped or inconclusive cases.
