---
type: reference
updated: 2026-10-05
tags:
  - unity
  - alo
  - rebellion
  - mc80
---
# Mon Calamari Cruiser Import

## Goal

- Add vanilla `RV_MONCALCRUISER.ALO` as the Rebellion MC80; retain the import and the later user-approved tuning.
- Follow [[Architecture/ALO_MODEL_IMPORT_GUIDE]]; preserve original ALO/DDS, geometry, UVs and attachment hierarchy.

## Decision

- `ShipType.MonCalCruiser = 302`; `MonCalCruiserPowerToShields = 15`.
- Latest loadout: four `HeavyTurboLaser` enhanced batteries, four `DualHeavyTurboLaser`, two `TurboLaser` and two `IonCannon`. Shared weapon profiles remain unchanged.
- Power to Shields uses existing boost behavior: 15 s active, 40 s recovery, speed ×0.8, damage ×0.5, shield regeneration ×10 → 700/s.
- User approved committing the later tuning: speed 15 units/s, yaw/turn acceleration 6°/s, shields 2,500, regeneration 70/s and 12 weapon batteries. These supersede the initial request values.
- No shield-generator hardpoint or hangar. Thirteen destroyable IDs `0..12`; existing health rules cannot permanently disable this ship's shields through a generator.

## Important Values

| Value | MC80 |
| --- | ---: |
| Hull / shields | 8,500 / 2,500 |
| Shield regeneration | 70/s |
| Speed / yaw | 15 units/s / 6°/s |
| Cost / build / tech / population | 4,500 / 30 s / 5 / 4 |
| Visible size | 86.52927 × 23.62375 × 160 units |
| Navigation radius / bank | 80 units / ±5° |
| Banked hull Y | −11.81187..12.27966 |

