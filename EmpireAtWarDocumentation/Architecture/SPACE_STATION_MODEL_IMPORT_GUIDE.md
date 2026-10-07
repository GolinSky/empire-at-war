---
type: reference
updated: 2026-10-07
tags:
  - unity
  - blender
  - alo
  - space-stations
  - rebellion
  - empire
---
# Space Station Model Import

## Goal

- Import each station level from its own ALO; preserve source geometry, UVs, hierarchy and attachment names.
- Bind levels 1–5 to the existing faction-level system. Rebel models belong to **Rebellion only**.
- Republic at War CIS models: [[Architecture/CIS_SPACE_STATION_MODEL_IMPORT_GUIDE]] — five originals, Separatist-only mapping, embedded hardpoints, source normals and exact rebuild commands.

## Rules

- Read [[Architecture/ALO_MODEL_IMPORT_GUIDE]], [[Architecture/PROJECT_ORGANIZATION]] and repository `AGENTS.md` before work.
- Use the official `unity` CLI and the existing Blender MCP pipeline. Preserve source ALO/DDS files, asset GUIDs and Addressables group structure.
- Audit binary material assignments and bone indices before export. A successful Blender → FBX round trip alone cannot detect importer mistakes.
- Reuse gameplay components and existing level/economy data. Asset import does not authorize combat balance changes.
- Keep one common scale across levels; never substitute scaled copies of a single model.

## Implementation

### 1. Source selection and textures

- Source folder: `output/aotr-space-stations/preview-selection/Vanilla/Data/ART/MODELS/`.
- Texture folder: sibling `TEXTURES/`; all four required textures were already present.
- Missing texture fallback: `D:/SteamLibrary/steamapps/common/Star Wars Empire at War/GameData/Data/textures.meg`.
- Match referenced names case-insensitively by stem; ALO `.tga` references may resolve to archived `.dds` files.
- Extract only missing referenced members. Existing archive-reader example: `output/aotr-space-stations/preview-selection/PrepareStations.py`, `MegIndex` / `MegRead`; its top-level script also builds unrelated previews, so do not rerun it blindly.
- `Prepare.py` audits each original binary and losslessly decodes `RB_Station`, `RB_Station_Bump`, `RB_Stationlights`, `W_blast00`.
- Base ALOs omit separate hardpoint artwork. `PrepareAttachments.py` reads `STARBASES.XML` → `HARDPOINTS.XML` from `config.meg` and extracts each `Model_To_Attach` from sibling `models.meg`.
- `Attachments.json` records the original `Attachment_Bone` mapping: **20 unique models**, **5 / 8 / 11 / 14 / 18** pieces at levels 1–5. All use the existing station textures. Missing references fail immediately.

| Level | Original ALO | Visual prefab | Configuration element |
| --- | --- | --- | --- |
| 1 | `RB_STATION_01.ALO` | `RebelSpaceStationLevel1.prefab` | `levelModels[0]` |
| 2 | `RB_STATION_02.ALO` | `RebelSpaceStationLevel2.prefab` | `levelModels[1]` |
| 3 | `RB_STATION_03.ALO` | `RebelSpaceStationLevel3.prefab` | `levelModels[2]` |
| 4 | `RB_STATION_04.ALO` | `RebelSpaceStationLevel4.prefab` | `levelModels[3]` |
| 5 | `RB_STATION_05.ALO` | `RebelSpaceStationLevel5.prefab` | `levelModels[4]` |

- All visual prefabs: `Assets/Prefabs/Models/Stations/`.
- All configuration elements: `StationLevelView.levelModels` on `Assets/Prefabs/Models/Stations/RebellionSpaceStationView.prefab`.

### 2. Blender setup and conversion

