# Blender / Rothana conversion

## CIS Patrol Frigate — 2026-10-04

- Reference: Obsidian `GameDesign/Patrol Frigate Import.md`; active acceptance plan `TODOs/Features/Patrol_Frigate_Import.md`. `ShipType.PatrolFrigate = 106`, separate from Recusant.
- Source: `C:/Users/golin/Documents/CIS_Space_Units2014_checked/CIS_Space_Units/Recusant Patrol/CIS_Recusant_Patrol.ALO`; all five original ALO/DDS/ALA hashes unchanged. Credits: Evillejedi model, Evillejedi/Nawrocki textures, Nomada_Firefox rigging.
- Exporter: `Tools/Blender/export_patrol_frigate.py`, once on a fresh matching Blender import; create `Temp/PatrolFrigateImport/Output/Textures` first. Restores the identity Root verified in the 33-bone binary source; preserves duplicate attachment `PPTW_PTWSA2.001` and uses `EaWName` for validation-object suffix changes.
- Nine meshes, three visible, six disabled helpers. Source 7,021 triangles → FBX 6,989 → Unity 6,987; 34 zero-area faces removed, all 6,986 nondegenerate faces retained. Positions/UVs and bone names/parents checked; maximum Blender/Unity bone displacement 0.0000457764 source / 0.00000145024 project units.
- Art uses type-first `SeparatistShips/PatrolFrigate` folders. Visual/gameplay: `Assets/Prefabs/Models/Ships/PatrolFrigate.prefab`, `PatrolFrigateShipView.prefab`. Centered hull 11.37934 × 12.45770 × 81.09705 units; user size = 60% of existing Recusant length (135.16174 units). Uniform resize factor 3.119117 from the original 26-unit import; bow +Z, up +Y; root scale 1. Placement and wreck match; attachments, engine effect and hull volumes refitted.
- Eight shared Laser weapons use `TurboR01/02/03/04`, `LaserR01/02/03`, `LaserNR02`; engine/shield use `Engines_00`/`Shield_00`. Ten unique hardpoint IDs; all follow the body pivot. No hangar. User stats: hull/shields/regen 900/700/15; tech/cost/build/population 1/1,500/15 s/1; Power to Engines.
- Navigation radius 56.14411; banked vertical hull limits −6.22884 .. +6.22884 units. Provisional corvette tuning: speed 36, turn/acceleration 60, height 80, bank 30°, limit 20; weapon/engine/shield health 160/300/300. RaW Corellian Gunboat armor is not a separate project profile.
- Hull and normal DDS converted to PNG; normals use data import and green flip. All five materials remapped; the unnamed source material becomes Unity `Material`. Livery hue/range/saturation/strength 0.62/0.06/0.25/1, including wreck; all eight owned palettes rendered.
- Registered CIS roster, data/view mappings, existing Addressables groups, HUD/tooltip icons, matchups, own placement preview and future icon regeneration. Transparent icon 512 × 512, uncropped.
- `Recusant Patrol-Converted/` beside the source holds packed Blender source, FBX, PNGs, reports, palette renders and credits. Missing `W_LASER_SMALL.dds` affects disabled helpers only; damaged model/death ALA and EaW proxy/shader animations are not converted.
- Saved asset/reference/geometry/visual inspection and compilation/import completed without new errors; no missing scripts/references or donor model dependencies. No Play Mode or automated tests run; runtime acceptance and provisional balance remain in the active plan.

## Advanced Droid Bomber — 2026-10-04

