---
category: Features
type: reference
updated: 2026-10-10
source_workshop: "1770851727"
---
# ISD I Import

## Decision

- Empire `ShipType.ISDI = 201`: replace visuals with Workshop `1770851727` `Star_Destroyer`; existing registrations/GUIDs retained.
- User, 2026-10-09: keep current hull/shields/speed, economy and Interceptor/Brute/Punisher bays.
- Previous AOTR visual set removed on 2026-10-10: delete 24 unused records; move 39 shared assets to `Tector/Tector_AOTR*`, retaining their GUIDs/content. All 63 archive paths absent. Commit `327da32c` and [[GameDesign/ISD I Import - AOTR History]] preserve the original archive/history.

## Implementation

- XML chain: `Star_Destroyer → Star_Destroyer_Upkeep → Star_Destroyer_Template → Capital_Base → Vessel_Base → SFX_Base`.
- Hull `Empire_Imperial_SD.ALO`; structure `Empire_Imperial_SD_1.alo`; 13 gun attachments at their XML bones. Dedicated `Empire_Imperial_SD_DeathClone.ALO` wreck + source structure attachment.
- 24 weapons: 6 heavy turbolasers, 2 heavy turbo-ion, 2 medium turbo-ion, 3 medium triple turbolasers, 5 light turbolasers, 6 lasers. Existing profiles `2/31/32/4/33/8`; source mounts add 3 weapons over the former 21.
- All weapons targetable: IDs `0..23`; retained systems `24..29` = 2 shields, 2 engines, tractor, hangar. Health: weapons 750; shields/engines 1,000; tractor 1,500; hangar 2,000.
- Shield pair derived around `HP_SG`; outer engine geometry centers retained as two targets; tractor `HP_TRAC_BONE_00`; hangar `SPAWN_00`, exit 8 units below hull.
- Retain abilities `ImperialBoostEnginePower=21` and `TractorBeam=22`. Source Full Salvo/upkeep omitted to retain existing behavior/balance. Shared weapon/ability definitions untouched by replacement; source projectile/pulse/cone mappings recorded in evidence.
- Opaque Ship Lit surfaces, lossless albedo/linear normals with green flip, separate additive effects. Source UVs, original material slots/parameters and auxiliary maps preserved. Team stripes fit 10 source-material surfaces / 905 derived triangles; original geometry unchanged.

## Important Values

- Retained hull/shields/speed: `4500/4000/15`; regeneration `6.6666665/s`; range/delay `750/0.25 s`.
- Retained cost/build/level/population/max: `4500/40 s/3/5/3`. Navigation radius `97.998657`; bank ±5°.
- Bays: Interceptor `203`, Brute `204`, Punisher `205`; each reserve `2`, active `1`; initial/shared delays `4/30 s`. Fighter assets unchanged.
- Source values: hull `40000`, shields `15000`, regeneration `300/s`, speed `3 EaW units`, cost `18000`, build `70 s`, population `20`. No direct unit conversion applied to balance.
- Opaque hull/collider/placement: `94.26325 × 49.6460953 × 167.997711` project units; roots `1`, bow `+Z`, up `+Y`; banked Y `−24.82305 .. 25.089714`.

## Verification

- 16 Blender conversions; original visible binary triangle/material counts and UV-corner matching verified. Maximum geometry/bone error `0.000158/0.000099` source units; Unity bone error ≤ `0.000002346`; muzzle error `0`.
- 56 source hashes unchanged; 100 decoded texture-reference copies pixel-identical. Tector's 39 shared assets retain content/GUIDs/imported mesh IDs; 38 existing consumer files unchanged. Seventeen PNGs restored from verified original LFS objects.
- Active ISD I and Tector saved prefabs: no missing scripts/broken references. Deleted GUIDs have zero remaining serialized references. Active ISD I has zero obsolete dependencies; all 30 targets, ownership/fog/explosion/shield/hangar references and AI loadout verified.
- Existing ship/data/View Addressables, roster/matchups, HUD/tooltip/icon generator, placement `UnitSpawnView`, abilities, weapon profiles and fighter dependencies verified without duplicate registrations.
- 55 helpers / 67,130 triangles stripped from unit prefabs; gameplay shield retained. Imported source files keep helpers; ALAMO welds duplicate collision/shadow geometry only.
- Icons/silhouette/placement/top/stern and all 8 live/wreck palettes inspected. Cleanup verification: 18,217 saved-asset checks passed; imports idle and compilation-failed flag false. No new Console errors after LFS texture repair; earlier Console history retained.

## Files

- `Tools/Blender/ISDIRemake/README.md` — mappings, rebuild order, verification limits.
- `Tools/Blender/ISDIRemake/Evidence/`, `Previews/` — source/Unity reports and integrated ship screenshots.
- `output/remake-empire-units/ISDI-Converted/` — 16 editable packed Blender/FBX sets, textures and reports.
- `Assets/Prefabs/Models/Ships/ISDIShipView.prefab`, `Assets/Settings/Data/Ship/ISDIShipData.asset` — active unit.
- `Assets/Prefabs/Models/Wrecks/Source/ISDIShipView.prefab` — dedicated source wreck assembly.
- Plan: [[Done/Features/ISDI_Model_Replacement]].

## TODO

- Runtime acceptance remains unrun: firing/arcs, hangar launches, boost/tractor cleanup, shields, fog/selection, reinforcement and wreck timing. No automated tests or Play Mode were started by this task, per `ALO_MODEL_IMPORT_GUIDE`.
- Source ALA animations and animated EaW shader/refraction/proxy effects are not recreated; existing project effects and static source art are used.