1. Use portable Blender `3.6.23`, ALAMO commit `2b0fb0e34e4f451d2e380b042d88ad1a4c8a09b5`, and `mcp-for-blender==2.1.3` / protocol `13`; see the ALO guide for installation.
2. Inspect running Blender sessions first. This import uses dedicated localhost port `9885`; the user's shared `9876` session is preserved.
3. Start the dedicated session only if absent. `Tools/Blender/RebelSpaceStation/Start.py` runs in the separate Blender process; `Call.py` reads project MCP configuration and overrides only its child process port.
4. Confirm Blender version, ALAMO operator availability, safe mode and the dedicated scene through MCP. Conversion clears that scene.
5. Run `Prepare.py`, then `ConvertRequest.py` and `Call.py` separately for each level. One level per request avoids MCP's AST node limit.
6. Save editable `.blend` and FBX, reimport FBX into a validation scene, compare geometry/UVs/bones, and restore source visibility.
7. Run `Stage.py` only after all five reports pass and identify the expected level/model.
8. Run `PrepareAttachments.py`, convert its 20 individual requests with the same isolated MCP client, then `StageAttachments.py`; exact commands are in the tooling README.

- **Root repair:** every supplied file has a verified identity `Root`; ALAMO removes it, so restore it.
- **Bone repair:** vanilla placeholders repeat names. ALAMO resolves parents by name; rebuild from original parent indices and matrices.
- Blender enumerates bones in hierarchy order, not file order. Never zip its bone list with the source table.
- FBX transports unique bone names; the Unity visual prefabs restore original repeated names while retaining parent relationships.
- Replace importer `CHILD_OF` constraints with exportable bone parenting, preserving source world transforms.
- Rebuild materials from each original shader + parameter set; preserve source hidden flags as metadata.

### 3. Unity import and materials

1. Verify `F:/Private/empire-at-war` is the connected project and Unity is in Edit Mode.
2. Run **`BuildArt.cs` → `BuildAttachments.cs` → `RepairDomeUv.cs` → `BuildView.cs`** through `unity command run_script --file ... --json`. Inspect nested `result.success` after each.
3. Base art rebuilding resets generated visual metadata. Attach source artwork and repair display UVs before fitting shields and assigning gameplay references.
4. All four builders save prefab contents and dirty assets. Reopen the saved assets with `Verify.cs`; no manual focus/save step is required.

| Asset | Location |
| --- | --- |
| FBX, level N | `Assets/Art/Models/SpaceStations/RebelSpaceStation/LevelN/RebelSpaceStationLevelN.fbx` |
| Attachment FBXs | `Assets/Art/Models/SpaceStations/RebelSpaceStation/Attachments/` |
| Corrected display hull, level N | `Assets/Art/Models/SpaceStations/RebelSpaceStation/LevelN/RebelSpaceStationLevelNHull.asset` |
| Materials | `Assets/Art/Materials/Models/SpaceStations/RebelSpaceStation/` |
| Textures | `Assets/Art/Textures/Models/SpaceStations/RebelSpaceStation/` |
| Shield, level N | `Assets/Art/Models/Shields/RebelSpaceStationLevelNShield.asset` |
| Gameplay prefab | `Assets/Prefabs/Models/Stations/RebellionSpaceStationView.prefab` |

- Slot `00`: damage overlay / `W_blast00`, disabled in gameplay.
- Slot `01`: opaque `EmpireAtWar/Ship Lit` hull, `RB_Station` albedo and `RB_Station_Bump` normal.
- Slot `02`: additive lights / `RB_Stationlights`, using the project's existing light-material convention.
- Slot `03`: source shadow-volume material, retained in FBX; disabled shadow renderer leaf removed from gameplay prefabs.
- Normal import: linear Normal Map, green-channel flip; all staged textures uncompressed.
- Team mask: **direct hull alpha**, not inverted; nonzero coverage `3.7109375%`. `_TeamMaskStrength=1`, `_TeamLiveryStrength=0`.
- Bind all nested renderers, including inactive levels and disabled damage overlays, to the gameplay `TeamColorView` and fog renderer lists.
- Render all eight team palettes with explicit owned shader user values; ordinary prefab previews can retain original colors.

### Dome surface repair attempt — 2026-10-07

- **Unresolved:** user retested and reported the same visual issue after this attempt. Keep the current assets as requested; do not treat passing structural tests or isolated renders as proof that the reported appearance is fixed.

