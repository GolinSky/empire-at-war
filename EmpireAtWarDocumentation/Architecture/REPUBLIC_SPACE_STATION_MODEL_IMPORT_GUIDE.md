---
updated: 2026-10-07
tags:
  - unity
  - alo
  - station
  - republic
---
# Republic Space Station Model Import Guide

## Goal
- Import Republic at War station levels 1–5 as distinct original models.
- Route them exclusively through `RepublicSpaceStationView`; preserve shared level, economy, and combat data.

## Rules
- Read [[Architecture/ALO_MODEL_IMPORT_GUIDE]], [[Architecture/PROJECT_ORGANIZATION]], and [[Architecture/SPACE_STATION_MODEL_IMPORT_GUIDE]].
- Source: Workshop `1129810972`, `D:/SteamLibrary/steamapps/workshop/content/32470/1129810972/Data/`. Never modify installed mod files.
- XML names `Empire_Star_Base_1–5` represent Republic units. They do not authorize Empire faction routing.
- Use the existing Rebel ALO reader/converter and shared `BuildStationLevels.cs` workflow.
- Use an isolated Blender process on port `9897`. Verify the listener PID belongs to that process; another import must use a different port.
- Inspect nested `data.result.success` after Unity scripts. Builders must save assets; no manual Editor save is required.

## Decision
- Chosen: five visual prefabs nested in the existing Republic gameplay prefab.
- Why: `SpaceStation.ApplyLevel` already calls `StationLevelView.ApplyLevel` during initialization and faction upgrades.
- Avoid: new runtime level systems, duplicate faction mapping entries, per-level centering, or changes to shared balance data.
- Keep the existing 17 gameplay hardpoints and unlock levels. Mount them on verified source anchors.
- Source weapon art is embedded in cumulative hull meshes; `StationMount.Art=null` is intentional. Destroyed hardpoints stop functioning while embedded art remains visible.

## Implementation

### Source and level mapping
- Models: `Data/Art/Models/ReB_Shipyard_Level_01.ALO` through `ReB_Shipyard_Level_05.ALO`.
- Definitions: `Data/XML/StarBases_Republic.xml` and `Hardpoints_Republic_Starbase.xml`.
- Textures: `Data/Art/Textures/`; references resolve to DDS files despite some XML/material references using `.tga`.

| Level | Original ALO | Visual prefab | Gameplay configuration |
|---|---|---|---|
| 1 | `ReB_Shipyard_Level_01.ALO` | `RepublicSpaceStationLevel1.prefab` | `levelModels[0]` |
| 2 | `ReB_Shipyard_Level_02.ALO` | `RepublicSpaceStationLevel2.prefab` | `levelModels[1]` |
| 3 | `ReB_Shipyard_Level_03.ALO` | `RepublicSpaceStationLevel3.prefab` | `levelModels[2]` |
| 4 | `ReB_Shipyard_Level_04.ALO` | `RepublicSpaceStationLevel4.prefab` | `levelModels[3]` |
| 5 | `ReB_Shipyard_Level_05.ALO` | `RepublicSpaceStationLevel5.prefab` | `levelModels[4]` |

- All prefabs: `Assets/Prefabs/Models/Stations/`.
- Runtime configuration: `StationLevelView.levelModels` on `RepublicSpaceStationView.prefab`.
- Build configuration: `Tools/Blender/SpaceStations/Republic.json`.
- Routing: `Assets/Settings/AssetMappingData.asset`, key `RepublicSpaceStationView`; retain its existing GUID and Addressables `View` entry.

### Convert and import
1. Run preparation and capture the existing station size/routing before rebuilding.
2. Start the dedicated Blender 3.6.23 process; ALAMO and Blender MCP must be installed/enabled.
3. Confirm port `9897` belongs to the newly started process. Conversion clears that process's objects.
4. Convert each level. Normal payloads are split into requests below the MCP code-size limit; `Call.py` uses the installed MCP dispatcher with safe-mode validation.
5. Restore exact source normals in FBX, then stage models and textures.
6. Build visual prefabs, the gameplay view, shared level bindings, and the Republic wreck.

