---
type: reference
updated: 2026-10-05
tags:
  - blender
  - unity
  - alo
  - republic
  - imperator
---
# Imperator Import

## Goal
- Import Republic RaW `EV_StarDestroyer_MKI.ALO` with intact/damaged visuals, 27 weapons and Laser Beam replacing Tractor Beam.
- Acceptance plan: [[TODOs/Features/Imperator_Import]]. Guide: [[Architecture/ALO_MODEL_IMPORT_GUIDE]].

## Decision
- User values override local RaW XML: build limit `1` (XML `3`), Y-Wings (XML ARC-170 bombers).
- Reuse `VenatorShipView` component setup; rebuild model-specific references. No new combat implementation.
- `ShipAbilityId.LaserBeam = 10` → existing `ProtonBeamSettings` and beam implementation/audio. Tractor hardpoint omitted.
- V-Wing type absent. User approved ARC-170/Delta-7 substitution; ARC-170 selected.
- StarDestroyer2 disabled by user request: remove its Republic build-roster entry. Player build UI and AI production both enumerate that roster.

## Important Values
- Hull/shields/regen: `7,500 / 3,500 / 50 per second`; regeneration interval `1 s`.
- Cost/build/population/tech/limit: `8,000 / 90 s / 4 / 3 / 1`.
- Weapons: `6 DualHeavyTurboLaser + 2 HeavyIonCannon + 14 TurboLaser + 5 IonCannon`; no point defense.
- `27 weapons + shield + engines + hangar = 30` destroyable targets, IDs `0..29`; all follow banking body.
- ARC-170s: `14` total launches / `4` active; Y-Wings: `8` total / `2` active. Saved ARC-170 view/data mappings resolve.
- Beam project tuning: `6,000 damage`, active `8 s`, recovery `50 s`, enemy range `500`; not RaW Tractor Beam values.
- Provisional movement: source speed `1.4` → project `14 units/s`; yaw/turn acceleration `5`, bank `5°`, range `250`, height tier `1`.
- Provisional hardpoint health: weapons/hangar `400`, engines/shield `600`; launch delay/interval `4 s / 8 s`.
- Living/placement bounds: `88.84087 × 48.69202 × 160`; root scale `1`, bow `+Z`, up `+Y`; navigation radius `81`.
- Banked hull Y: `−24.34601 .. +24.646`; launch exit `8 units` below hull. Wreck XML scale `1.3/3.5` → bounds `96.37397 × 53.09503 × 173.73288`.

## Implementation
- Portable Blender `3.6.23`; static ALAMO import. Restore only binary-verified identity `Root`: intact `168` bones, wreck `75`.
- Preserve intact `26 meshes / 34,974 triangles`; wreck `6 / 36,119`. Living `10` surfaces visible; collision/shadow/damage helpers retained disabled. Wreck hull/bridge/hull01 visible; shadows disabled.
- Six twins: `HP_Weapon_R1..3/L1..3`; heavy ions: R4/L4. Use paired FP muzzle bones.
- Turbos: `1C_Ext/Extra`, `1R/L`, `2R/L`, `MTBL01/02`, `MidR/L`, `R1/L1_TBL`, `R/L_TBL`. Actual ALO bones replace broken XML references; retain available muzzle pairs.
- Ions: `HP_Weapon_1C_Bone` + `FP_Weapon_1C_00/01`, `ION01..04`; shield/engine/hangar: `HP_Shield_Bone` / `HP_Engines_Bone` / `Spawn_00`.
- Opaque surfaces use Ship Lit; lights/engines use additive materials. Normals: linear normal import, green flip. Hull alpha → linear team mask, coverage `3.6773%`; team-mask strength `1`, hue livery `0`.
- Saved explicit shield/ion/health/fog/team/explosion/hangar bindings; own hologram placement and damaged-model wreck. Existing weapon/audio profiles reused.
- Register `ShipType.Imperator = 10`, Republic roster, ship/view mappings, existing View/Data Addressables entries, HUD/tooltip/roster icons, matchups and icon generator.

## Edge Cases
- Missing source `ISDI_shiplights.dds` affects only disabled `HP_Weapon_1C_Blast`; living surface textures are available.
- FBX vertex welding disabled, mesh compression off. Visible and non-shadow UVs match within `0.00001`; Unity remaps nearby UVs on four disabled shadow helpers. Authored UVs remain in blend/FBX.
- Death `_D_DIE_00.ala` and EaW shader/proxy animations are not converted.
- Blender intact bone/geometry error ≤`0.000669512 / 0.000111991` source units; wreck ≤`0.000092703 / 0.000145560`.
- Unity bone/corner error ≤`0.000013604 / 0.000004053` project units; all bone parents match. Both ALO/eight DDS hashes unchanged; all eight DDS→PNG conversions pixel-identical.
- Four saved prefabs have no missing scripts/broken references/donor model dependencies. Health/fog/weapon bindings `30/30/27`; registrations each resolve once.
- All eight living/wreck palettes rendered; blue/green inspected. Icons `512 × 512`, transparent, alpha bounds `(48,43)..(464,469)`, uncropped. No new import/serialization/compilation errors.
- No automated tests or Play Mode run. Static checks do not establish runtime acceptance; active plan retains that work.

## Files
- Source: `F:/EaW/Republic_at_War_121_MINIMAL/Mods/Republic_At_War/Data/Art/Models/EV_StarDestroyer_MKI.ALO` and `EV_Stardestroyer_MKI_D.ALO`.
- Bundle: sibling `EV_StarDestroyer_MKI-Converted/`; packed converted blends, source snapshots, FBXs, PNGs, reports, previews, scripts and full RaW credits.
- Credits: `Mesh-EvilleJedi | Texture-Evillejedi | Rigging-Warbnull` — RaW `credits.txt`, Star Destroyer row.
- Exporter/details: `Tools/Blender/export_imperator.py`, `Tools/Blender/README.md`; scripts use checkout-specific paths, fresh audited scenes and prepared textures.
- Art: `Assets/Art/{Models,Materials,Textures}/.../RepublicShips/Imperator/`; wreck materials `Assets/Art/Materials/Wrecks/Imperator/`.
- Prefabs: `Assets/Prefabs/Models/Ships/{Imperator,ImperatorShipView}.prefab`, `Models/Wrecks/ImperatorWreckView.prefab`, `Ui/Reinforcement/ImperatorReinforcementView.prefab`.
- Data: `Assets/Settings/Data/Ship/ImperatorShipData.asset`, `Ship/Wreck/ImperatorWreckData.asset`, `Tooltip/Matchups/ImperatorMatchups.asset`.
- Icons: `Assets/Art/Textures/Ui/Icons/ShipIcon/Imperator{Icon,Silhouette}.png`; baked shield `Assets/Art/Models/Shields/ImperatorShipViewShield.asset`.

## TODO
- Verify runtime weapons, beam, hangar, reinforcement placement and wreck/destruction with explicit authorization for runtime scene operations.
- Review provisional movement, weapon/system health, beam tuning, launch timing and combat balance.