- The reported dark radial strip also exists in the original ALO UVs; textures and imported triangle corners match the source. Missing attachment artwork was a separate omission.
- `RepairDomeUv.cs` creates a derived hull mesh per level: **15 UV vertices / 16 triangles** map to the matching curved plating at +90° around the source dome.
- Positions, triangle indices, normals, textures and original FBX UVs remain unchanged. Recalculate tangents for the corrected UVs.
- This explicit visual repair intentionally differs from source UVs. Do not modify the atlas globally; other station surfaces and hardpoint meshes share it.

### 4. Scale, collision and attachments

- Gameplay and visual prefab roots use unit scale. **All levels share level 1's source pivot** (see Level progression); only level 1's hull is centered at the root.
- FBX scale `0.02`; shared nested scale approximately `12.3978834`; source effective scale approximately `0.247957668`.
- Chosen size: level 5's maximum XZ diameter `299.302856` units matches the existing station envelope. Why: retain project scale while preserving growth between distinct models.
- Artwork placement uses the XML bone's original matrix conjugated by the source-to-Unity basis; FBX bone-axis rotations are not attachment rotations. Retain source rigs/helpers in FBXs and copy only visible meshes under existing named anchors in prefabs.
- Keep Unity up `+Y`; standard FBX axes are forward `-Z`, up `Y`. Verified raw source → Unity position mapping: `(-x, z, -y) × 0.02`.
- Bake a separate shield shell for every level with existing `ShieldHullBaker`; each stores mesh plus `1024` clipping planes.
- Bind 17 existing gameplay hardpoints by ID to explicit source anchors in `Tools/Blender/SpaceStations/<Faction>.json`; weapon types, arcs and unlock levels remain unchanged.
- Shield anchor: `HP01_SHG_Bone` at levels 1–3 → `HP04_SHG_Bone` at levels 4–5. Level 5 replaces `FP02_TBL_00` with `FP05_TBL2_00`.
- Locked hardpoints have no mount until their unlock level; they stay inactive.
- Preserve original `Spawn_00`. Generated `GameplayLaunchExit` keeps its X/Z and sits `8` units below the level's collider minimum Y.
- Existing Rebellion map-generation radius `396` already covers the largest hull. Shared station spawn-block settings remain unchanged.

### 5. Faction and upgrade wiring

- `Assets/Settings/AssetMappingData.asset`: key `RebellionSpaceStationView` → the new gameplay prefab; other faction station keys retain their existing references.
- Existing Addressables `View` group: address `RebellionSpaceStationView`; no group/schema reorganization.
- `Assets/Settings/Data/Factions/Shared/StationLevelData.asset`: existing maximum level `5`, costs and upgrade timing.
- `Assets/Settings/Data/Models/SpaceStation/SpaceStationData.asset`: existing per-level health/shield multipliers, weapon health and hangar configuration.
- `SpaceStation` applies current faction level during initialization and listens to the existing `OnLevelUpgraded` event.
- `StationLevelView.ApplyLevel` activates exactly one mapped model and updates collider, selection marker, hardpoints (by id from `StationLevelModel.Mounts`), hangar launch point, and shield center/surface (`Shield.SetHull`).
- The entity updates explosion hull renderers and the ion field (`IIonFieldShape.SetIonFieldBounds`) before the existing health upgrade.
- One gameplay entity persists across upgrades; no replacement of health, weapons, hangar, ownership or subscriptions.
- Each faction routes to its own gameplay prefab; CIS now has its own five-level `StationLevelView`. See [[Architecture/CIS_SPACE_STATION_MODEL_IMPORT_GUIDE]] for CIS-specific source and setup.

### 6. Verification and rebuild commands