```powershell
uv run --with pillow python Tools/Blender/RepublicSpaceStation/Prepare.py
unity command run_script --file Tools/Blender/RepublicSpaceStation/VerifyMapping.cs --args '[true]' --json
Start-Process -FilePath "$env:LOCALAPPDATA/AI-Tools/Blender/blender-3.6.23-windows-x64/blender.exe" -ArgumentList '--python','F:/Private/empire-at-war/Temp/RepublicStationImport/Start.py' -WindowStyle Hidden
Get-NetTCPConnection -LocalPort 9897 -State Listen
./Tools/Blender/RepublicSpaceStation/ConvertLevels.ps1
uv run --with numpy --with pillow python Tools/Blender/RepublicSpaceStation/RestoreNormals.py
uv run --with pillow python Tools/Blender/RepublicSpaceStation/Stage.py
unity command run_script --file Temp/RepublicStationImport/BuildArt.cs --json
unity command run_script --file Temp/RepublicStationImport/BuildView.cs --json
unity command run_script --file Tools/Blender/SpaceStations/BuildStationLevels.cs --args '["Republic"]' --json
unity command run_script --file Tools/Blender/RepublicSpaceStation/BuildWreck.cs --json
```

### Hierarchy, orientation, and materials
- Restore the verified identity `Root` bone and index-based parent hierarchy; ALAMO drops the root and misresolves repeated names.
- All levels retain 204 source bones. Unique FBX transport names are restored to authored repeated names in Unity visual prefabs.
- Static FBX bone axes use a common orientation to avoid Euler/gimbal drift. Original world matrices remain in `EaWSourceWorldMatrix`; the Unity builder restores authored rotations while preserving mesh world poses.
- ALAMO skips stored vertex normals; preparation reads original normal bytes. Blender quantizes custom normals and changes degenerate-face normals; `RestoreNormals.py` writes exact normals into the FBX using Blender's native FBX parser/encoder.
- Import normals/tangents; disable animation, vertex welding, mesh compression, and hierarchy optimization. Keep hierarchy and readable meshes.
- Hull: `EmpireAtWar/Ship Lit`; normal map green channel flipped. Lights use the existing additive convention.
- Team mask = source hull alpha; `_TeamMaskStrength=1`, `_TeamLiveryStrength=0`, `_TeamRimStrength=0`.
- Six original texture references: `ReB_shipyards`, `ReB_Shipyards_B`, `ReB_shipyards_L`, `NB_SHIELDBASE`, `NB_SHIELDRIPPLE`, `NB_SHIELDWAVE`. All were present; staged PNG pixels equal decoded DDS pixels.

### Gameplay setup
- `BuildStationLevels` assigns hull renderers/bounds, typed mounts, a launch exit, and each fitted shield to `StationLevelModel`.
- `StationLevelView` enables exactly one level, updates the collider, mounts, launch point, selection marker, and shield. Entity integration updates explosion and ionization hull renderers.
- Team/fog bindings include every nested level renderer; fog reveal bounds contain the maximum-level hull.
- Launch anchor: `HP_Spawn_01`; `GameplayLaunchExit` lies 8 units below each hull's collider.
- Shield meshes: `Assets/Art/Models/Shields/RepublicSpaceStationLevelNShield.asset`; 1024 fitted planes per level.
- Wreck: `Assets/Prefabs/Models/Wrecks/RepublicSpaceStationWreckView.prefab`. Existing wreck data/reference remains; rebuild materials and geometry from level 5 only.
- `ShipWreckBuilder` uses the final configured hull for leveled stations; copying every nested level would duplicate overlapping geometry.

### Verification
```powershell
unity command run_script --file Temp/RepublicStationImport/Verify.cs --json
uv run --with pillow python Temp/RepublicStationImport/VerifyGeometry.py
unity command run_script --file Tools/Blender/RepublicSpaceStation/VerifyMapping.cs --args '[false]' --json
unity command run_script --file Temp/RepublicStationImport/Render.cs --json
unity command list_open_scenes --json
unity command run_tests --mode editor --filter StationLevelViewTests --async_tests true --json
unity command test_status --json
```

