---
type: reference
updated: 2026-10-07
tags:
  - unity
  - blender
  - alo
  - space-stations
  - cis
  - separatist
---
# CIS Space Station Model Import

## Goal

- Import five distinct Republic at War station models; level 1–5 swaps keep one gameplay entity.
- Use these models only for `FactionType.Separatist` (CIS). Mod `Rebel_Star_Base_1–5` names do not mean Rebellion.

## Rules

- Read [[Architecture/ALO_MODEL_IMPORT_GUIDE]], [[Architecture/PROJECT_ORGANIZATION]] and repository `AGENTS.md`.
- Source Workshop ID: `1129810972`. Preserve installed ALO/DDS/XML files and existing Unity GUIDs.
- Reuse existing station upgrade/combat data, hardpoint IDs, unlock levels and Addressables structure.
- Use official `unity` CLI; builders must save all assets. Check nested `data.result.success` and reload saved prefabs.

## Decision

- Chosen: five original ALOs, five visual prefabs, one existing `SeparatistSpaceStationView.prefab`.
- Why: preserve source progression while reusing `SpaceStation.ApplyLevel` and `StationLevelView`.
- Avoid: scaled copies of one model, another faction's gameplay prefab, source XML combat rebalance.

## Implementation

### Source and level mapping

- Source root: `D:/SteamLibrary/steamapps/workshop/content/32470/1129810972/Data/`.
- ALOs: `Art/Models/`; textures: `Art/Textures/`.
- Definitions: `XML/StarBases_Cis.xml` → `XML/Hardpoints_CIS_Starbase.xml`.
- Visual prefab folder: `Assets/Prefabs/Models/Stations/`.

| Level | Original ALO | Visual prefab | Gameplay element |
| --- | --- | --- | --- |
| 1 | `SeB_Station_level_01.ALO` | `CisSpaceStationLevel1.prefab` | `levelModels[0]` |
| 2 | `SeB_Station_level_02.ALO` | `CisSpaceStationLevel2.prefab` | `levelModels[1]` |
| 3 | `SeB_Station_level_03.ALO` | `CisSpaceStationLevel3.prefab` | `levelModels[2]` |
| 4 | `SeB_Station_level_04.ALO` | `CisSpaceStationLevel4.prefab` | `levelModels[3]` |
| 5 | `SeB_Station_level_05.ALO` | `CisSpaceStationLevel5.prefab` | `levelModels[4]` |

### Setup and rebuild

1. Follow `Tools/Blender/README.md` for Blender **3.6.23**, ALAMO commit `2b0fb0e34e4f451d2e380b042d88ad1a4c8a09b5`, MCP **2.1.3** / protocol **13**.
2. Verify `unity status --json` targets `F:/Private/empire-at-war` in Edit Mode.
3. Run `Prepare.py`, then `CaptureCisStationBaseline.cs`. Preserve the first pre-import baseline; later capture means a rebuild baseline.
4. Inspect port **9887** and running Blender processes; start/reuse only the isolated CIS session. Keep safe mode enabled and telemetry disabled.
5. Upload each level's normal chunks in **numeric** order; run its conversion request. All writes go to `Temp/CisStationImport/`.
6. Run `Stage.py` → generated `BuildArt.cs` → generated `BuildView.cs` → shared `BuildStationLevels.cs` with `["Separatist"]`.
7. Rebuild the fixed CIS wreck with `ShipWreckBuilder.Build("Assets/Prefabs/Models/Stations/SeparatistSpaceStationView.prefab")`; the shared builder explicitly selects level 5.
8. Run `Verify.cs` → `VerifyGeometry.py` → `VerifyNormals.py` → `VerifyCisStationMapping.cs` → `Render.cs`; inspect every level and contrasting team colors.
9. Inspect all open scenes before tests. Save named dirty scenes and inspect again; dirty untitled scene → `BLOCKED_DIRTY_UNTITLED_SCENE`.
10. Start station Edit Mode tests asynchronously; poll until complete. Avoid Editor automation while long synchronous tests occupy its main thread.

- Exact PowerShell commands: `Tools/Blender/CisSpaceStation/README.md`.
- `Prepare.py` generates CIS adapters from the established Rebel audit/converter/builders; no new conversion backend.
- Safe mode requests stay below **200,000 bytes**; normal payload chunks use **120,000 characters**.

### Fidelity and materials

- Restore verified identity `Root`, original parent indices and bone matrices; ALAMO removes Root and resolves duplicate names incorrectly.
- FBX bone names are unique transport identifiers; visual prefabs restore original repeated bone names.
- ALAMO skips authored vertex normals; read the original binary normal field and assign Blender custom normals before export.
- Canonical material names derive from complete shader/parameter signatures; slot order differs between source levels.
- Five lossless PNGs: `seb_shipyards`, `seb_shipyards_b`, `seb_girder`, `W_blast00`, `W_buildinglights_3`.
- Hull: `EmpireAtWar/Ship Lit`, direct source alpha team mask (**13.565%** coverage), `_TeamRimStrength=0`.
- Normal map: linear, green flipped. Girders: separate texture, alpha clip **1**, cutoff **0.5**, `_ALPHATEST_ON`.
- Source shadow/collision helpers remain in FBX; disabled helper renderer leaves are removed from gameplay prefabs.

