# AOTR XQ-3 Empress defense platform

- Source: AOTR Workshop `1397421866`, `RB_Empress_Station_lvl3.ALO`, `SpecialStructures_Space.xml`, `R_Defense_Empress` (Rebel).
- One hull model. No faction station levels or upgrade mappings changed.
- Shared defense platform: `DefendPlatformType.Empress = 3`; existing four faction rosters, shared catalog, factory, Addressables and placement mapping.
- User balance: **6,000 hull / 24,000 shields** = **60% hull / 2× shields** of the existing Golan III's 10,000 / 12,000.
- Weapons: **3** heavy three-burst long-range turbolasers, **3** heavy three-burst long-range turbo-ions, **6** medium long-range dual turbolasers, **12** light dual turbolasers, **6** heavy dual lasers. All 30 weapons + 3 source shield generators are targetable.
- XML supplies 41 hardpoints, including 6 fighter bays and 2 hull helpers. Preserve their anchors; no fighter complement, abilities or death clone added.

## Assets

| Asset | Path |
| --- | --- |
| Separate station visual | `Assets/Prefabs/Models/Stations/AotrEmpressStation.prefab` |
| Gameplay | `Assets/Prefabs/Models/DefendStation/AotrEmpressDefensePlatformView.prefab` |
| Data | `Assets/Settings/Data/Models/DefendPlatform/AotrEmpressDefensePlatformData.asset` |
| Placement | `Assets/Prefabs/Ui/Reinforcement/AotrEmpressDefensePlatformReinforcementView.prefab` |
| Shield | `Assets/Art/Models/Shields/AotrEmpressDefensePlatformViewShield.asset` |
| FBXs | `Assets/Art/Models/SpaceStations/AotrEmpressStation/` |
| Materials | `Assets/Art/Materials/Models/SpaceStations/AotrEmpressStation/` |
| Textures | `Assets/Art/Textures/Models/SpaceStations/AotrEmpressStation/` |
| Icon, 512×512 RGBA | `Assets/Art/Textures/Ui/Icons/Ui/AotrEmpressDefensePlatformIcon.png` |
| Portable blend/FBX/PNG package | `output/aotr-space-stations/Empress-Converted/` |
| Audit, geometry and saved-reference reports | `Temp/AotrEmpressStationImport/` |

## Conversion and material decisions

- Isolated headless Blender **3.6.23** with the installed ALAMO importer. User Blender session and preferences preserved.
- Reuse `RebelSpaceStation/Convert.py` with source-specific adaptations. Restore the binary-verified identity Root, source-index bone parents, every material slot and authored normals.
- ALAMO leaves root-attached meshes without CHILD_OF constraints. Rebuild their parenting from binary connection indices too.
- **25** FBXs/blends = main hull + **24** separate artwork files. All XML attachment matrices and inherited hardpoint definitions resolved.
- Main model: **238 bones / 22 meshes**; gameplay-visible base geometry **16 meshes / 48,648 triangles**. Complete visible artwork: **64 meshes / 75,540 triangles**.
- **18** referenced DDS files → **18** lossless RGBA PNGs, plus **4** direct-alpha team masks. Missing base-game members extracted only into the task workspace.
- All **20** source material signatures remapped explicitly. Opaque hull: `EmpireAtWar/Ship Lit`; additive lights and static hangar energy field: existing `Victory_Lights.mat` convention.
- Preserve source Diffuse/Color RGB per slot; source alpha has shader-specific meaning and is not copied as opaque material opacity. Hull source alpha supplies team masks; livery/rim strengths 0.
- `Hullplates_diffuse` alpha coverage **5.592823%**; other three mask textures contain zero alpha. No invented paint mask.
- Normal maps: linear Normal Map, green flipped; textures uncompressed. Source shader + parameters retained in audit/Blender metadata.
- FBX scale **0.02**, nested visual scale **16.1197281**, visual/gameplay roots identity. Chosen maximum XZ diameter **300** units, matching Golan III; complete bounds **300 × 207.398346 × 266.3863** units.
- Collider, selection marker, ion volume, renderer lists and 1,024-plane shield rebuilt for the final model.
- Dedicated weapon profiles **67 / 68 / 69**; existing medium dual **53** and light dual **36** reused. Triple profiles preserve source 150 / 157.5 damage, 3 pulses, 7.5 / 8.5 s interval, mean recharge 15.25 / 17.25 s and range 9,000 × 0.05 = 450 units.
- Heavy dual laser: 35 source combined damage represented by two 17.5 shots; mean recharge **3.8705 s**, range **150**. The **0.08 s** shot separation and existing projectile art/damage matrices are project conventions.
- Source price **7,500 credits**, build **250 s**, population **3**, starbase level **1**, limit **2**. Regeneration, hardpoint health, attack range and gameplay size remain project choices inherited from the station pipeline.

