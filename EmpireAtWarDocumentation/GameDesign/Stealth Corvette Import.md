---
type: reference
updated: 2026-10-06
tags:
  - alo
  - unity
  - republic
  - stealth
---
# Stealth Corvette Import

## Goal
- Republic IPV-2C Stealth Corvette: imported model, four weapons, cloaking, roster, own placement/wreck/icons and team colors.
- Guide: [[Architecture/ALO_MODEL_IMPORT_GUIDE]]; acceptance: [[TODOs/Features/StealthCorvette_Import]].

## Important Values
- User/local RaW XML: hull `850`, shields `900`, regeneration `15/s`; cost `2150`, build `18 s`, tech `3`, population `2`, limit `1`.
- Source armor `Corellian Gunboat`, starbase requirement `2`; no separately destroyable systems or hangar.
- Source cloak: active `80 s`, recharge `10 s`, manual cancel enabled.
- Project IDs: `ShipType.StealthCorvette = 11`, `ShipAbilityId.Cloak = 11`.

## Implementation
- Missile mounts `MuzzleB_00/01`: two `ConcussionMissile = 13`, ±45° yaw. Laser mounts: midpoints of `MuzzleA_00/01` and `MuzzleA_02/03`, two `Laser = 8`, full yaw.
- Mount IDs `0..3`; four weapon/fog bindings under banking body. Empty health hardpoint list uses existing whole-hull target/damage path.
- Shared laser: damage `6`, three shots/`0.1 s`, reload `1.5 s`, range `100`. Shared missile: damage `40`, four shots/`0.25 s`, reload `10 s`, range `75`; profiles unchanged.
- Republic roster, data/view mappings, existing View/Data Addressables, HUD/tooltip icons, placement and icon-generator mapping saved.
- Source matchups: strong Captor/Patrol Frigate/Munificent; weak Providence/Lucrehulk/bombers. All six tooltip icons resolve.
- Dedicated wreck uses intact opaque hull with project wreck shader; source death clone has no separate damaged geometry.

### Cloak
- `CombatModifiers.IsCloaked` owns state; `CloakAbility` starts/stops it through existing ability slots.
- Enemy rendering/minimap/radar/selection/tooltips/hardpoint overlays/cinematic selection and audio reject cloaked entities.
- Attack acquisition, queued shots, targeted abilities and cached attack/hunt/guard/attack-move pursuit reject cloaked targets.
- Cloaked caster cannot emit weapons or start other abilities. Friendly selection, movement and vision remain active.
- Friendly `CloakView` swaps the hull to `ShipCloak.mat` while cloaked and suppresses its shield surface; cancellation/expiry restores the original hull material and shield rendering. Enemy cloak remains fully hidden by `FogVisibilityComponent`.
- Ability button shows the active `80 s` countdown even though manual cancellation is available; decloaking starts the existing `10 s` recovery countdown and blocks reactivation.
- Existing reinforcement vision, clearance and enemy spawn-blocker rules still apply. Cloak grants no blocker bypass or new deployment zone.
- Already-fired projectiles/impacts remain valid; cloaking grants no invulnerability.
- Uses existing Invulnerability on/off/loop audio as provisional shared sounds.

## Decision
- Gameplay length `20` project units; centered size `1.863283 × 2.194350 × 20`, bow +Z/up +Y, prefab roots scale 1.
- Provisional speed/yaw/turn acceleration `42/24° per s/36`, bank `15°`, radar `175`, height tier `6`/Y `80`.
- Navigation radius `11`; banked Y `−1.117181..1.117181`; collider, shield, ion bounds and selection fitted to own hull.
- Generic `ShipClass.Corvette = 2` substitutes for unavailable Corellian Gunboat armor.
- Project exposes one build-level gate: `AvailableLevel = 3`. Separate source starbase-level requirement `2` is retained as provenance.
- These size/movement/weapon/armor/audio choices need balance acceptance; canonical meters are not project units.

## Edge Cases
- `stealth` is a duplicate `MeshShield.fx` effect shell; retained but disabled in Unity alongside Collision/Shadow.
- Referenced `EV_PHANTOM_SCAN_LINES.dds`/`EV_SCANLINES2.dds` are absent. Project cloak replaces source visibility behavior; EaW refraction/animated proxy effects are not reproduced.
- Four original meshes and all attachment bones remain. Duplicate `PE_Stealth_L/S` names retain Blender `.001` suffixes.
- Unity remaps disabled Shadow UV corners by ≤`0.000782482`; source blend/FBX corners remain preserved. Visible hull UVs match.

## Rules
- Verification: four meshes/`15184` triangles/`15` bones; identity Root restored only after binary verification. Bone parents match.
- Blender geometry/bone error ≤`0.000014949/0.000005245` source units; Unity geometry/bone error ≤`0.000000225/0.000000090` project units.
- Both `1024 × 1024` DDS→PNG conversions are pixel-identical; original ALO/DDS SHA-256 values unchanged.
- Hull alpha supplies linear team mask: `3.6116%` coverage. Living/wreck ownership and material opt-in verified; eight palettes rendered, blue/green inspected.
- Model icon/silhouette `512 × 512`, transparent and uncropped. Saved prefab references have no missing scripts, broken references or donor-model dependencies.
- Unity compilation/import checks pass. No automated tests or Play Mode run by this task.
- 2026-10-06 cloak follow-up: active countdown display fixed; friendly translucent hull/shield suppression saved. Visible/cloaked mesh previews inspected; saved renderer/material references valid; shader and Console error checks clean. Runtime timing/transition acceptance remains pending.

## Files
- Source: `F:/EaW/Republic_at_War_121_MINIMAL/Mods/Republic_At_War/Data/Art/Models/ReV_Stealthship.ALO`; XML `Data/XML/Units_Space_republic_stealthship.xml`.
- Credits: Warbnull mesh/textures/rigging, RaW `credits.txt`; full credits retained in sibling `ReV_Stealthship-Converted/`.
- Conversion: `Tools/Blender/prepare_stealth_corvette_textures.py`, `export_stealth_corvette.py`; audit/report/preview bundle in source sibling.
- Art: `Assets/Art/{Models,Materials/Models,Textures/Models}/RepublicShips/StealthCorvette/`.
- Visual/gameplay: `Assets/Prefabs/Models/Ships/StealthCorvette.prefab`, `StealthCorvetteShipView.prefab`; own reinforcement/wreck prefabs.
- Data: `Assets/Settings/Data/Ship/StealthCorvetteShipData.asset`, `Ship/Wreck/StealthCorvetteWreckData.asset`.
- Cloak: `Assets/Scripts/Services/ShipAbilities/Abilities/Cloak/`, `Assets/Scripts/Entities/BaseEntity/EntityDetection.cs`.

## TODO
- Authorized clean-skirmish check: spawn/placement, four weapons/shields, enemy and friendly cloak presentation, cached targets, cancellation/expiry/recharge, reinforcement rules and death/wreck.
- Review provisional movement/scale, shared weapon profiles, generic armor and audio.
