---
category: Features
status: done
completed: 2026-10-24
---
# Invincible Import

## Goal

- Add Republic at War `SeV_Invincible.ALO` as a separate Separatist ship.
- Existing Providence size × `39/14`; hull `10000`, shields `6000`, `28` weapons. Keep one engine, shield-generator and hangar.

## Decision

- Source: Workshop `1129810972`, `Admiral_Trench` → `SeV_Invincible.ALO`; source ALO/DDS/XML unchanged.
- Preserve unspecified Providence movement, regeneration, range, hardpoint health, abilities, hangar and roster economy.
- Import scope: static conversion, complete saved assets/registrations and visual/reference verification. No Play Mode combat, automated test suite or packed content build executed.

## Implementation

- [x] Convert source and textures; verify raw bone parents/world positions, FBX corners/UVs and source hashes.
- [x] Create own visual/gameplay/data/shield/wreck/placement/icon/matchups and complete all Separatist registrations.
- [x] Verify saved references, weapon counts/IDs, health, sizes, banked bounds, shader planes, lifecycle and faction routing.
- [x] Render eight live/wreck palettes, icon, hologram and actual-size comparison; inspect blue/green colors and framing.

## Important Values

- `ShipType.Invincible = 109`; hull/shields `10000/6000`.
- Weapon counts: type `3` × `12`, type `4` × `4`, type `5` × `8`, type `11` × `4`; total `28`. Health targets `31`, unique IDs `0..30`.
- Visible size `59.28171 × 109.37660 × 318.23566`; Providence length `114.23844`; ratio `2.785714`.
- Bank `±10°`; hull Y `−54.68830..54.68830`; navigation radius `162`; centered visual, root scale `1`.
- Transport `7` meshes / `21421` triangles / `85` bones. Visible hull/lights `9810/836` triangles; source-alpha team mask coverage `4.52621%`.
- FBX max corner error `0.000190735` source units, UV error `0`. All `64263` Unity corners checked: position error ≤ `0.000008021` units; visible UV error `0`; bone error ≤ `0.000005812` units.
- Verification passed: `VerifyInvincible.Main`, `VerifyInvincibleUnityGeometry.Main`; zero Console errors. MainMenuScene remains clean; source Providence asset hashes unchanged.

## Edge Cases

- Source repeats damage-bone names; map source indices to unique imported names before restoring parents. Identity Root repair is binary-backed.
- Upstream importer welds shadow helpers. Unity remaps `83` Shadow UV corners by ≤ `0.000994811`; helpers/weapon-flash planes remain in transport and are absent from runtime geometry.
- Providence places both full-arc turbolaser templates on its left. Invincible mirrors the right template's target, retaining its arc/profile; all `28` weapon mounts use distinct authored sockets.
- Runtime combat/balance remains unverified. Packed Player builds require the normal Addressables content rebuild.

## Files

- Tools and durable reports: `Tools/Blender/Invincible/`; full source/FBX/blend/corners/previews: `Temp/InvincibleImport/`.
- Model: `Assets/Art/Models/SeparatistShips/Invincible/Invincible.fbx`.
- Gameplay: `Assets/Prefabs/Models/Ships/Separatist/InvincibleShipView.prefab`.
- Data: `Assets/Settings/Data/Ship/Separatist/InvincibleShipData.asset`.
- Reference: [[AssetPipeline/Imports/Invincible Import]].
