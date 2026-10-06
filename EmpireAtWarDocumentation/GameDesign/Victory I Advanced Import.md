---
type: reference
updated: 2026-10-06
tags:
  - alo
  - empire
  - ship-import
---
# Victory I Star Destroyer — Advanced Loadout

## Goal

- Import AOTR `EV_VSD_I.ALO` as Empire ship `VictoryIAdvanced = 202`.
- Preserve the existing `Victory = 200` assets and registration.
- Requested hull/shields/speed: `12,000 / 8,000 / 175`.

## Important Values

| Saved configuration | Value |
| --- | --- |
| Targetable weapons | 4 heavy artillery-rocket launchers, 4-shot bursts; 2 barrage-rocket launchers, 6-shot bursts |
| Targetable systems | Shield generator ×1; engines ×2; tractor beam ×1 |
| Other weapons | Light dual turbolasers ×6; heavy laser cannons ×4 |
| Hangar | Non-targetable; TIE-Interceptor squadron `203`; two total launches, one active |
| Hardpoints | 10 health targets; IDs `0–9`; 16 weapon emitters |
| Full Salvo `19` | Ordnance delays ×0.33; other weapon delays ×3; active 20 s / recovery 60 s |
| Tractor Beam `22` | Shared project ability: target speed ×0.4; up to 20 s; recovery 25 s; range 150 |
| Visible hull | `67.5321 × 38.1539 × 110` project units; bow +Z; up +Y |
| Banking / navigation | ±8°; vertical range `−20.4312 .. 19.4099`; navigation radius 70 |
| Hangar exit | `(−0.0443, −28.4312, −5.0887)`, below banked hull |
| Purchase | Source advanced variant: 12,250 credits / 245 s / level 2 / population 7; project cap 10 |

## Implementation

- Source: Workshop `1397421866`, `D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data`.
- Live XML: `E_Victory_Star_Destroyer_1_Fighters` inherits `T_Victory_Star_Destroyer_1`; same hull as standard loadout.
- Hull: `EV_VSD_I.ALO`; turrets `T_VSD_DTL_01.ALO .. 06.ALO` have identical hashes; six instances attached to `T_01 .. T_06`.
- Fighter: `EV_TIE_INTERCEPTOR_E.ALO`; reuse the shared imported TIE-Interceptor squadron: eight craft, 15 hull each, no shields.
- Restore identity `Root` only after binary bone audit confirmed importer removal. Preserve authored hidden meshes and UVs.
- Native Blender 3.6.23 MCP safe mode; isolated process/port `9880` avoids the other active conversion scene.
- Separate FBXs: hull and turret; cancel the turret FBX root's repeated coordinate rotation/scale when attaching beneath hull bones.
- Lossless DDS → PNG albedo conversion; opaque Ship Lit materials and additive effects. Source Colorize alpha produced all-white hull/turret masks.
- 2026-10-06: authored hull mask → four mirrored team stripes; neutral turrets; `_TeamRimStrength = 0` on hull/turrets and corresponding wrecks. `_TeamMaskStrength = 0` on turret materials; original albedo preserved. Red/blue top/angled Unity renders: team-colored surface `99.9% → 7.5%`; GUIDs/linear imports retained; no new import/serialization errors.
- `HangarComponent.isDestroyable = false` permits the non-targetable hangar outside the health list. Existing hangars default to `true`.
- New weapon profiles: `HeavyArtilleryRocket = 34`, `BarrageRocket = 35`, `LightDualTurbolaser = 36`; four heavy lasers reuse profile `28`.
- Heavy artillery rockets reuse the shield-piercing MassDriver damage matrix. Barrage rockets reuse the laser matrix. Existing projectile effects/audio reused.

### Hardpoint mapping

