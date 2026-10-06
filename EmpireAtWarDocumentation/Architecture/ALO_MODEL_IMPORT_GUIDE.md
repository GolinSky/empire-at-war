---
type: reference
updated: 2026-10-03
tags:
  - blender
  - unity
  - alo
  - asset-pipeline
---
# ALO Model Import — Blender to Unity

## Goal

- Convert an Empire at War ALO into a verified FBX and a fully registered Unity ship: visuals, gameplay data, hardpoints, hangar, placement preview, wreck, icons, faction entry and team colors.
- Use Rothana as the working example. Conversion and integration are recorded in commit `73bcb9b7`; verified on 2026-10-03. A successful FBX import alone does not complete ship integration.

## Rules

- Read [[Architecture/PROJECT_ORGANIZATION]] before creating asset folders; read [[Rules/UI_UX_GUIDELINES]] before changing UI assets and [[Rules/UI_CODE_BUILD_GUIDE]] for new UI features.
- Keep original ALO/DDS files unchanged. Work in a fresh Blender scene; preserve geometry, UVs, attachment names, hierarchy and authored hidden state.
- Use the existing Blender MCP connection and official Unity CLI/`unity mcp` bridge. Inspect live versions and command schemas; these are pinned working versions, not a claim about the latest releases.
- Separate source facts from project balance choices. Use [[GameDesign/Republic at War - Republic Unit Stats Reference]] for Republic unit data, retaining its provenance/version caveats.
- Preserve existing `.meta` GUIDs, Addressables group structure and unrelated user changes. Do not run automated tests without an explicit request or discard/save unrelated dirty scenes.
- Reuse existing MVP responsibilities: models own rules/state; views render and hold explicit references; existing entities/presenters coordinate components. A new ship normally needs assets and registrations, not new combat logic.

## Implementation

### 1. Inspect Blender and verify MCP

1. Check installed Blender versions, running processes and the listener at `127.0.0.1:9876`. An occupied port does not prove the correct Blender version is connected.
2. Reuse portable Blender `3.6.23` at `%LOCALAPPDATA%/AI-Tools/Blender/blender-3.6.23-windows-x64/blender.exe`. Existing Blender `4.2.16 LTS` stays installed; this ALAMO importer depends on pre-4.0 material APIs.
3. Verify enabled add-ons: `mcp-for-blender==2.1.3` / protocol `13`, and ALAMO importer commit `2b0fb0e34e4f451d2e380b042d88ad1a4c8a09b5`. Rothana used unmodified upstream add-on code.
4. On a new machine, install those compatible versions, verify the Blender download against its published checksum, install/enable the MCP add-on, then install/enable ALAMO through Blender MCP and save preferences.
5. Start with `powershell -File Tools/Blender/Start-Blender.ps1`. Verify MCP initialization, tool listing, Python execution, add-on availability and a viewport screenshot before importing.
6. Read `bpy.app.version_string` through MCP. Confirm `bpy.ops.import_mesh.alo` is registered; stop and fix the connection/add-on if it is absent.

- Canonical connection: `.codex/config.toml`; agent JSON files are mirrors. Keep localhost binding, safe mode enabled and telemetry disabled.
- Native tools may require a Codex restart after configuration changes. The checked-in SDK client can use the existing configuration immediately:

```powershell
uvx --from mcp-for-blender==2.1.3 python Tools/Blender/mcp_call.py request.json
```

```json
{"tool":"execute_blender_code","arguments":{"code":"import bpy\nprint(bpy.app.version_string)"}}
```

### 2. Audit the source and import ALO

1. Inventory ALO variants, separate turrets, DDS textures and any ALA animations. Choose the complete variant intentionally; do not combine a Full Armed model with duplicate separate turrets.
2. Import with `bpy.ops.import_mesh.alo(filepath=SOURCE_PATH, importAnimations=False)` for a static ship.
3. Record mesh/triangle counts, bone names and parents, attachment positions, UVs, materials, loaded image sizes, hidden flags and visible bounds.
4. Inspect the model and source material assignments. Resolve every referenced texture; identify albedo, normal, emissive and effect textures explicitly.
5. Inspect attachment and helper geometry. Names such as `LOD`, `Shield` or `Collision` are evidence to investigate, not sufficient grounds to remove a mesh.

