# Blender / Rothana conversion

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