- Exact PowerShell rebuild procedure: `Tools/Blender/RebelSpaceStation/README.md`.
- `Verify.cs`: reload all six prefabs, reject missing scripts/broken references, verify imported bone counts/parents and export raw Unity vertices/UVs.
- `VerifyGeometry.py`: compare original binary bone transforms and every non-shadow triangle corner with Unity; verify source hashes and exact decoded texture pixels.
- `Render.cs`: generate `Temp/RebelStationImport/Previews/LevelNTeam0..7.png` and `LevelNCloseup.png` from geometry-only preview scenes.
- Before tests: inspect all open scenes; save named dirty scenes and inspect again. Untitled dirty scene → `BLOCKED_DIRTY_UNTITLED_SCENE`.
- Run `unity command run_tests --mode editor --filter Station --async_tests true --json`; poll `test_status` to completion. Capture results immediately when sharing an Editor.
- Inspect compilation and Console import/serialization errors after asset persistence.

### Recorded verification — 2026-10-07

- Final station Edit Mode suite: **38/38 passed**, including **14/14 StationLevelViewTests**.
- Final prefab reload and original-binary geometry/UV/bone checks passed for raw FBXs; four source textures round-trip losslessly. Derived-mesh tests verify exactly 15 changed UVs and unchanged geometry/normals per level.
- Attached art counts, source bone parents, material references, fog/team bindings and shield swaps passed; results: `Temp/RebelStationImport/StationSurfaceTests.json`.
- Rendered all five levels in eight palettes; inspected every repaired dome close-up and distinct model silhouettes.
- Compilation passed. No station import/serialization errors were found after saving.
- Broader `TeamColorViewPrefabTests` encountered an unrelated existing `AcclamatorAssaultShipView.prefab` renderer-list mismatch (31 expected, 29 bound); that ship was not changed by this task.
- Tests invoke the station's real level handler, including initial levels 1/5, repeated upgrades, world-space attachments, faction mapping, health forwarding and shield impacts. No full battle playthrough was performed.

### Imperial Stations — 2026-10-07

- Empire only: `Assets/Settings/AssetMappingData.asset`, key `EmpireSpaceStationView` → `Assets/Prefabs/Models/Stations/EmpireSpaceStationView.prefab`; existing Addressables `View` group.
- Level mapping: `StationLevelView.levelModels` on `EmpireSpaceStationView.prefab`; elements `0..4` map to levels `1..5`.
- Distinct originals: `EB_STATION_01.ALO` → `EmpireSpaceStationLevel1.prefab`, through `EB_STATION_05.ALO` → `EmpireSpaceStationLevel5.prefab`. All prefabs: `Assets/Prefabs/Models/Stations/`.
- FBXs: `Assets/Art/Models/SpaceStations/EmpireSpaceStation/LevelN/EmpireSpaceStationLevelN.fbx`; separate artwork in `Attachments/`.
- Material/texture folders: `Assets/Art/{Materials,Textures}/Models/SpaceStations/EmpireSpaceStation/`. Actual references: `EB_Station`, `EB_Station_bump`, `EB_stationlights`, `W_blast00`; all four were present.
- XML attachment mapping: **18 unique ALOs**, **5 / 8 / 11 / 14 / 18** pieces at levels 1–5. Reconstruct placements from original bone matrices.
- Source bone counts: **63 / 87 / 110 / 134 / 162**. Base hull triangles: **2405 / 2901 / 3741 / 4679 / 8201**, plus lights, damage and attached artwork.
- Chosen scale: FBX `0.02`, common nested scale `9.116956`, unit gameplay/visual roots. Level 5 base diameter `299.3029` units; attachment-inclusive diameter `302.8595` units.
- Preserve source UVs; do not apply the Rebel dome repair. Hull: `EmpireAtWar/Ship Lit`, direct alpha team mask, green-flipped normal, existing additive light convention. Mask coverage **0.640869%**; `_TeamRimStrength=0` keeps gray panels unchanged and recolors authored stripes.
- Team-color correction: the default rim strength `0.6` caused the broad tint; GPU mask red mean matched source alpha correctly. Isolated mask/rim renders confirmed the cause. All **40 owned renders** change only **0.468–4.200%** of visible pixels versus unowned references (channel delta >4/255); retain at least **95.8%** unchanged. Existing-import fix: `Tools/Blender/EmpireSpaceStation/FixEmpireStationTeamColors.cs`; builder preserves the setting.
- Imperial gameplay anchors use `CM`; shield remains `HP01_SHG_Bone` at every level; level 5 retains `FP02_TBL_00`. Preserve the existing 17 hardpoint profiles/unlock levels. Launch exit: `8` units below each collider.
- All **23 FBXs** pass original-binary geometry/UV/bone comparisons. Maximum raw Unity vertex/bone displacement: **0.000006174 / 0.000005185 units**; UV delta **0.000034060**, below **0.018 pixels** at 512px.
- Four staged PNGs match decoded DDS pixels exactly; original ALO/DDS hashes unchanged. Six saved prefabs reload without missing scripts or broken references.
- Mapping checks exercise levels `1/5/2/3/4/5/1`: distinct sources, one active model, collider/attachment updates, shield data, fog/team renderer bindings and Empire-only routing.
- Shared combat/upgrade data and other faction prefab/routing references remain unchanged. Existing Empire wreck configuration remains unchanged.
- All five levels rendered in eight palettes; inspected each level and contrasting blue/green ownership. No new import/serialization errors after saving. Unity tests and a full battle were not run.
- Limitations: ALAMO simplifies shadow/collision helpers; retain them in source FBXs and exclude them from source-topology equality assertions. Static attachment artwork; no ALA, damaged/wreck models, animated destruction shaders/lights or proxy particles converted.
- Rebuild: `Tools/Blender/EmpireSpaceStation/README.md`; adapter reuses the established Rebel binary reader/converter/builders with Imperial-specific inputs. Isolated Blender MCP port `9886`; safe mode enabled.
- Reports, editable packed blends and previews: `Temp/EmpireStationImport/`; see `VerifiedSourceGeometry.json`, `VerifiedMapping.json` and `Previews/LevelNTeam0..7.png`.