- Before tests, every open scene must be clean. Save dirty named scenes and recheck; dirty untitled scenes → `BLOCKED_DIRTY_UNTITLED_SCENE`.
- Poll tests to completion. Do not mutate/import assets while tests run.
- Inspect all five models and contrasting team colors. Preview images: `Temp/RepublicStationImport/Previews/LevelNTeam-1..7.png`.
- 2026-10-07: all five FBXs pass original binary hierarchy, geometry, UV, and normal comparison. Six prefabs reload without missing scripts or broken references.
- Maximum raw Unity displacement: vertices `0.000003556` units; bones `0.000002218` units. Maximum UV delta `0.000000061`; normal vector delta `0.000000299`.
- Mapping verifier exercises `1→5→2→3→4→5→1`, checks authored rotations, colliders, mounts, shields, faction isolation, and preserved shared configuration.
- Tests: broader station filter `96/96`; `StationLevelViewTests 60/60`; Republic filter `22/22`, including wreck/material synchronization.
- Original ALO/DDS/XML hashes unchanged; imports and assets saved.

## Important Values
- FBX importer scale: `0.02`; shared nested scale: `14.926302`; gameplay and visual roots: `1,1,1`.
- Level 5 XZ diameter: `299.3029` project units, matching the previous Republic station.
- Common level-1 pivot preserves shared anchor positions during upgrades.
- Hull triangles by level: `6800 / 9236 / 9744 / 14160 / 16960`; levels add their own original meshes.
- Gameplay mounts by level: `4 / 7 / 10 / 13 / 17`.
- Hull texture: `2048×2048`; nonzero team-mask coverage: `0.957298%`.

## Edge Cases
- Levels 1–3 look similar at a distance; their original cumulative hull triangle totals and mesh counts differ. Do not substitute a single scaled model.
- IDs 9 and 10 reuse already-visible missile/torpedo anchors at level 3 because the corresponding new source pieces appear at later levels. Keep existing gameplay unlock rules; this import does not rebalance RaW hardpoint composition.
- Helper shield/collision/shadow meshes stay in source conversion files; disabled leaf helper renderers are removed from gameplay prefabs. Gameplay uses a box collider and fitted project shields.
- ALAMO welds collision/shadow helpers and removes degenerate helper faces; helper topology is excluded from original geometry equality checks.
- Static import only: no ALA animations, EaW animated shield behavior, particles, or destruction shaders. Original rotations are restored in Unity; Blender transport axes remain normalized.
- Embedded weapon art cannot disappear independently when a hardpoint is destroyed. Separate art would require a deliberate mesh-authoring task.
- Wreck uses the final-level hull for every level; the existing wreck system has one Republic station wreck reference.
- Full battle/Play Mode integration was not run; Editor tests and saved-asset verification were run.

## Files
- Models: `Assets/Art/Models/SpaceStations/RepublicSpaceStation/LevelN/RepublicSpaceStationLevelN.fbx`.
- Materials/textures: `Assets/Art/{Materials,Textures}/Models/SpaceStations/RepublicSpaceStation/`.
- Prefabs: `Assets/Prefabs/Models/Stations/RepublicSpaceStationLevel1–5.prefab`, `RepublicSpaceStationView.prefab`.
- Builders: `Tools/Blender/RepublicSpaceStation/`, `Tools/Blender/SpaceStations/BuildStationLevels.cs`, `Republic.json`.
- Evidence: `Temp/RepublicStationImport/{SourceAudit,ConversionReport,VerifiedSourceGeometry,VerifiedMapping,StationLevelTests,RepublicTests}.json`.

## TODO
- Optional focused refactor: extract material asset writing from `ShipWreckBuilder` into an Editor helper; the existing builder exceeds the project's 200-line guideline. Keep this outside the station import scope.
