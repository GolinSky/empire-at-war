---
type: reference
updated: 2026-10-05
tags:
  - unity
  - alo
  - rebellion
  - x-wing
---
# X-Wing Import

## Goal

- Integrate vanilla `RV_XWING.ALO` as the Rebel T-65 X-Wing following [[Architecture/ALO_MODEL_IMPORT_GUIDE]].
- Preserve original ALO/ALA/DDS, visible geometry, UVs, attachment hierarchy and animated S-Foils.

## Decision

- `SquadronType.XWing = 300`, `WeaponType.XWingLaser = 24`, `ShipAbilityId.LockSFoils = 18`.
- Five craft; four lasers each; no independently destructible weapon hardpoints. Member death uses existing fighter explosion/removal.
- User speed multiplier ×1.3 overrides the individual vanilla XML's ×1.5. Fire delay ×3 and regeneration ×3 follow the individual fighter XML.
- Movement/range use provisional project pacing after the optional scale question received no reply. Preserve raw EaW values as reference; no direct unit conversion is established.
- Existing reinforcement transport supports arrival/retreat. No separate campaign hyperdrive system was added.

## Important Values

| Value | Source / requested | Integrated |
| --- | ---: | ---: |
| Craft / lasers per craft | 5 / 4 | 5 / 4 |
| Hull / shields per craft | 60 / 20 | 60 / 20 |
| Shield regeneration | 3/s | 3/s |
| Speed / minimum speed | 4 / 2.5 | combat 32 / cruise 20 units/s |
| Turn rate | 3 | 110°/s |
| Laser attack range | 450 | 45 units |
| Closed-wing speed / fire delay / regeneration | ×1.3 / ×3 / increased | ×1.3 / ×3 / ×3 → 9/s |
| Tactical cost / build / tech / population | 500 / 15 s / 1 / 1 | 500 / 15 s / 1 / 1 |

- Source verified in `Temp/VictoryImport/BaseGame/DATA/XML/SPACEUNITSFIGHTERS.XML` and `SQUADRONS.XML`; linked StrategyWiki rejected retrieval.
- Laser damage 5 per cannon, one shot, reload 1.5 s, projectile speed 140 units/s. All 20 cannon IDs are unique `0..19`.
- Provisional values: acceleration 22, bank ±40°, spacing 5, height 11, radar 70, member length 4, member collider radius 2.5, navigation radius 16, selection diameter 32, queue limit 10.
- Shield regeneration delay 1 s; no passive hull repair. Gameplay/placement bounds: 23.16832 × 2.48642 × 12 units.

## Implementation

- Blender 3.6.23 + ALAMO: 14 source bones, 10 meshes, 2,639 triangles. Visible `X_Wing_LOD1`: 1,732 triangles.
- ALAMO removed the source identity `Root`; restore it only after binary audit. Duplicate engine attachment `pe_Z95engines` is retained as `.001`.
- Original ALA uses an 18-byte header incompatible with the installed compressed-header parser. Model-specific decoding preserves quantized translations/quaternions: 31 frames at 30 fps.
- `Deploy` → `CloseSFoils`; `Undeploy` → `OpenSFoils`; Legacy clips, 1 s, ClampForever, no automatic playback.
- Hull vertices each have exactly one full bone weight; triangles never cross bones. Five rigid body/wing sections preserve vertices, normals, tangents and UVs; original skin/helpers remain disabled in the model.
- Rigid sections support the existing MeshRenderer team-color and fog APIs. No shared renderer/shader changes remain.
- Four `MuzzleA_00..03` mounts and four nozzle trails per fighter follow the animated wing bones. Health/flight member bindings 5; weapon/fog hardpoints 20; team meshes 25; S-Foils animations 5.
- Entity owns `SFoilsModel` → view coordination; ability uses `ISFoilsFacade`. Toggle skips timed expiry; second press removes its modifiers and opens the wings. Existing death, ion disable and service teardown still stop it.
- Ability tooltip omits timed-duration/remaining fields for toggles; existing active highlight and second-click cancellation remain.
- Rebellion roster, view/data mappings, existing Addressables groups, placement, HUD/tooltip sprites, matchups and dedicated audio keys registered. Sound/VFX reuse existing project effects.
- Hull alpha → linear team mask; hull stays opaque. Three DDS → PNG conversions are lossless. Gloss texture preserved as source data.

### Verification

- Source SHA-256 unchanged. Source/FBX/Unity retain all triangle counts; maximum Unity triangle-corner error 0.000000158 units and rest-bone error 0.000001672 units.
- Visible hull UV error ≤0.000000040. Disabled low-detail hull merges nearby UVs by ≤0.000344 during Unity import; packed source/FBX retain authored UVs.
- Six sampled animation poses: maximum Unity attachment error 0.000255 units; rigid section displacement against original skin math is zero.
- Three saved prefabs reload with zero missing scripts or broken references. Gameplay/placement each have 25 visible sections and 8,660 triangles; bounds agree.
- Roster, icons, placement, mapping and Addressables resolve to X-Wing assets; ability settings resolve to `LockSFoilsSettings`.
- Eight distinct palettes rendered; blue/green inspected. Transparent 512 × 512 actual-model icon/silhouette are uncropped; alpha bounds `(34,80)..(457,414)`.
- Final script compilation clean; no new console import/serialization errors. Dirty `MainMenuScene` preserved.
- No automated tests or Play Mode executed. Runtime combat/toggle/placement/destruction and balance acceptance remain in [[TODOs/Features/XWing_Import]].

## Edge Cases

- Run animation preparation only on a fresh source rig; repeating root repair is invalid. `SplitHull` and `Register` are one-time fresh-integration scripts.
- Isolated preview rendering avoids uninjected gameplay components; restore palette/lighting state and close preview scenes.
- Low-detail, collision, shadow, source flash/engine helpers stay disabled. No A-Wing assets used.
- Source cinematic ALA, animated material effects and proxy particles are not converted; use existing project fighter effects.

## Files

- Source: `output/eaw-rebel-ships/DATA/ART/MODELS/RV_XWING.ALO`, `RV_XWING_DEPLOY_00.ALA`, `RV_XWING_UNDEPLOY_00.ALA`; `RV_XWING.DDS`, `RV_XWING_GLOSS.DDS`, `W_LASER_SMALL.DDS`.
- Editable package/reports/previews/scripts: `output/eaw-rebel-ships/XWing-Converted/`.
- Tooling: `Tools/Blender/prepare_xwing.py`, `prepare_xwing_animations.py`, `export_xwing.py`, `README.md`.
- Art: `Assets/Art/{Models,Materials/Models,Textures/Models}/RebellionShips/XWing/`.
- Visual/gameplay: `Assets/Prefabs/Models/Squadrons/XWing.prefab`, `XWingSquadronView.prefab`.
- Placement: `Assets/Prefabs/Ui/Reinforcement/XWingReinforcementView.prefab`.
- Data: `Assets/Settings/Data/Squadron/XWingSquadronData.asset`, `Tooltip/Matchups/XWingSquadronMatchups.asset`.
