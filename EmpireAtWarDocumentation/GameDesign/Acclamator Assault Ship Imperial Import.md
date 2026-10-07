---
type: reference
updated: 2026-10-07
tags:
  - alo
  - empire
  - ship-import
---
# Acclamator Assault Ship — Imperial Loadout

## Implementation

- Source: AOTR `E_Acclamator_Assault_Ship_Fighters` → `ev_acclamator.ALO`, plus six `T_DTL_Acc_01..06.ALO` turrets. `Temp/ALO_MODEL_MAP.txt` was missing; resolved through `output/aotr-empire-units/ship-catalog.csv` and live XML.
- Read [[Architecture/ALO_MODEL_IMPORT_GUIDE]] before conversion. Original seven ALO/seven DDS hashes unchanged.
- Empire `ShipType.AcclamatorAssault = 209`; user values: hull `5,000`, shields `2,500`, speed `40`.
- Four targetable hardpoints: two `PTL_00/01` assault launchers (`750 HP` each), `SG` shield generator (`500 HP`), `ENG` engine (`750 HP`). IDs `0..3` match health order.
- Non-targetable: four shared `HeavyLaser` mounts at `MuzzleA_00..03`; six existing `MissileInterceptorHardPoint` systems at `QTL_01..06_Turret/FB_02`. No hangar hardpoint.
- `AssaultMissile = 63`: four shots / `1 s` spacing / `15 s` reload / damage `30` / range `250`; shared concussion projectile/audio. Projectile speed/category/range conversion are project choices; no source defense debuff added.
- `AcclamatorBoostWeaponPower = 31`: active `20 s`, recovery `60 s`, fire delay ×`0.5`, speed ×`0.25`, shield regeneration ×`0`, damage taken ×`1.5`.
- TIE-Fighters `3` total / `1` active; TIE-Bombers `2` total / `1` active. Total launches include starting squadrons. Non-destroyable launch bays; launch exit `8` units below banked hull.

## Important Values

- Source-backed roster: cost `4,300`, build `86 s`, tech `3`, population `4`; regeneration `4.16/s`.
- Project choices: length `70`, navigation radius `43`, dimensions `40.38952 × 17.104553 × 70`, bank `20°`, Y range `−9.749215..8.552272`, turn/acceleration `30/30`, height tier `4`, launch delays `4/8 s`, queue limit `10`, intact-geometry wreck and matchup hints.
- Hull conversion: `12` meshes / `17,643` triangles / `125` bones. Each turret: `3` meshes / `1,616` triangles / `7` bones.
- Blender maximum corner/bone difference `0.000035479 / 0.000026703` source units; UV difference `0`. Unity attachment difference ≤`0.000000642` project units.
- Source alpha masks contain no paint. User requested two dorsal team-color stripes → `Livery.cs` bakes `AcclamatorAssault_HullStripeMask.png` (`2048 × 2048`, linear, `47,100` pixels / `1.123%` coverage); living/wreck hull materials share it. Original albedo/normal pixels unchanged.
- Brightness correction: all `14` living/wreck opaque materials use `_BaseColor = (0.5, 0.5, 0.5, 1)` and `_TeamRimStrength = 0`; albedo was already assigned. All `29` ownership bindings retained; eight live/wreck palettes and owned top views verified.
- Livery uses saved geometry; renders use an isolated preview scene and restore temporary palette/ambient state. Applied while the user's match remained in Play Mode.

## Files

- `Assets/Prefabs/Models/Ships/AcclamatorAssaultShipView.prefab` — gameplay prefab; `AcclamatorAssault.prefab` — geometry.
- `Assets/Settings/Data/Ship/AcclamatorAssaultShipData.asset` and `Ship/Wreck/AcclamatorAssaultWreckData.asset` — stats/dependencies.
- `Assets/Prefabs/Ui/Reinforcement/AcclamatorAssaultReinforcementView.prefab` — own identity-root placement preview.
- `Assets/Art/Textures/Ui/Icons/ShipIcon/AcclamatorAssaultIcon.png` — actual-model transparent `512 × 512` icon.
- `Tools/Blender/AcclamatorAssault/README.md` — source mapping, reproduction, tuning and verification. `Temp/AcclamatorAssaultImport/` — reports/blends/exports/previews.

## Edge Cases

- User targetability overrides the guide's general targetable-weapon rule. Only the two missile launchers, shield generator and engine enter `HealthComponent.ShipUnits`.
- Remove donor hardpoints outside the banking pivot; unpack the donor model pivot → no old hangar/weapons or Republic model dependency.
- Reset placement root position/rotation/scale → correct centered hologram geometry.
- Upstream ALAMO helper cleanup scans other scenes; complete its specific `removeShadowDoubles` failure only on the new scene. Preserve installed add-on and unrelated scenes.
- Saved asset references/registrations, counts, bays, ability, profile, placement/wreck and imported hierarchy verified; zero missing scripts/broken serialized references. No asset import/serialization errors observed.

## TODO

- Runtime acceptance: firing arcs/salvos, TIE launches, interception, ability, reinforcement placement and death/wreck behavior. No automated Unity tests or Play Mode run for this import.
- Playtest provisional project tuning. Source animation/proxy effects remain outside static conversion.