## Rebuild

Read repository `AGENTS.md` and the vault placement/import guides first. Preserve the current station/platform files and asset mappings in `Temp/AotrEmpressStationImport/PreservationBaseline.json` before rebuilding if running the preservation check.

```powershell
uv run --with pillow python Tools/Blender/AotrEmpressStation/Prepare.py
& "$env:LOCALAPPDATA/AI-Tools/Blender/blender-3.6.23-windows-x64/blender.exe" --background --python Tools/Blender/AotrEmpressStation/Convert.py *> Temp/AotrEmpressStationImport/Conversion.log
python Tools/Blender/AotrEmpressStation/RestoreNormals.py
& "$env:LOCALAPPDATA/AI-Tools/Blender/blender-3.6.23-windows-x64/blender.exe" --background --python Tools/Blender/AotrEmpressStation/PackBlends.py
& "$env:LOCALAPPDATA/AI-Tools/Blender/blender-3.6.23-windows-x64/blender.exe" --background --python Tools/Blender/AotrEmpressStation/VerifyBlend.py
python Tools/Blender/AotrEmpressStation/Stage.py
unity command run_script --file Tools/Blender/AotrEmpressStation/BuildArt.cs --timeout_ms 180000 --timeout 180 --json
unity command run_script --file Tools/Blender/AotrEmpressStation/BuildPlatform.cs --timeout_ms 180000 --timeout 180 --json
unity command run_script --file Tools/Blender/AotrEmpressStation/Render.cs --timeout_ms 90000 --timeout 90 --json
unity command run_script --file Tools/Blender/AotrEmpressStation/Register.cs --json
unity command run_script --file Tools/Blender/AotrEmpressStation/Verify.cs --timeout_ms 90000 --timeout 90 --json
uv run --with pillow python Tools/Blender/AotrEmpressStation/VerifyGeometry.py
python Tools/Blender/AotrEmpressStation/Package.py
```

- Check nested `data.result.success` for Unity scripts. Blender can exit 0 after a Python exception; check logs and expected report files.
- Shared source geometry/UVs remain unchanged. Raw FBXs retain helpers; saved visual prefab strips disabled helper renderer/mesh components.
- Every editable blend was reopened and checked for packed external images, source mesh count, UVs and bone parenting.

## Verification — 2026-10-09

- Source → Blender → FBX and Unity original-binary corner checks pass for all 25 models.
- Maximum raw Unity error: vertices **0.00000246403** units; bones **0.00000202228** units; UV **0.0000966974**; normal vector **0.000000265696**. Normal restoration avoids FBX's authored-normal changes.
- All source material slots, per-submesh triangle counts, 24 artwork poses, 33 saved gameplay mounts, renderer bindings, factory paths and asset references pass after save/reload.
- No missing scripts/broken references; 30 weapons, unique target IDs, health ratios, shared roster, own placement/icon and existing Addressables registrations checked.
- Original ALO/DDS/inherited XML hashes unchanged. **739** existing station/platform asset files unchanged; every pre-existing asset mapping preserved.
- Unowned + eight owned palette previews rendered in Unity. Color changes affect **769–9,055** pixels of a 900×900 image (>4/255 channel change), preserving neutral hull regions.
- Unity compilation completed successfully. No Empress import/serialization errors. Existing Balance Editor `RememberScroll` errors were observed and left outside this task.
- No automated Unity test suite or battle playthrough run.

## Conversion limitations

- Static artwork: no ALA animation, turret articulation, proxy particles, fighter garrison, source Defensive Shield ability or death-clone conversion.
- EaW shader scrolling/refraction/damage effects and normal-alpha specular response are approximated by project hull/light materials. Gameplay shields use the project's fitted shield component.
- ALAMO welds/removes degenerate shadow/collision helper geometry. Helpers remain in blends/FBXs; source equality checks focus on visible geometry.
- Source texture slots, UVs and surface geometry are retained; no display UV repair performed.
- Runtime combat/placement acceptance and balance are not established by asset checks or preview renders.