- Import reference: Obsidian `GameDesign/Droid Bomber Import.md`; early-game Trade Federation / Advanced Droid Bomber, `SquadronType.DroidBomber = 101`.
- Source: `C:/Users/golin/Documents/CIS_Space_Units2014_checked/CIS_Space_Units/DroidBomber/Cis_DroidBomber.ALO`. Original ALO/DDS files are unchanged. Pack credits: Berruga (model), Berruga and Chris Boudreaux (textures), Nomada_Firefox (rigging).
- Exporter: `Tools/Blender/export_droid_bomber.py`, run once through Blender MCP on a fresh matching import. Expects 45 imported bones; restores the identity `Root` verified in the 46-bone binary source. Create `Temp/DroidBomberImport/Output/Textures` first.
- Conversion: 35 meshes, 46 bones and 28 visible meshes; preserve seven hidden collision/muzzle-flash helpers. Blender round trip preserves every nondegenerate triangle's positions and UVs; 306 zero-area source faces are dropped in Blender and another 39 in Unity, leaving 2,278 triangles. Bone displacement: Blender `0.000002563` source units; Unity `0.00000004391` project units.
- Unity visual/gameplay: `Assets/Prefabs/Models/Squadrons/DroidBomber.prefab`, `DroidBomberSquadronView.prefab`; per-bomber visible size `3.32159 × 0.93928 × 4.00000`, bow `+Z`, up `+Y`. FBX/materials/textures use `SeparatistShips/DroidBomber` in their type-first art folders.
- Source-backed roster/member values: tech 1, four bombers, cost 450, build 8 s, population 1, hull/shields 25/10; two lasers and one proton-torpedo launcher per bomber. Port/starboard lasers use `MuzzleA_01`/`MuzzleA_00`; launcher uses the midpoint of `MuzzleB_00..03`. All authored attachments remain.
- Provisional project tuning: cruise/combat 24/27 units/s, acceleration 18 units/s², turn 65°/s, bank 35°, formation spacing 4, navigation radius 10, height 11, limit 10; refresh 3 points every 1 s. Separate RaW Fighter shield resistance is not represented by the current common shield damage multiplier.
- Registered CIS roster, data/view lookup, existing Addressables groups, HUD/tooltip icons, matchup keys and four-member reinforcement preview. Team livery uses blue hue 0.67, range 0.08, minimum saturation 0.25, strength 1; all eight owned palettes rendered.
- `DroidBomber-Converted/` beside the source folder contains packed Blender source, FBX, PNGs, reports, previews and source credits. Missing `W_LASER_SMALL.dds` affects only disabled source helpers; runtime weapons use project effects.
- Saved asset/reference inspection, geometry checks, icon/palette/squadron renders and compilation completed. No missing scripts or references; no new Console errors after correcting serialized field types. No automated tests or Play Mode run; carrier garrisons were not changed.

## Installed setup