### Republic Stations — 2026-10-07

- Republic only: existing `RepublicSpaceStationView.prefab` GUID, `AssetMappingData.asset` key `RepublicSpaceStationView`, and Addressables `View` entry.
- `ReB_Shipyard_Level_01–05.ALO` → `RepublicSpaceStationLevel1–5.prefab`; `StationLevelView.levelModels[0..4]` swaps the distinct original hulls.
- RaW XML names `Empire_Star_Base_1–5` represent Republic. Weapon art is embedded; `StationMount.Art=null` keeps hull geometry visible after hardpoint destruction.
- All five models retain 204 bones, source UVs, and exact authored normals. Normal restoration after Blender export avoids degenerate-face/custom-normal changes.
- Dedicated source, conversion, setup, scale, material, wreck, test, and limitation instructions: [[Architecture/REPUBLIC_SPACE_STATION_MODEL_IMPORT_GUIDE]].

### Level progression — 2026-10-07

- Final step for every leveled station: `unity command run_script --file Tools/Blender/SpaceStations/BuildStationLevels.cs --args '["<Faction>"]' --json`. Config: `Tools/Blender/SpaceStations/{Rebellion,Empire,Republic,Separatist}.json`; README beside it. CIS-specific conversion and mapping: [[Architecture/CIS_SPACE_STATION_MODEL_IMPORT_GUIDE]].
- **Shared pivot.** Per-level centering made stations jump on upgrade: Rebel ≤35 units, Empire ~100 units in X/Z. All levels now use level 1's source offset; builder fails if a shared anchor moves > `0.01` units.
- Hull centers after fix — Rebel L1–5: `(0,0,0)`, `(15.0,0,13.4)`, `(15.0,0,27.0)`, `(4.4,34.4,6.1)`, `(11.8,34.4,25.5)`. Empire: `(0,0,0)`, `(1.7,-4.5,105.0)`, `(79.0,-14.0,105.0)`, `(91.0,-15.6,105.0)`, `(92.2,-17.0,105.0)`.
- Farthest XZ hull corner from root: Empire `≈341`, Rebel `≈238`; both inside station radius `396`.
- **Typed mounts.** `StationLevelModel.Mounts` = `{HardPointId, Point, Art}`; replaces index-paired `AttachmentPoints`/`attachmentTargets`. Builder rejects missing ids, first anchor ≠ `unlockLevel`, or anchor type ≠ weapon type.
- **Per-level data.** `LaunchExit`, `ShieldCenter`, ion field bounds (`HealthComponent.SetIonFieldBounds`) and fog reveal radius (max over levels) follow the active model.
- `SpaceStation.levelView` remains an optional runtime binding. The Separatist gameplay prefab now assigns it and all five CIS models; the earlier lack of CIS level models is resolved.
- Verification: `--filter Station` **57/57 passed**; `Health` 17/17, `Shield` 10/10. Unrelated pre-existing failure: `ShipEngineHardpointTests.TwoEngines_*` NRE in `ShipMoveComponent.ApplyMoveCoefficient`.