### Level and faction wiring

- Gameplay root: identity rotation/unit scale; remove old root mesh components while preserving gameplay components and child placement.
- FBX scale: **0.02**; common nested scale: **9.379914**. Checked-in target diameter: **380.5378 project units**.
- Align `HP01_SHG` across levels to correct small source-origin differences. Keep imported mesh/UV/normal data unchanged.
- `StationLevelView.levelModels` on `SeparatistSpaceStationView.prefab` owns the five nested instances; exactly one is active.
- Upgrade → collision, marker, hardpoint positions, launch exit, shield center/surface, explosion hull and ion bounds follow the active level.
- Launch exit: source `Spawn_00` X/Z; **8 units** below each hull. Each shield stores **1024 planes**.
- `Separatist.json` retains **17** gameplay hardpoint IDs/profiles; unlocked mount counts **4/7/10/13/17**.
- `AssetMappingData.asset` key/address `SeparatistSpaceStationView` retains its original prefab GUID. Other faction routing contains no CIS models.
- Shared `StationLevelData.asset` and `SpaceStationData.asset` remain unchanged: max level **5**, costs, timing, health/shield, hangar and wreck tuning.

## Important Values

| Level | Source bones | Base hull triangles | Hull size X × Y × Z, project units |
| --- | ---: | ---: | --- |
| 1 | 98 | 3442 | 230.529 × 213.535 × 93.819 |
| 2 | 181 | 6364 | 380.538 × 213.535 × 195.344 |
| 3 | 183 | 7180 | 380.538 × 222.541 × 240.654 |
| 4 | 183 | 9603 | 380.538 × 222.541 × 346.525 |
| 5 | 183 | 12869 | 380.538 × 222.541 × 346.525 |

- Original → raw Unity maximum vertex/bone displacement: **0.00001089 / 0.00001173 units**; UV delta: **0.000000211**.
- Nondegenerate normal vector maximum error: **0.000671**, below verifier tolerance **0.001**.
- Source ALO/DDS/XML hashes unchanged; all five PNGs equal decoded DDS pixels exactly.
- Seven transitions `1/5/2/3/4/5/1` pass mapping checks; largest hull remains inside existing station clearance **396**.
- **45 previews**: five levels × eight team palettes plus unowned. Inspect all silhouettes; levels 4/5 share an envelope but contain distinct geometry.

### Recorded verification — 2026-10-07

- Final `--filter Station` Edit Mode suite: **96/96 passed**; dedicated CIS tests **12/12**, all four factions' level-view tests **60/60**.
- Compilation clean; no new Unity import/serialization errors after saving and final verification.
- Tests cover the real initial/upgrade handler, one active model, hardpoint damage, collider/marker/shield swaps, CIS-only dependencies and the saved level-5 wreck.
- Results: `Temp/CisStationImport/StationTests.json`, `CisStationTests.json`, `StationLevelTests.json`. Verify captured test names when sharing the Editor; counts alone can belong to another run.

## Edge Cases

- Turrets are embedded in one hull; XML has no separate `Model_To_Attach`. `art: null` is deliberate; individual destroyed turret artwork cannot hide without splitting source geometry.
- No proton anchors. Existing proton IDs **8/10/15** explicitly reuse CCM anchors; level-5 IDs **14/15/16** reuse earlier locations. Combat profiles remain unchanged.
- Source has **2/2/2/4/4** exact zero-area triangles. Preserve their geometry/UVs; exclude only these faces from authored-normal equality because Blender cannot represent their split normals.
- ALAMO simplifies shadow/collision helpers; exclude helper topology/normals from exact source equality.
- Fixed faction wreck: `SeparatistSpaceStationWreckView.prefab`, rebuilt from **level-5 hull**. The shared builder selects only the highest level; all levels still use this single wreck.
- No source damaged/wreck model, ALA, proxy particles or animated EaW light/destruction shaders converted. No full battle playthrough recorded.

## Files

- `Tools/Blender/CisSpaceStation/` — audit, source normal restoration, staging, baseline/mapping verifiers and exact rebuild procedure.
- `Tools/Blender/SpaceStations/Separatist.json` — source anchors and level mapping.
- `Assets/Prefabs/Models/Stations/SeparatistSpaceStationView.prefab` — saved gameplay level configuration.
- `Assets/Art/Models/SpaceStations/CisSpaceStation/LevelN/CisSpaceStationLevelN.fbx` — source-derived FBXs.
- `Assets/Art/{Materials,Textures}/Models/SpaceStations/CisSpaceStation/` — shared art.
- `Assets/Art/Models/Shields/CisSpaceStationLevelNShield.asset` — per-level shield surfaces.
- `Assets/Scripts/Tests/Editor/CisStationImportTests.cs`, `StationLevelViewTests.cs` — fidelity, materials, faction isolation and real upgrade handler.
- `Temp/CisStationImport/` — packed editable blends, audits, raw Unity data, verified reports, tests and previews.