| Health IDs | Source attachment / muzzle | Saved role |
| --- | --- | --- |
| 0–3 | Missile_01..04 / MuzzleB_01,04,07,10 | Heavy artillery rockets |
| 4–5 | Torpedo_00,01 / MuzzleA_05,03 | Barrage rockets |
| 6 | HP_S_BONE_03 | Shield generator |
| 7–8 | HP_E_BONE_01,02 | Engines |
| 9 | HP_T_BONE | Tractor beam |
| 10–13, outside health | MuzzleC_00,01,04,05 | Heavy lasers |
| 14–19, outside health | T_01..06 / attached turret FB_00 | Dual turbolasers |
| 20, outside health | SPAWN_00 | Hangar |

## Decision

- Chosen: dedicated ship `202`; reuse shared Full Salvo, Tractor Beam and TIE-Interceptor assets.
- Why: distinct AOTR advanced variant and requested targetability; no replacement of the earlier Victory.
- Source-backed: supplied hull/shields/speed, composition, rocket burst counts/timing/damage, fighter complement, advanced variant economy.
- Provisional: 110-unit scale, movement/turn/banking relation, range conversion, projectile speed/effects, arcs, damage matrices, system durability, hangar timing, cap and matchups.
- Source barrage hardpoints have 500 HP; current per-type health data gives all six weapon targets 750 HP.
- Shared Tractor Beam settings differ from Victory's source multipliers: source target speed ×0.1 / caster speed ×0.8. Saved shared ability uses target ×0.4 and does not apply the caster penalty.

## Edge Cases

### Verification — 2026-10-06

- Hull source → FBX → Unity: 44 meshes / 21,749 triangles / 178 bones. Turret: 6 meshes / 1,138 triangles / 8 bones.
- FBX bone parents match. Blender maximum bone displacement: hull `0.000208286`; turret `0.0000015232` source units.
- Direct ALO/FBX position-UV comparison: hull maximum position difference `0.000299454`; turret `0.00000166893` source units; UV difference `0`.
- Unity attachment differences: hull `0.00000470913`; turret `0.0000000175651` project units at raw import scale.
- Own transparent 512×512 icon/silhouette, top/stern renders, Blender viewport and eight team palettes inspected.
- Saved health/weapon/hangar/selection/fog/team-color/explosion bindings inspected. Collider/shield/ion bounds fit new geometry; preview dimensions match.
- Ship data, Empire roster, Addressables, view/data mappings, all three icon consumers, own preview/wreck and Interceptor references resolve.
- Saved assets: no missing scripts or broken object references. Unity imported/saved assets; no new import, serialization or production compile errors.
- No automated tests or Play Mode run. Runtime acceptance remains in [[TODOs/Features/VictoryIAdvanced_Import]].

## Files

- `Assets/Prefabs/Models/Ships/VictoryIAdvancedShipView.prefab`, `VictoryIAdvanced.prefab`, `VictoryIAdvancedTurret.prefab`.
- `Assets/Settings/Data/Ship/VictoryIAdvancedShipData.asset`; `Ship/Wreck/VictoryIAdvancedWreckData.asset`.
- `Assets/Prefabs/Models/Wrecks/VictoryIAdvancedWreckView.prefab`; `Assets/Prefabs/Ui/Reinforcement/VictoryIAdvancedReinforcementView.prefab`.
- `Assets/Art/{Models,Materials/Models,Textures/Models}/EmpireShips/VictoryIAdvanced{,Turret}/`.
- `Assets/Art/Textures/Ui/Icons/ShipIcon/VictoryIAdvancedIcon.png`; `VictoryIAdvancedSilhouette.png`.
- `Assets/Settings/Data/Tooltip/Matchups/VictoryIAdvancedMatchups.asset`.
- `Tools/Blender/prepare_victory_i_advanced.py`; `output/aotr-empire-units/VictoryIAdvanced-Converted/`: blend/FBX, scripts, audits and renders.

## TODO

- Clean-skirmish acceptance: target selection/destruction, 4/6-shot salvos, Full Salvo rates, Tractor Beam lifecycle, Interceptor launches, fog/selection/shields, placement and death/wreck.
- Review provisional scale, movement, arcs, range, durability, damage/economy, matchups and launch timing.
- AOTR animated shaders, articulated turret aiming, wing animation and source death effects are outside this static conversion.