- Original request: shields 2,000, regeneration 50/s, speed 1.5, yaw 0.4 and six batteries. Later user-approved tuning is recorded above; the initial values are not the current asset configuration. Reference links: [Mon Calamari Cruiser](https://empireatwar.fandom.com/wiki/Mon_Calamari_Cruiser), [Rebel units](https://strategywiki.org/wiki/Star_Wars%3A_Empire_at_War/Rebel_units). Those sites rejected retrieval; values are attributed to the request.
- Provisional project values: length 160, radar 500, height tier 2, weapon/engine hardpoint health 400 each, queue limit 10, regeneration delay 1 s.

## Implementation

- Blender 3.6.23, existing ALAMO/native MCP; preserve other scenes, safe mode and configuration. Main 71 bones / 14 meshes / 7,290 triangles; damaged 79 / 15 / 4,177.
- Six separate hardpoint-pod FBXs preserve 77 bones each. Triangles F-L/F-R/M-L/M-R/B-L/B-R: 52/58/93/130/221/272. Attach their roots at main `HP00_*` positions with model-local translation and identity rotation/scale; parenting to scaled bones duplicates FBX scale.
- Four `HeavyTurboLaser`: `FP_F-L_00`, `FP_F-R_00`, `FP_B-L_00`, `FP_B-R_00`; two `IonCannon`: `FP_M-L_00`, `FP_M-R_00`. Engines: `HP_E_Bone`. Port arcs −177.5..−2.5°, starboard 2.5..177.5°.
- Later weapon mounts: `Turbolaser_FL/FR`, `DualHeavyTurbolaser_ML/MR/BL2/BR2`. All 12 weapon mounts and engines have unique IDs and saved health/fog bindings; all follow the banking body.
- Living renderers: hull/windows and six pod hull/window pairs. Authored helpers, shield, collision, shadow, damage overlays and source engine planes stay disabled.
- FBX scale 0.02; visual scale 13.7813988; centered bow +Z/up +Y. Gameplay/placement/wreck roots scale 1. Rebuilt collision, shield, ion bounds, selection, banking hardpoints, fog, team and explosion references.
- Eight DDS conversions are pixel-identical. Hull-alpha inverse supplies linear team mask; nonzero coverage 93.8679%. Normal map imports linear with green-channel flip; hull is opaque despite team-mask alpha.
- Damaged FBX keeps its rig. Wreck uses a rest-pose bake of hull/shadow, with vertex/normal/tangent/UV/index arrays copied into fresh static meshes. `BakeMesh(..., true)` compensates transform scale; direct array assignment updates existing mesh GPU buffers. Keep baked mesh GUIDs.
- Wreck shader supports opt-in `_ALPHATEST_ON` across forward/shadow/depth/normals to preserve the source girder cutouts. Hull keeps the opaque variant; existing opaque wreck behavior remains unchanged.
- Registered Rebellion roster, ship data, asset mapping, existing View/Data Addressables, HUD/tooltip/icon generator, placement, matchups and dedicated ability/audio entries.

### Verification

- Source hashes unchanged. Eight models / 47 meshes retain triangle corners, UVs and bone parents. Blender max geometry error 0.000070222 source units; Unity max corner error 0.000002416 units and bone error 0.000001527 units; no parent mismatches.
- Saved static wreck bake matches the source rest pose exactly after reimport; no skinning data remains on the static meshes.
- Saved four prefabs have no missing scripts, broken references or donor model/data dependencies. Latest gameplay bindings: 12 weapons, 13 health/fog hardpoints, unique IDs 0..12. Registrations resolve to the MC80 assets; bounds and required renderer/hardpoint collections match.
- All eight living/wreck palettes rendered; blue and green inspected. Actual-model transparent 512 × 512 icon and silhouette are uncropped.
- Assets saved/imported; Unity reports compilation up to date with zero errors at commit preparation. Wreck shader reports zero compile messages; no asset import/serialization errors remain.
- The concurrent `TeamColorView` compile error reported during import is resolved; this task did not modify that file.
- No automated tests or Play Mode executed. Runtime combat, ability timing, placement, fog/selection and destruction behavior remain unverified.

## Edge Cases

- ALAMO global helper cleanup can fail on collision objects in older scenes after completing the new import. Cleanup only the new scene; do not edit the add-on or other scenes. Binary-verified roots already exist; no root repair needed.
- `RV_MONCALCRUISER_HP00_E.ALO` contains collision geometry only; it adds no visible engine pod. The main engine hardpoint and project engine VFX are used.
- Static import does not reproduce source death ALA, animated shader/refraction or proxy particles. `RV_MONCALCRUISER_D.ALO` supplies the damaged geometry; the separate HULK variant is unused.
- Render isolated geometry clones; do not instantiate uninjected gameplay scripts. Restore temporary palette/lighting state and preserve dirty user scenes.

## Files

- Source: `output/eaw-rebel-ships/DATA/ART/MODELS/RV_MONCALCRUISER.ALO` and `_D.ALO`; six `HP00_*` pod files and eight DDS textures.
- Tooling: `Tools/Blender/prepare_mon_cal_cruiser_textures.py`, `export_mon_cal_cruiser.py`, `README.md`.
- Art: `Assets/Art/{Models,Materials/Models,Textures/Models}/RebellionShips/MonCalCruiser/`.
- Prefabs: `Assets/Prefabs/Models/Ships/MonCalCruiser.prefab`, `MonCalCruiserShipView.prefab`; `Models/Wrecks/MonCalCruiserWreckView.prefab`; `Ui/Reinforcement/MonCalCruiserReinforcementView.prefab`.
- Data: `Assets/Settings/Data/Ship/MonCalCruiserShipData.asset`, `Ship/Wreck/MonCalCruiserWreckData.asset`, `Tooltip/Matchups/MonCalCruiserMatchups.asset`.
- `Temp/MonCalCruiserImport/` contains packed blends, exports, integration scripts, reports and palette renders; temporary files are excluded from Git. The original integration scripts record initial tuning; current saved assets include the later fixes.
