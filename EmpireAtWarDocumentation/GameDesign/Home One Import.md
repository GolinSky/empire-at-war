---
updated: 2026-10-05
tags:
  - ships
  - rebellion
  - import
---
# Home One Import

## Important Values

- Unit: Home One / Admiral Ackbar; `ShipType.HomeOne = 303`; Rebellion hero; maximum count `1`; population `4`.
- User values: hull `8,100`; shields `2,500`; shield regeneration `80/s`; speed `1.5`; turn rate `0.3`.
- Weapons: `4× TurboLaser` + `4× IonCannon`; systems: shield generator, engines, fighter bay; `11` unique IDs `0..10`.
- Concentrate Fire: selected enemy receives `1.5×` Home One weapon damage; own ships/squadrons within `400` units receive attack orders.
- Power to Shields: regeneration `800/s`, speed `80%`, weapon delays `3×`.
- Both abilities: duration `15 s`; recovery `20 s` after the effect ends, following the existing ability lifecycle.
- Fleet Commander: allied Rebel ships/squadrons throughout the battle; damage/speed `+10%`, hull `+20%`, shields `+10%`, vision `+50%`, incoming damage `×0.65`.

## Decision

- Fleet Commander reuses existing project command bonuses; fleet-wide range implements the user's command role.
- Passive excludes Home One itself, does not stack, stops after commander destruction or ion disable.
- Provisional economy: level `5`, cost `6,500`, build `45 s`; these were not supplied by the user.
- Provisional scale/movement: length `180` units; banking `±5°`; turn acceleration `0.3`; navigation radius `90`; shared Heavy height tier.
- Provisional weapons/systems: existing global weapon profiles; range `500`; weapon hardpoint HP `400`; engine/shield/bay HP `600`.
- Provisional hangar: A-Wing + Y-Wing; each has `3` total reserve launches and `1` active slot; first launch `4 s`, interval `8 s`. A-Wing was the available fighter during integration; fighter types were unspecified.
- Power to Shields regeneration/fire-delay multipliers and passive numerical bonuses are project choices; user supplied qualitative behavior.

## Implementation

- Source: `output/eaw-rebel-ships/DATA/ART/MODELS/RV_HOMEONE.ALO`; dedicated damaged source `RV_HOMEONE_D.ALO`.
- Import/export: Blender `3.6.23` + ALAMO; animations disabled; retain verified binary root and all source bones, UVs and helper flags.
- Eight weapon pods and shield pod attach to matching `HP_*_Bone` transforms; muzzle positions use `FP_{TBL/IC}_{FL/FR/BL/BR}_00`.
- Shield generator → `HP_SHG_Bone`; engines → `HP_E_Bone`; fighter bay uses an authored ventral position because no bay attachment exists.
- Gameplay root scale `1`; bow `+Z`, up `+Y`; FBX scale `0.02`; preserve hierarchy; centered visual scale `13.3160114`.
- Ship/gameplay/placement bounds: `39.14969 × 34.43432 × 180`; wreck: `39.14969 × 29.92235 × 179.99995`.
- Health/fog lists: `11`; weapon list: `8`; all mounts follow `BodyPivot`; launch point `(27.57485, 0, -20)` clears the collider by `8` units.
- Ship data, roster, hero flag, asset mapping, icons, tooltip icon, placement, ability/audio catalogs and existing Addressables `View`/`Data` groups registered.

### Art and verification

- Hull: `16` meshes / `9,002` triangles / `107` bones; wreck: `13 / 5,948 / 50`; each exported pod retains `111` bones.
- Blender FBX round-trip corner displacement: hull `≤0.00006948`, wreck `≤0.00011058` source units; source parent hierarchy retained.
- Unity: all `56` meshes across `11` models preserve triangle counts and UV corners; maximum corner displacement `0.000002720` project units before visual scaling.
- Unity bone displacement `≤0.000003054` units; no parent mismatches. Original ALO/DDS SHA-256 values unchanged.
- Hull alpha → separate inverted linear team mask; HSV livery disabled; normal green channel flipped. Eight hull/wreck palettes rendered; blue and green inspected.
- Actual-model transparent `512×512` icon and silhouette saved. Reloaded four prefabs have no broken references or missing scripts; all asset metadata exists.
- Wreck skinned meshes baked with `BakeMesh(mesh, true)`; static vertex/index channels assigned explicitly. `EditorUtility.CopySerialized` alone left stale render buffers during conversion.
- Final Unity compilation and post-save import/serialization Console checks clean. No automated tests or Play Mode executed.

## Edge Cases

- ALAMO shadow cleanup scans objects in other scenes; a known foreign-view-layer selection error is followed by scene-local cleanup and audited mesh/bone counts. Other scenes remain intact.
- Preserve FBX hierarchy: collapsing the imported root changes nested pod transforms and doubles scale.
- Attach pods to `HP_*_Bone`, not the similarly named helper transform; helpers have authored offsets.
- Generic intact-hull wreck regeneration would replace the dedicated damaged source.
- Source ALA death animation and animated EaW shader/proxy effects are not converted; static source meshes feed the existing procedural wreck effect.

## Files

- `Assets/Prefabs/Models/Ships/HomeOne.prefab` — visual assembly.
- `Assets/Prefabs/Models/Ships/HomeOneShipView.prefab` — gameplay; `Assets/Settings/Data/Ship/HomeOneShipData.asset` — values.
- `Assets/Prefabs/Models/Wrecks/HomeOneWreckView.prefab`; `Assets/Settings/Data/Ship/Wreck/HomeOneWreckData.asset` — wreck.
- `Assets/Prefabs/Ui/Reinforcement/HomeOneReinforcementView.prefab` — placement.
- `Tools/Blender/prepare_home_one_textures.py`; `Tools/Blender/export_home_one.py` — source-specific preparation/export.
- `Temp/HomeOneImport/` — ignored packed Blender files, reports, geometry comparisons, integration scripts and palette previews.

## TODO

- Runtime acceptance: target selection from fighters through capital ships, damage bonus cleanup, fleet command expiry, shield-generator destruction, engine/bay disable, launches, banking, fog, placement and wreck lifecycle.
- Confirm provisional economy, scale, passive values and fighter complement with gameplay balance.