## Important Values

| Level | Base source bones | Base hull triangles | Attached pieces | Complete visible bounds X × Y × Z, project units |
| --- | ---: | ---: | ---: | --- |
| 1 | 88 | 3656 | 5 | 168.302 × 379.463 × 163.543 |
| 2 | 103 | 4235 | 8 | 246.932 × 379.463 × 190.371 |
| 3 | 121 | 4641 | 11 | 246.932 × 379.463 × 217.549 |
| 4 | 141 | 6001 | 14 | 268.159 × 448.243 × 259.284 |
| 5 | 165 | 7407 | 18 | 299.303 × 448.243 × 298.060 |

- Original → raw Unity maximum vertex displacement: `0.00000384` units; bone displacement: `0.00001029` units.
- Maximum UV delta: `0.00001212`, below `0.007` pixels at source `512 × 512` resolution.
- All five original ALO hashes and four DDS hashes remained unchanged. Imported PNG pixels equal decoded DDS pixels exactly.

## Edge Cases

- ALAMO welds duplicate shadow-volume vertices and removes degenerate faces. Full FBX triangle counts therefore differ from binary totals; visible hull/light/damage triangle counts are preserved.
- No ALA animation, damaged Rebel model or Rebel wreck was supplied. Damage overlays remain disabled; EaW destruction shaders, animated lights and proxy particles are not recreated.
- Rebellion's previous Republic wreck fallback was removed from `SpaceStationData.wrecks`; existing explosion behavior remains.
- This mapping retains current project combat profiles; it does not recreate every vanilla XML weapon or balance value.
- A Blender material suffix such as `.001` is a transport name; the builder remaps it to the same stable four project materials.
- Five nested model instances keep fog/team references stable; only one level is active. All five geometries remain dependencies of the gameplay prefab.
- Rebuilding base art without the attachment, UV repair and view builders removes the surface fix and leaves attachment/shield metadata incomplete.
- Attached hardpoint art hides while its hardpoint is destroyed and returns when an upgrade restores it. EaW destruction animation is not recreated; existing project combat profiles remain unchanged.
- `HP01_CA` (both factions) is decorative: no gameplay hardpoint, never hidden.
- A full battle playthrough and source animation conversion are outside the recorded verification.

## Files

- `Tools/Blender/RebelSpaceStation/` — source audit, isolated MCP client, converter, Unity builders, verification scripts, preview renderer and commands.
- `Assets/Scripts/Components/ViewComponents/Station/StationLevelModel.cs` — saved per-model hull, attachment and shield references.
- `Assets/Scripts/Components/ViewComponents/Station/StationLevelView.cs` — explicit level mapping and visual updates.
- `Assets/Scripts/Entities/SpaceStation/SpaceStation.cs` — existing upgrade-event integration.
- `Assets/Scripts/Components/ViewComponents/Health/Shield.cs` — shield surface replacement.
- `Assets/Scripts/Tests/Editor/StationLevelViewTests.cs` — model identity, initial/upgraded level, faction isolation, attachment and shield tests.
- `Temp/RebelStationImport/` — editable blends, conversion/source audits, raw Unity geometry, measured verification, test results and previews.