- Rothana source: `C:/Users/golin/Downloads/Rothana.2/Models/Rothana_Stardestroyer_Full_Armed.ALO`.
- Its importer produced 99 bones from a verified 100-bone source because it removed identity `Root`. Restore a missing root only after checking the actual source; do not add one to every model.
- `Hull_LOD_1` contains engine geometry and must remain visible. `Shield`, `Col` and `Shadow` remain in the asset with renderers disabled.
- No ALA files were supplied for Rothana. Animation conversion was not implemented or validated.

### 3. Prepare export and validate the FBX round trip

1. Convert importer `CHILD_OF` constraints into exportable bone parenting while preserving each mesh's world transform. Compare bounds and attachment positions before/after.
2. Store source visibility and shader intent as custom properties; Rothana uses `EaWHidden` and `EaWShader`.
3. Convert referenced DDS images to PNG. Keep normal maps as data and preserve transparency; create a simple Principled material representation for FBX transport.
4. Save the editable `.blend`, export FBX, then reimport it into a separate validation scene. FBX does not reproduce EaW shader behavior automatically.
5. Compare mesh/triangle counts, UVs, bone names/parents, attachment positions and visible bounds. Check rendered appearance as well as numbers.

```python
bpy.ops.export_scene.fbx(
    filepath=fbx_path, use_selection=False,
    object_types={'MESH', 'ARMATURE', 'EMPTY'},
    add_leaf_bones=False, use_armature_deform_only=False,
    bake_anim=False, use_custom_props=True,
    axis_forward='-Z', axis_up='Y',
    apply_unit_scale=True, path_mode='RELATIVE')
bpy.ops.import_scene.fbx(
    filepath=fbx_path, use_anim=False, use_custom_props=True,
    automatic_bone_orientation=False)
```

- Export from a clean source scene: `use_selection=False` includes all eligible scene objects. Keep validation copies out of subsequent exports.
- Restore authored hidden flags after the round trip. Blender may append `.001` to validation object names because scenes share a global object namespace.
- Compare imported ALO geometry against FBX geometry. The upstream importer and Unity may weld vertices; raw vertex-count equality is not the sole integrity check.
- `Tools/Blender/export_rothana.py` is **Rothana-specific**: fixed rig name, 99-bone precondition, root repair, 16-texture map and output path. Run once on a fresh matching import; adapt and review it for each different model.
- Create `Temp/BlenderConversion/Output/Textures` before that script runs. Its MCP request can use `{"tool":"execute_blender_code","code_file":"F:/Private/empire-at-war/Tools/Blender/export_rothana.py"}`.

### 4. Import Unity art and establish gameplay size

1. Confirm the correct open project with `unity status --json`; inspect available schemas before unfamiliar `unity command` operations.
2. Import FBX, textures and external materials into the type-first locations below. Explicitly remap FBX material slots to project materials.
3. Use `EmpireAtWar/Ship Lit` for opaque hull surfaces. Configure additive engine/window effects separately; retain disabled helpers.
4. Set albedo/data texture import types correctly. Rothana normals use linear normal-map import with green-channel flip to match ALAMO; verify the convention for each new model.
5. Create a geometry/attachment-only visual prefab. Center its visible bounds, verify bow `+Z` and up `+Y`, and choose gameplay length relative to existing ships.
6. Keep the gameplay root at unit scale. Fit collision, shields, ion-effect bounds, selection marker, navigation radius and hangar exit to the final visible hull; recompute vertical range with banking.

| Asset | Pattern |
| --- | --- |
| FBX | `Assets/Art/Models/<Faction>Ships/<Ship>/<Ship>.fbx` |
| Materials / textures | `Assets/Art/Materials/Models/<Faction>Ships/<Ship>/`, `Assets/Art/Textures/Models/<Faction>Ships/<Ship>/` |
| Visual / gameplay prefabs | `Assets/Prefabs/Models/Ships/<Ship>.prefab`, `<Ship>ShipView.prefab` |
| Ship / wreck data | `Assets/Settings/Data/Ship/<Ship>ShipData.asset`, `Ship/Wreck/<Ship>WreckData.asset` |
| Wreck prefab / materials | `Assets/Prefabs/Models/Wrecks/<Ship>WreckView.prefab`, `Assets/Art/Materials/Wrecks/<Ship>/` |
| Placement preview | `Assets/Prefabs/Ui/Reinforcement/<Ship>ReinforcementView.prefab` |
| Ship icon / matchups | `Assets/Art/Textures/Ui/Icons/ShipIcon/<Ship>Icon.png`, `Assets/Settings/Data/Tooltip/Matchups/<Ship>Matchups.asset` |

