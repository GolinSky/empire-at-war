# AOTR Golan III defense platform

- Source: installed AOTR `SB_Golan_3.ALO`, `SpecialStructures_Space.xml`, template `T_Defense_Golan3`, Empire variant `E_Defense_Golan3`.
- Same physical model as AOTR's Rebel skirmish Golan III. No converted FBX/material library existed at import time; the earlier `output/aotr-space-stations/preview-selection/02-SB_Golan_3.png` was only a preview.
- Shared FBXs: `Assets/Art/Models/SpaceStations/AotrGolanIII/`; textures/materials: `Assets/Art/{Textures,Materials}/Models/SpaceStations/AotrGolanIII/`. Future station variants should reuse this artwork.
- Visual prefab: `Assets/Prefabs/Models/DefendStation/AotrGolanIIIDefensePlatform.prefab`.
- Gameplay prefab: `Assets/Prefabs/Models/DefendStation/AotrGolanIIIDefensePlatformView.prefab`.
- Data: `Assets/Settings/Data/Models/DefendPlatform/AotrGolanIIIDefensePlatformData.asset`.
- Placement: `Assets/Prefabs/Ui/Reinforcement/AotrGolanIIIDefensePlatformReinforcementView.prefab`.
- Icon: `Assets/Art/Textures/Ui/Icons/Ui/AotrGolanIIIDefensePlatformIcon.png`.

## Decisions

- User balance overrides XML: hull **10,000**, shields **12,000**; 12 heavy dual turbolasers, 4 heavy assault missile launchers, 12 heavy proton torpedo launchers, 8 laser cannons.
- `DefendPlatformType.GolanIII = 2`; shared catalog and all four existing faction rosters. Existing XQ-6 and faction station routing remain intact.
- The template contains 67 hardpoints and 24 separately attached models. Preserve all 24 source artwork pieces; the 12 light-turbolaser artwork pieces are decorative for the requested gameplay loadout. No extra light/medium turbolaser gameplay weapons are added.
- 36 weapons and 3 source shield generators are targetable. No fighter complement, abilities, death clone or animated source shaders are added.
- Source purchase values: price 8,250 credits, build time 275 s, population 3, level 1. The source per-planet limit 2 becomes the project's roster limit 2.
- Project choices: maximum XZ diameter 300 units; root identity; radial weapon yaw with source cone widths; donor hardpoint health and shield regeneration; structure attack range 500. These are not AOTR scale/balance equivalence claims.
- Existing heavy long-range dual turbolaser profile 50 and laser profile 8 are reused. New profiles 65/66 retain source projectile damage (50/150), pulses (3/1), interval (1.5/0 s), reload (15/10 s). Source ranges use the established 0.05 conversion (300/200); existing projectile art, speeds and damage matrix are reused.
- Hulls use `EmpireAtWar/Ship Lit`, source-alpha team masks, no rim/livery tint, green-flipped authored normal maps. Additive surfaces follow `Victory_Lights.mat`. Every raw FBX material slot is explicitly remapped.
- Raw FBXs/blends retain helper geometry. The visual prefab removes disabled helper renderer/mesh components and restores repeated original bone names; transport FBXs retain unique names. Source geometry and UVs are not repaired or replaced.

## Rebuild

Read `AGENTS.md` and the vault placement/import guides. Use the installed Blender 3.6.23 with ALAMO enabled. The headless conversion is isolated from the user's running Blender session; it does not save preferences.

```powershell
uv run --with pillow python Tools/Blender/AotrGolanIII/Prepare.py
& "$env:LOCALAPPDATA/AI-Tools/Blender/blender-3.6.23-windows-x64/blender.exe" --background --python Tools/Blender/AotrGolanIII/Convert.py *> Temp/AotrGolanIIIImport/Conversion.log
python Tools/Blender/AotrGolanIII/RestoreNormals.py
python Tools/Blender/AotrGolanIII/Stage.py
unity command run_script --file Tools/Blender/AotrGolanIII/BuildArt.cs --timeout_ms 180000 --timeout 180 --json
unity command run_script --file Tools/Blender/AotrGolanIII/BuildPlatform.cs --timeout_ms 180000 --timeout 180 --json
unity command run_script --file Tools/Blender/AotrGolanIII/Render.cs --timeout_ms 90000 --timeout 90 --json
unity command run_script --file Tools/Blender/AotrGolanIII/Register.cs --json
unity command run_script --file Tools/Blender/AotrGolanIII/Verify.cs --timeout_ms 90000 --timeout 90 --json
uv run --with pillow python Tools/Blender/AotrGolanIII/VerifyGeometry.py
```

- Inspect nested `data.result.success` after every Unity script.
- `Prepare.py` resolves actual DDS references case-insensitively. Only missing base-game textures are extracted from `textures.meg` into the task workspace; source files stay untouched.
- Converter reuses the audited station workflow, reconstructs source-index bone parents, repairs ALAMO material slots, restores authored normals and preserves mesh-to-bone transforms. Neighbouring position buckets handle FBX corner rounding efficiently.
- `RestoreNormals.py` restores exact source vertex normals in the binary FBX before Unity imports them.
- `Temp/AotrGolanIIIImport/` contains per-model editable blends, conversion reports, source audit, raw Unity geometry, saved-reference verification, source comparisons and nine Unity previews.
- Portable base blend: `output/aotr-space-stations/GolanIII-Converted/AotrGolanIII.blend`.
- On a fresh checkout, capture the existing station assets/mapping in `Temp/AotrGolanIIIImport/PreservationBaseline.json` before registration if running the preservation audit. The import scripts themselves do not modify existing station art or mappings.

## Verification — 2026-10-09

- 25 FBXs; 403 base source bones; 79 base visible meshes; 24 attachments; 125,592 visible source triangles total.
- Geometry/UV/normal checks compare Unity triangle corners directly with original ALO binaries. Source hard-edge normals are compared among coincident geometry/UV corners.
- Maximum raw Unity displacement: vertices `0.000006950` units; bones `0.000003358` units. Maximum UV delta `0.000074751`; normal-vector delta `0.0000003724`.
- 21 referenced DDS textures decode losslessly into 21 PNGs; 7 additional alpha team masks. Original ALO/DDS/XML hashes unchanged.
- All original submesh triangle counts and texture slots pass; all 24 XML artwork poses pass. Gameplay and placement reference the separate visual prefab and its shared source library.
- 36 requested weapons / 39 targetable hardpoints; health, fitted 1,024-plane shield, catalog, four rosters, placement mapping, icons and Addressables verified after save/reload.
- Existing asset mappings retained; 212 existing station asset files unchanged. Unity compilation/import completed; no new import/serialization errors.
- Rendered unowned and all eight team palettes; inspected unowned and contrasting blue/green previews. No automated Unity test suite or battle playthrough was run.