- Existing Blender: `C:\Program Files\Blender Foundation\Blender 4.2\blender.exe` — 4.2.16 LTS.
- Converter: `%LOCALAPPDATA%\AI-Tools\Blender\blender-3.6.23-windows-x64\blender.exe` — portable 3.6.23. The ALAMO importer uses pre-4.0 material APIs.
- [MCP for Blender](https://github.com/ahujasid/mcp-for-blender): package `mcp-for-blender==2.1.3`, add-on protocol 13. Installed in both versions; enabled and verified in 3.6.
- [ALAMO importer](https://github.com/AndrewFullard/Blender-ALAMO-Plugin): commit `2b0fb0e34e4f451d2e380b042d88ad1a4c8a09b5`, installed and enabled through MCP in 3.6. Add-on source is unchanged.
- Converter download SHA-256, verified against Blender's published checksum: `e3296eba7eab32c2e5182459ec7614af32224eee2bd32c9d0a08ffd751c54f3b`.
- `.codex/config.toml` is canonical. Agent JSON mirrors contain the same Blender connection. Safe mode enabled; telemetry disabled; socket binds only `127.0.0.1:9876`.
- Start the converter with `powershell -File Tools/Blender/Start-Blender.ps1`. The enabled add-on starts its listener automatically. Restart Codex after configuration changes to refresh its native tool list.

## Converted asset

- Source: `C:\Users\golin\Downloads\Rothana.2\Models\Rothana_Stardestroyer_Full_Armed.ALO`. Original files were not edited.
- Separate deliverables: `C:\Users\golin\Downloads\Rothana.2-Converted\` — `Rothana.fbx`, `Rothana.blend`, `Textures/`, conversion reports and Unity preview.
- Unity FBX: `Assets/Art/Models/RepublicShips/Rothana/Rothana.fbx`.
- Visual prefab: `Assets/Prefabs/Models/Ships/Rothana.prefab`. Contains geometry and attachment transforms, centered and scaled for gameplay.
- Gameplay prefab: `Assets/Prefabs/Models/Ships/RothanaShipView.prefab`. Republic ship with movement, health, weapons, hangar, selection, shields, fog visibility, team color, audio and wreck configuration.
- Materials: `Assets/Art/Materials/Models/RepublicShips/Rothana/` — 11 materials.
- Textures: `Assets/Art/Textures/Models/RepublicShips/Rothana/` — 16 PNGs converted from the referenced DDS images.
- FBX import scale: `0.02`, matching the existing Venator and Acclamator FBX imports. Raw visible size: approximately `4.918 × 1.783 × 9.680` Unity units; bow faces `+Z`, up is `+Y`.
- Visual prefab scale: approximately `16.53`; centered visible size: `81.289 × 29.477 × 160.000` Unity units. Gameplay root stays at unit scale. This is a project scale choice, not a canonical length in meters.

## Gameplay integration — 2026-10-03

- Source: Obsidian `GameDesign/Republic at War - Republic Unit Stats Reference.md`, Rothana entry. The note's version/provenance caveats still apply.
- `ShipType.Rothana = 7`; registered in `ShipsData`, `AssetMappingData`, Republic `FactionsData`, `ShipUiData`, `TooltipIconData`, and the existing Addressables `View`/`Data` groups.
- `RothanaShipData.asset`: hull `10,300`, shields `3,000`, Power to Shields and Power to Weapons.
- Destroyable systems: `18` weapons + shield generator + engines + hangar = `21` hardpoints. IDs and health/weapon/fog reference lists are assigned explicitly.
- User-approved fighter substitution: Delta-7 and A-Wing. Each bay has `3` reserves and `1` active squadron; `6` total launches, `2` simultaneous. Initial delay `4 s`; launch interval `8 s`.
- Provisional project balance: level `5`, price `7,500`, build time `35 s`, population `8`, maximum count `3`; movement `6.5 units/s`, rotation/turn acceleration `4.5`, navigation radius `95`.
- Inherited Venator tuning: shield regeneration `90` with delay `3 s`; weapon/hangar hardpoint health `400`, engine/shield hardpoint health `600`. These and hangar capacity are project choices; the reference does not supply them.
- Collision, shield, ion-effect bounds, selection marker and hangar exit fit Rothana. Baked hull range with ±15° banking: `-23.337 .. 23.858`.
- `RothanaWreckData.asset` references `Assets/Prefabs/Models/Wrecks/RothanaWreckView.prefab`; seven opaque hull renderers use dedicated wreck materials.
- `RothanaIcon.png`: transparent `512 × 512` render of the actual model, used by roster, ship UI and tooltips. `ShipIconGenerator` includes Rothana. Icon/wreck generation respects disabled helper renderers.
- Reinforcement placement uses `Assets/Prefabs/Ui/Reinforcement/RothanaReinforcementView.prefab`, registered under Rothana in `ReinforcementData`. Its ten visible meshes use the shared hologram material; its trigger bounds and orientation match the gameplay ship. Ship preview lookups require an explicit mapping instead of falling back to Venator.

### Hardpoint mapping

The ALO's turret names/layout differ from the stats reference. Gameplay types use the reference; positions use the supplied model's existing muzzles.

| Gameplay weapon | Count | Model attachment positions |
| --- | ---: | --- |
| DBY-827 heavy twin turbolaser | 8 | Six `MTL_01..06` muzzle-pair midpoints, plus `HTL01` and `HTL02` muzzle-pair midpoints |
| Turbolaser battery | 2 | `TL01`, `TL03` |
| Laser battery | 4 | `MuzzleC_01..04` |
| Point defense | 2 | `MuzzleC_05`, `MuzzleC_08` |
| Ion cannon | 2 | `MuzzleC_06`, `MuzzleC_07` |

- Shield generator uses `HP_Shield`; engine system uses the midpoint of `Eng01`/`Eng02`; hangar uses the `hangar` bone. Launch point clears the ventral hull.
- Side batteries use port/starboard firing arcs; dorsal heavy weapons have ±160° yaw coverage and point defense has full yaw coverage.
- Source weapon geometry is static; gameplay muzzle positions follow the banking body pivot.

### Integration verification

- Unity compilation completed without errors; no new import, serialization or console warnings/errors during integration.
- Reloaded saved assets: `18` weapons, `21` hardpoints, `10` visible mesh renderers and `7` wreck meshes; no missing scripts or broken serialized references.
- All five weapon types already have profiles in `WeaponsData`. New view/data addresses point to the correct assets; all three icon consumers reference the generated sprite.
- Inspected the generated icon. No Play Mode or automated tests were run; battle behavior and provisional balance remain untested. Existing unsaved scene changes were retained.
- Team-color material correction: rendered the model with the existing eight-color palette; inspected blue/green recoloring of the red stripes, with neutral hull panels retained. This is an isolated Editor rendering check, not a battle test.

## Conversion decisions

- Restored the identity `Root` bone removed by the importer, verified against the ALO binary header. Preserved all 100 bones and their hierarchy.
- Converted the importer's `CHILD_OF` constraints to bone parenting while preserving world transforms. No meshes merged, decimated or removed by the conversion script.
- The upstream importer welds duplicate vertices in collision/shadow meshes automatically. Geometry validation compares the imported ALO to the exported FBX.
- Retained `Shield`, `Col`, and `Shadow` meshes; their renderers stay disabled in the Unity prefab.
- `Hull_LOD_1` contains engine geometry. Both hull-named meshes remain visible; their names alone do not establish interchangeable LODs.
- Opaque surfaces use `EmpireAtWar/Ship Lit`; effects use additive URP particle materials. Original EaW animated shield, refraction, proxy particle effects, and shader logic are not recreated.
- Team livery: `Rothana_hull_1_Base`, `Rothana_hull_3_base`, `Rothana_engines` and their wreck materials use hue `0` (red), hue range `0.07`, minimum saturation `0.4`, strength `1`. `TeamColorView` supplies the owner's palette index to their renderers. Unowned previews retain the source red paint.
- Preserve these explicit livery settings after reimport. Automatic detection's `0.5%` texture-coverage cutoff misses the sparse red patches on hull 1 (`0.3%`) and engines (`0.4%`). Leave the yellow hangar markings and unpainted materials at livery strength `0`.
- Normal maps import as linear normal textures with the green channel flipped, matching the ALAMO importer's normal convention. No ALA animation files were supplied.

## Verification — 2026-10-03

- MCP initialization, code execution, add-on installation, import/export and viewport screenshot calls succeeded.
- Blender FBX round trip: 13 meshes, 42,813 triangles, 100 bones; all bone names and hierarchy retained. Maximum bone position difference: `0.0000511` source units. Bounds difference below `0.00003` source units.
- Unity imported 13 meshes and 42,813 triangles, with UVs on every mesh. All 100 bone transforms and parent names verified; maximum position difference `0.00000121` Unity units. Unity's vertex welding changes vertex counts.
- Inspected a rendered Unity preview; verified materials, texture references, hidden helper renderers, and saved asset metadata. No new import/serialization errors. No Unity automated test suites or Play Mode were run.

## Repeat this conversion

1. Use a fresh Blender 3.6 file with the ALAMO and MCP add-ons enabled. Import the Full Armed ALO with `bpy.ops.import_mesh.alo(filepath=SOURCE_PATH, importAnimations=False)`.
2. Create `Temp/BlenderConversion/Output/Textures` from the shell. Run `export_rothana.py` through MCP. It expects this specific original import and writes only to that staging folder.
3. Inspect the printed `ROTHANA_REPORT` and the separate `Rothana_FBX_Validation` scene. Blender appends `.001` to re-imported mesh names because both scenes share its global object namespace.
4. Reconnect Unity materials explicitly when importing another asset; do not assume FBX reproduces EaW shaders.

`mcp_call.py` is an MCP SDK client for sessions whose native tool list predates installation. It reads the canonical project configuration, including safe mode:

```powershell
uvx --from mcp-for-blender==2.1.3 python Tools/Blender/mcp_call.py request.json
```

Example request:

```json
{"tool":"execute_blender_code","arguments":{"code":"import bpy\nprint(bpy.app.version_string)"}}
```

For the saved export script, use `{"tool":"execute_blender_code","code_file":"F:/Private/empire-at-war/Tools/Blender/export_rothana.py"}`. Screenshot requests use `get_viewport_screenshot`; the client saves the returned image beside the request JSON.

## Captor-class Carrier — 2026-10-04

- Source: `C:/Users/golin/Documents/CIS_Space_Units2014_checked/CIS_Space_Units/Carrier/CIS_Carrier.ALO`; damaged ALO and death ALA remain separate. Original source files are unchanged.
- Editable packed Blender file, FBX, PNGs, source credits, reports and palette previews: sibling `Carrier-Converted/` folder.
- Exporter: `Tools/Blender/export_captor.py`, run once through MCP on a fresh intact Carrier import. It expects 63 imported bones and restores the identity `Root` verified in the 64-bone binary source.
- The importer does not resolve textures beside this source reliably; the exporter loads the hull DDS files explicitly. `Yellow_thruster.dds` comes from `CIS_Hero_Units_Pack_2014/Admiral Trench/`. Missing original hangar-shield effects use the existing project `Shields.mat` appearance.
- Blender round trip: 7 meshes, 6,438 triangles, 64 bones, UVs on every mesh; maximum bone displacement `0.00005928` source units.
- Unity: 7 meshes, 6,426 triangles, 64 bones; maximum bone displacement `0.000001051` units; no parent mismatches. Unity removes 12 verified zero-area faces: hull 2, hidden `Motor` 6 and `MotorSML` 4. `MotorSML` remains as an empty hidden helper.
- FBX import scale `0.02`; centered visual scale approximately `20.01525`; final size `65.293 × 44.256 × 140` project units. Bow `+Z`, up `+Y`, gameplay root scale `1`.
- Visual/gameplay/placement/wreck bounds agree. Banking ±10° gives hull range `−22.823 .. 22.128`; flight height `−118`, navigation radius `85`. These are provisional project choices.
- `ShipType.Captor = 105`; ship data, asset mapping, CIS roster, ship UI, tooltip icon, placement mapping and existing Addressables `View`/`Data` groups are registered.
- User-supplied values: hull `3,400`, shields `800`, regeneration `50`, cost `3,500`, build `30 s`, population `2`. Station level `3` follows the supplied Level 3+ build requirement; source Tech 2/5 variants have no distinct registration here.
- Provisional choices: maximum count `20`, speed `7`, yaw/acceleration `5`, shield regeneration delay `3 s`; weapon/hangar hardpoint HP `400`, engine/shield HP `600`, inherited wreck tuning.
- Hardpoints total **15**, matching the supplied itemized composition; its stated total of 14 is inconsistent. `TurboMR01/02` → 2 turbolasers; `LaserR01..06` → 6 lasers; `LaserR07/08` → 2 ions; `Shield_00`, `Engines_00..02`, `Spawn_00` → shield, 3 engines, hangar. Extra ALO turret attachments remain in the visual asset.
- All hardpoints follow `BodyPivot`; health/fog lists contain 15 unique IDs, weapons list contains 10. Launch `(0.015, −7.465, 78)` clears the collision box's forward edge by 8 units.
- Power to Weapons uses existing `BoostWeaponPower` tuning; original shield/engine tradeoffs and Victory/Frigate armor/shield types are not recreated by the current project data model.
- Hangar uses the user-approved temporary Belbullab-22 complement: 19 total launches (standing in for 11 Vulture + 8 Droid Bomber), maximum 2 active; first launch after 4 s, interval 8 s. Original fighter/bomber roles remain pending their squadron assets.
- Hull and wreck livery: hue `0.67`, range `0.08`, minimum saturation `0.25`, strength `1`. All eight palettes rendered; blue/green inspected. Icon is a transparent `512 × 512` render of the model.
- Saved assets have no missing scripts or broken references. Imports/compilation and post-save Console checks passed; no automated tests or Play Mode were run. Pending acceptance is tracked in Obsidian `TODOs/Features/Captor_Import.md`.
- Credits: model `Evillejedi`; textures `Evillejedi`, modified by `Nawrocki`; rig `Nomada_Firefox`. Pack README asks contacting its author before public-mod use.