- Rothana's FBX import scale `0.02` produced only `9.680` units of visible length. Its visual prefab needed approximately `16.53×` scale to reach `160` units.
- Final visible size: `81.289 × 29.477 × 160.000`; banked hull range at ±15°: `-23.337 .. 23.858`; navigation radius: `95`. These are project units/choices, not canonical meters.

### 5. Build the gameplay prefab, data and hardpoints

1. Start from a compatible existing ship configuration; Rothana used Venator. Replace old geometry, hardpoints and effects, then rebuild all model-dependent references.
2. Create ship data with explicit source-backed health, shields, weapon composition and abilities. Label cost, speed, size, population, regeneration and other unsupported values as provisional.
3. Map each weapon/system to actual attachment positions. Preserve reference weapon counts even when ALO bone names differ; document the mapping.
4. Place hardpoints under the banking body pivot. Calculate muzzle/world positions correctly; imported bone rotation is not automatically Unity yaw.
5. Assign unique hardpoint IDs, health ordering, weapon profiles/arcs, fog references and hangar dependencies. A twin barrel pair need not represent two separate gameplay weapons.
6. **Hardpoints must be visible/targetable.** Add every weapon hardpoint to `HealthComponent.ShipUnits` and give each used `HardPointType` a `ShipData.hardPointHealth` entry — even when the source XML says `Is_Targetable=No` / `Is_Destroyable=No` (common in AOTR). Hull-only targeting needs explicit user approval.
7. Use available fighter types only with an approved substitution. Fit the launch point outside the hull and record reserves, simultaneous capacity and launch delays.

| Component | Explicit bindings to rebuild |
| --- | --- |
| Ship / movement | Opaque explosion hull renderers; body pivot; movement line renderer |
| Health / shield | ShipUnits hardpoint list; shield view/renderer; ion prefab and bounds |
| Weapons / fog | New hardpoint collections; all intended visible/effect renderers |
| Selection / team color | Selection canvas/image; intended team-colored renderer list |
| Hangar / audio | Launch point and hangar hardpoint; correct faction audio configuration |

- Rothana: `18` weapons + shield generator + engines + hangar = `21` destroyable hardpoints.
- Weapon composition: `8` DBY-827, `2` turbolasers, `4` lasers, `2` point-defense and `2` ion cannons. The exact bone mapping is in `Tools/Blender/README.md`.
- User-approved hangar: Delta-7 + A-Wing instead of the reference V-Wing/Y-Wing; each has `3` reserves and `1` active slot. Delays `4 s` initially / `8 s` between launches are project choices.
- Reusing a prefab does not prove its references fit the replacement ship. Inspect saved references for old ship GUIDs/transforms and missing scripts.

### 6. Complete every registration

| Registration | Required result |
| --- | --- |
| `ShipType` | Unique explicit enum value; never renumber existing entries. Rothana = `7`. |
| `ShipsData` | Ship type → correct ship-data asset reference |
| `AssetMappingData` | `<Ship>ShipView` → correct gameplay prefab |
| Existing Addressables groups | View/data entries and expected addresses; retain group structure |
| Republic `FactionsData` | Roster entry, role/description, cost, level, limits, population, icon and matchups |
| `ShipUiData.shipIconWrapper` | Ship type → correct ship sprite |
| `TooltipIconData.icons` | Correct sprite identifier/reference, including local file ID |
| `ReinforcementData.spawnShipWrapper` | Ship type → its own placement prefab's `UnitSpawnView` component |
| Ship data dependencies | Wreck data, height tier, supported weapon profiles and abilities |
| `ShipIconGenerator.MAPPINGS` | New ship included for future icon regeneration |

