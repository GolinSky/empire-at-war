---
type: reference
updated: 2026-10-06
tags:
  - alo
  - empire
  - ship-import
---
# Victory II Star Destroyer — Advanced Loadout

## Goal

- Import AOTR `EV_VSD_II.ALO` as Empire ship `VictoryIIAdvanced = 203`.
- Requested hull/shields/speed: `12,000 / 10,000 / 20`.

## Important Values

| Saved configuration | Value |
| --- | --- |
| Targetable weapons | Medium dual turbo-ion ×4; medium turbo-ion ×2 |
| Targetable systems | Shield generator ×1; engines ×2; tractor beam ×1 |
| Other weapons | Medium 3-burst turbolasers ×6; heavy laser cannons ×6 |
| Hangar | Non-targetable; TIE-Interceptor squadron `203`; two total launches, one active |
| Hardpoints | Health targets IDs `0–9`; 18 weapon emitters; 23 fog-bound hardpoints |
| Boost Weapon Power `12` | Fire delays ×0.5; speed ×0.5; shield regeneration ×0; 20 s active / 60 s recovery |
| Tractor Beam `22` | Target speed ×0.4; up to 20 s; recovery 25 s; range 150 |
| Visible bounds | `65.43347 × 42.26736 × 110` project units; bow +Z, up +Y |
| Banking / navigation | ±8°; vertical range `−24.06689 .. 21.44096`; navigation radius 69 |
| Hangar exit | `(0.02554, −32.06689, −4.43979)`; below the banked hull |
| Purchase | Source advanced variant: 13,750 credits / 275 s / level 3 / population 7; provisional cap 10 |
| Shield / hangar timing | Regeneration 16.67/s after 1 s; launch initial/interval 4/30 s |

## Implementation

- Read [[Architecture/ALO_MODEL_IMPORT_GUIDE]], [[Architecture/PROJECT_ORGANIZATION]] and [[Rules/UI_UX_GUIDELINES]] before integration.
- Source: Workshop `1397421866`, `D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data`.
- Live XML: `E_Victory_Star_Destroyer_2_Fighters` inherits `T_Victory_Star_Destroyer_2`; advanced and standard variants share `EV_VSD_II.ALO`.
- Six source turrets: `Empire_Imperial_Star_Destroyer_Triple_Turbolaser_01.ALO .. 06.ALO`; variant 01 differs by hash, 02–06 share a hash. Converted both distinct files; verified equal geometry.
- Turrets attach to `T_01 .. T_06`; firing attachment `FP_01`. Cancel each nested turret rig's imported rotation and scale when attaching beneath hull bones.
- Turbo-ion IDs 0–5 use `MuzzleC_07/01/04/10/22/17`. Heavy lasers use `MuzzleB_08/09/04/05` and `MuzzleA_01/03`. Systems use `HP_S_BONE_03`, `HP_E_BONE_01/02`, `HP_T_BONE`; hangar uses `SPAWN_00`.
- Reuse shared TIE-Interceptor assets: eight craft, hull 15 each, no shields. No fighter substitution.
- Blender 3.6.23 MCP, protocol 13, safe mode enabled, telemetry disabled; dedicated process on port `9880`. The importer scans global objects and fails across existing conversion scenes; isolated import preserves the other Blender session.
- Restore identity `Root` only after binary audit: hull 179 imported → 180 source bones; each turret 5 → 6. Living model imported statically; source death-clone companion `EV_VSD_II_DC.ALO` and animation `EV_VSD_II_DC_DIE_00.ala` exist but are not converted.
- DDS → PNG pixel equality verified. Ship Lit opaque surfaces; separate additive effects; source-alpha-derived team masks, linear normal maps with green flip. Helpers stay disabled.
- Dedicated profiles `MediumDualTurboIon = 37` and `MediumBurstTurbolaser = 38`; existing `MediumTurboIon = 32`, `HeavyLaser = 28` reused. Dual shots use one emitter with a 0.08 s interval; six turbolasers fire three shots per salvo.
- Own gameplay, opaque-hull hologram placement, wreck, transparent 512-pixel icon/silhouette, matchups, Empire roster, data/view Addressables, HUD/tooltip and reinforcement registrations saved.

## Edge Cases

- Unity source comparison: hull 40 meshes / 24,024 triangles / 180 bones; each turret 4 / 1,447 / 6. Mesh counts, triangle counts, UVs and bone parents retained.
- Blender FBX round trip: UV error 0; hull vertex-component error ≤0.317452 source units, bone error ≤0.074284; turret error ≤0.00000123.
- Unity non-lens geometry error ≤0.000002101 raw import units; turret error ≤0.0000000485. Lens-plane error ≤0.008128 raw units, approximately 0.088 gameplay units; effect-bone error ≤0.001486 raw units. Gameplay mounts match saved muzzle/system nodes with zero displacement; source attachment error ≤0.000000958 raw units. Larger displacement belongs to near-coincident engine-effect bone chains.
- Six saved prefabs reload without missing scripts or broken references. Metadata present for all 39 assets; 20 source hashes verified, including the unconverted death companions. All eight team palettes rendered; contrasting blue/green, placement and wreck inspected.
- No automated Unity tests or Play Mode run in this import. An unrelated Hero UI lifecycle test error appeared during concurrent Editor work; no Victory II import/serialization/production compilation errors observed.
- Provisional choices: size, bank/navigation/height, cap, movement tuning, weapon balance/arcs, system HP and matchup copy. Weapon/system HP: 750 / shield 1,500 / each engine 1,000 / tractor 500; source single-ion HP 500 is represented by the common project weapon HP 750.
- Source Boost Weapon Power differs: delay ×0.5, speed ×0.25, shield regeneration ×0, incoming damage ×1.5. Integration uses existing project preset `12`; Tractor Beam uses project preset `22` rather than XML speed ×0.6 / recharge 20 s.
- Source turret aiming, animated shaders/proxy particles and death animations are not recreated.

## Files

- `Tools/Blender/prepare_victory_ii_advanced.py`, `Tools/Blender/VictoryIIAdvanced/` — source audit, conversion, integration and inspection scripts.
- `output/aotr-empire-units/VictoryIIAdvanced-Converted/` — packed blends, FBXs, textures and verification evidence.
- `Assets/Prefabs/Models/Ships/VictoryIIAdvancedShipView.prefab`
- `Assets/Settings/Data/Ship/VictoryIIAdvancedShipData.asset`
- [[TODOs/Features/VictoryIIAdvanced_Import]] — runtime acceptance.
