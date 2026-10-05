---
type: reference
updated: 2026-10-05
tags:
  - rebellion
  - alo
  - asset-pipeline
---
# Corellian Corvette / CR90 Import

## Goal
- Register vanilla `RV_CORVETTE.ALO` as a Rebel anti-fighter / anti-bomber corvette.
- Source guide: [[Architecture/ALO_MODEL_IMPORT_GUIDE]]. User-supplied stats are authoritative for this import.

## Important Values
- `ShipType.CorellianCorvette = 301`; `ShipAbilityId.CorellianCorvettePowerToEngines = 14`.
- Hull `750`; shields `600`; regeneration `15/s` with `1 s` refresh interval.
- Level `2`; price `1,250`; build `15 s`; population `2`; provisional queue limit `10`.
- User-approved movement conversion: speed `2.45 × 10 = 24.5 units/s`; yaw `1.25 × 10 = 12.5°/s`.
- Provisional acceleration `24°/s²`; bank `15°`; hull length `30 units`; size `12.72498 × 10.07087 × 30`; radius `17`; radar `175`; height tier `6` / Y `80`.
- Power to Engines: active `20 s` → recovery `50 s`; speed `×2`, weapon delay `×3`, shield regeneration `×0`; damage unchanged. Cannot cancel; AI use `Escape`.
- Eight `WeaponType.Laser` mounts, IDs `0..7`, mapped one-to-one to `MuzzleA_00..07`; all follow the banking body. No engine/shield hardpoints or hangar.
- Shared Laser profile unchanged: damage `6`, three-shot salvo, interval `0.1 s`, reload `1.5 s`, range `100`. Laser damage multipliers: fighter/bomber `1.5`, frigate `0.5`, capital `0.25`. Weapon tuning and `360°` arcs are project choices.

## Implementation
- Source: `output/eaw-rebel-ships/DATA/ART/MODELS/RV_CORVETTE.ALO`; dedicated wreck: `RV_CORVETTE_D.ALO`.
- Blender `3.6.23` via existing MCP; ALAMO completed model parsing but failed during global multi-scene helper cleanup. Completed cleanup only on this import; other scenes and installed add-on remain unchanged.
- Retained binary-verified identity Root and parented `Corvette_Parent` to it. Preserved geometry, UVs, names, attachments and authored visibility.
- Intact: `13` meshes / `3,684` triangles / `17` bones after normal helper welding. Only `Corvette` renders; authored engine-effect mesh, flashes, shield, collision and shadow remain disabled.
- Damaged: `3` meshes / `2,446` triangles / `12` bones; dedicated wreck shader/material uses all three damaged meshes at the living scale/offset.
- Textures: lossless `RV_CORVETTE.DDS`, `RV_CORVETTE_BUMP.DDS`, `W_LASER_SMALL.DDS` → PNG. Hull/normal `256 × 256`; flash `128 × 128`. Inverse hull alpha provides the linear team mask (`85.6293%` nonzero coverage).
- Living `EmpireAtWar/Ship Lit` and `EmpireAtWar/Ship Wreck` materials have explicit normal/mask maps; normal import flips green. Both ownership bindings and material opt-in verified.
- All mappings saved: Rebel roster, ship data, view/data asset mappings, existing View/Data Addressables groups, own reinforcement preview, roster/HUD/tooltip sprite, matchups, future icon generation and ability audio.

## Decision
- Chosen: shared Laser combat profile; no new combat logic. Eight source muzzles match the supplied weapon count.
- Hardpoints (2026-10-05, user request): all eight Laser mounts are targetable `Weapon` hardpoints in `HealthComponent.ShipUnits`, `160` HP each, `hullDamageMultiplier 1`, destroyed explosion on. Deviates from vanilla `HARDPOINTS.XML` (`Is_Targetable No`).
- Attack range = radar `Range` `175` (`useWeaponDamageRange` removed project-wide 2026-10-05). Play Mode: destroys a C-9979 at ~`140` units.
- Avoid: changing shared engine-boost balance for other ships, enabling authored hidden effect helpers, donor ship registrations or inherited geometry references.

## Verification
- Original ALO/DDS hashes unchanged; DDS→PNG pixels identical.
- Blender FBX round trip: maximum corner displacement `0.000110729` source units (living), `0.0000133514` (wreck); UVs preserved. Bone error `0.0000171662` / `0.0000118054` source units.
- Unity corner error ≤`0.000000475` project units; bone error ≤`0.000000303`; no parent mismatches; all mesh triangle totals retained. Disabled shadow UV difference ≤`0.000000042`.
- Reloaded four saved prefabs: roots at scale `1`, centered visual/gameplay/placement bounds match; zero broken references/missing scripts/donor dependencies; exactly eight Laser mounts and eight weapon/fog bindings.
- Transparent `512 × 512` icon/silhouette uncropped; all eight living/wreck palette renders created; blue/green inspected.
- Unity imports/compilation and Console checked; no CR90 import/serialization errors. The known unrelated `CoreGameUiController.LateDispose → BaseUi.SetParent` teardown error recurred while the shared Editor was in use.
- No automated tests or Play Mode run. Runtime combat, engine boost, placement and destruction behavior remain unverified.

## Files
- `Tools/Blender/prepare_corellian_corvette_textures.py`, `export_corellian_corvette.py` — model-specific audit/conversion.
- `Assets/Art/Models/RebellionShips/CorellianCorvette/` — intact/damaged FBX; matching material and texture folders under `Assets/Art`.
- `Assets/Prefabs/Models/Ships/CorellianCorvette.prefab`, `CorellianCorvetteShipView.prefab` — visual/gameplay.
- `Assets/Prefabs/Ui/Reinforcement/CorellianCorvetteReinforcementView.prefab`; `Assets/Prefabs/Models/Wrecks/CorellianCorvetteWreckView.prefab`.
- `Assets/Settings/Data/Ship/CorellianCorvetteShipData.asset`; `Ship/Wreck/CorellianCorvetteWreckData.asset`.
- `Temp/CorellianCorvetteImport/` — conversion reports, saved-reference inspection, geometry comparisons, integration scripts and palette renders.

## TODO
- Check clean-skirmish combat, Power to Engines activation/recovery, shields, banking, fog/selection, reinforcement and wreck behavior when runtime verification is requested.
- Review provisional gameplay size, range, acceleration, firing arcs and queue limit.
- EaW ALA/shader/proxy animations were not converted; static damaged geometry uses the existing wreck system.