- Inspect actual loaded references after saving. A matching name is insufficient when a serialized GUID or component reference still points to the donor ship.
- Check faction UI, tooltip and in-battle ship icon consumers independently. Rothana's requested faction/roster icon is a ship sprite rendered from its actual model.

### 7. Build the separate preview, wreck and icon

- **Placement preview:** create `<Ship>ReinforcementView.prefab` with the same visible geometry, size and orientation; assign the shared `Assets/Art/Materials/Vfx/Hologram.mat` to visible slots.
- Assign its `UnitSpawnView` explicitly in `ReinforcementData`; fit trigger bounds and use the existing kinematic/no-gravity setup. Keep helpers disabled and prefab root at identity; spawn height comes from ship data.
- **Wreck:** create dedicated wreck materials and data, using only intended visible opaque hull meshes. Preserve team-livery settings where the wreck shader uses them.
- **Icon:** render the actual model with transparent background at `512 × 512`, matching existing framing. Inspect alpha, silhouette and crop; assign the imported sprite to all three icon consumers.
- Icon and wreck generators must respect disabled helper renderers. Do not enable every renderer or regenerate unrelated ships as part of one import.
- Use a geometry-only clone for isolated preview rendering. Instantiating an uninjected gameplay prefab can invoke component cleanup/lifecycle code and create unrelated errors.

### 8. Configure and visually verify team colors

- Team recoloring needs **both** renderer ownership and material opt-in. `TeamColorView` supplies `SetShaderUserValue(ownerPaletteIndex + 1)`; `TeamColorService` supplies the `_TeamColors` palette.
- Shader user value `0` means unowned; retaining original red in an ordinary prefab preview is expected. Verify with explicit owned blue/green renders.
- Saturated red markings have hue near `0`. Ship Lit selects paint by hue range and minimum saturation, preserving neutral hull panels and source brightness.
- Rothana enables livery on `Rothana_hull_1_Base`, `Rothana_hull_3_base`, `Rothana_engines` and the three corresponding wreck materials:

| Property | Rothana value |
| --- | ---: |
| `_TeamLiveryHue` | `0` |
| `_TeamLiveryHueRange` | `0.07` |
| `_TeamLiveryMinSaturation` | `0.4` |
| `_TeamLiveryStrength` | `1` |

- Leave neutral materials and yellow hangar safety markings at strength `0`. Derive paint hue/material selection separately for each new ship.
- `TeamLiveryAnalyzer` uses a `0.5%` texture-coverage cutoff; Rothana hull-1 red coverage is approximately `0.3%`, engines `0.4%`. Automatic detection misses these valid thin markings.
- Preserve manual settings after reimport; do not blindly rerun detection across all materials. An empty/black `_TeamMaskMap` does not prevent the existing HSV livery path.
- Verify all eight palette entries, inspect at least contrasting colors, and restore any temporary global palette state after rendering.

### 9. Persist assets and record verification

1. For Unity API changes, mark modified assets dirty and call `AssetDatabase.SaveAssets()`. Structural prefab edits use `LoadPrefabContents → SaveAsPrefabAsset → UnloadPrefabContents`, then save assets.
2. After direct serialized-file edits, run `AssetDatabase.Refresh()`, `ForceReserializeAssets` with only the changed asset paths, and `SaveAssets()`.
3. Wait for imports/compilation; inspect the Console for import, serialization and compile errors. Reload saved assets and verify references, material values and prefab dimensions.
4. Record conversion counts/tolerances, screenshots, registration checks and any unresolved limitations. Never require a manual focus/save action from the user to persist the assets.
5. Run automated tests only when explicitly requested. Before any authorized scene-changing verification, inspect open scenes and preserve dirty/untitled user work.
6. Review the Git diff, include the new assets and their `.meta` files, exclude temporary conversion files and unrelated edits; commit when requested.

## Edge Cases

### What I missed on Rothana and how to prevent it

| Miss or problem | Observed result | Required prevention |
| --- | --- | --- |
| Separate reinforcement mapping omitted | Placement showed Venator although the gameplay prefab was Rothana | Create/register the dedicated preview; verify the resolved `UnitSpawnView`. Ship lookup now fails for a missing key instead of silently choosing the first preview. |
| Material livery strength left at `0` | Red paint ignored team ownership despite correct renderer bindings | Inspect material livery properties and owned-color renders, including wreck materials. |
| Import scale treated as final size | Model was far too small | Measure visible gameplay bounds and fit every dependent volume after scaling. |
| Reliance on automatic paint detection | Sparse red trim was missed; yellow hangar markings could be selected | Inspect source texture regions and explicitly choose livery materials/hue. |
| Helper/LOD names taken at face value | Hidden helpers could enter icons/wrecks; real engine geometry could be removed | Inspect geometry and honor saved renderer state. |
| Full gameplay prefab used for an isolated render | Uninjected component cleanup produced a `WeaponComponent.Release` error | Render a geometry-only clone with appropriate layers/camera/lights; do not add production null guards to accommodate a preview. |
| Source `Is_Targetable=No` copied as hull-only targeting (Imperial Arquitens, 2026-10-06) | Empty `ShipUnits` + empty `hardPointHealth` → no hardpoints shown or targetable in battle | Always bind weapon hardpoints to `ShipUnits` with matching `hardPointHealth`; the build/verify scripts must assert `ShipUnits` count > 0. Ignore source targetability flags. |
| A working conversion script mistaken for a generic importer | Hardcoded Rothana names/counts/root repair would fail or corrupt another model | Audit each source and adapt the script; keep model-specific repairs explicit. |

### Verification limits and work still requiring validation

- Verified for Rothana: MCP operations; Blender/Unity geometry and hierarchy; saved assets/references; compile/import checks; icon/preview renders; placement mappings; team-color renders.
- No Play Mode combat verification or automated Unity test suite was run. Fire arcs, hangar launches, abilities, shields, fog/selection, reinforcement placement and death/wreck behavior still need an authorized runtime check.
- Gameplay cost/speed/scale, regeneration and hangar timing remain provisional; working references and renders do not establish balance.
- EaW animated shader effects, refraction/proxy particles and animation retargeting were not recreated. Source weapon geometry is static.
- There is no checked-in general one-command Unity ship integration builder. Temporary integration scripts are not a reusable pipeline; this guide and the saved Rothana assets are the repeatable reference.

## Important Values

| Rothana evidence | Recorded value |
| --- | --- |
| Source → FBX | `13` meshes, `42,813` triangles, `100` bones |
| Blender round trip | Maximum bone displacement `0.0000511` source units; bounds delta below `0.00003` |
| Unity import | Same mesh/triangle/bone totals; UVs on every mesh; maximum bone displacement `0.00000121` Unity units before gameplay scaling |
| Gameplay geometry | `10` visible meshes; `7` opaque wreck meshes |
| Source-backed ship values | Hull `10,300`; shields `3,000`; Power to Shields + Power to Weapons |
| Project balance example | Level `5`; cost `7,500`; build `35 s`; population `8`; max count `3`; speed `6.5 units/s` |
| Output package | `C:/Users/golin/Downloads/Rothana.2-Converted/`: FBX, blend, textures, reports and preview |

## Files

- `Tools/Blender/README.md` — installed versions, exact Rothana hardpoint mapping, conversion decisions and verification evidence.
- `Tools/Blender/Start-Blender.ps1`, `mcp_call.py`, `export_rothana.py` — launcher, MCP SDK fallback and model-specific exporter.
- `Assets/Prefabs/Models/Ships/RothanaShipView.prefab` and `Assets/Prefabs/Ui/Reinforcement/RothanaReinforcementView.prefab` — gameplay and placement examples.
- `Assets/Settings/Data/Ship/RothanaShipData.asset` — ship values and dependencies; `Assets/Settings/Data/Reinforcement/ReinforcementData.asset` — preview registry.
- `Assets/Art/Shaders/Units/ShipLitInput.hlsl`, `Assets/Scripts/Components/TeamColor/TeamColorView.cs`, `Assets/Scripts/Editor/Rendering/TeamLiveryAnalyzer.cs` — livery contract and detection limits.
- `Assets/Scripts/Editor/ShipIconGenerator.cs`, `Assets/Scripts/Editor/Rendering/ShipWreckBuilder.cs` — generators must preserve hidden-renderer state.
