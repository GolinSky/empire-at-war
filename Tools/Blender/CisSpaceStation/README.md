# CIS space station import

Dedicated reference: `Architecture/CIS_SPACE_STATION_MODEL_IMPORT_GUIDE.md` in `empire-vault`. Shared station system: `Tools/Blender/SpaceStations/README.md`.

## Level mapping

| Level | Original Republic at War model | Visual prefab | View element |
| --- | --- | --- | --- |
| 1 | `SeB_Station_level_01.ALO` | `CisSpaceStationLevel1.prefab` | `levelModels[0]` |
| 2 | `SeB_Station_level_02.ALO` | `CisSpaceStationLevel2.prefab` | `levelModels[1]` |
| 3 | `SeB_Station_level_03.ALO` | `CisSpaceStationLevel3.prefab` | `levelModels[2]` |
| 4 | `SeB_Station_level_04.ALO` | `CisSpaceStationLevel4.prefab` | `levelModels[3]` |
| 5 | `SeB_Station_level_05.ALO` | `CisSpaceStationLevel5.prefab` | `levelModels[4]` |

- Source: `D:/SteamLibrary/steamapps/workshop/content/32470/1129810972/Data/Art/Models/`; sibling `Textures/`.
- Definitions: `Data/XML/StarBases_Cis.xml` → `Hardpoints_CIS_Starbase.xml`. Internal `Rebel_Star_Base_1–5` names mean CIS in this mod.
- Visual prefabs: `Assets/Prefabs/Models/Stations/`.
- Gameplay and level mapping: `Assets/Prefabs/Models/Stations/SeparatistSpaceStationView.prefab`, `StationLevelView.levelModels`.
- Faction routing: `Assets/Settings/AssetMappingData.asset`, key `SeparatistSpaceStationView`; existing Addressables `View` group and original prefab GUID.
- FBXs: `Assets/Art/Models/SpaceStations/CisSpaceStation/LevelN/CisSpaceStationLevelN.fbx`.
- Materials/textures: `Assets/Art/{Materials,Textures}/Models/SpaceStations/CisSpaceStation/`.
- Shields: `Assets/Art/Models/Shields/CisSpaceStationLevelNShield.asset`.

## Setup and rebuild

1. Read `AGENTS.md`, `ALO_MODEL_IMPORT_GUIDE`, `PROJECT_ORGANIZATION` and the dedicated station guide through `empire-vault`.
2. Use installed Blender **3.6.23**, ALAMO commit `2b0fb0e34e4f451d2e380b042d88ad1a4c8a09b5`, and `mcp-for-blender==2.1.3` / protocol **13**. See `Tools/Blender/README.md` for installation. Keep MCP safe mode enabled and telemetry disabled.
3. Verify the connected project with `unity status --json`; require Edit Mode. Run commands from `F:/Private/empire-at-war`.
4. Audit originals and generate adapters. `Prepare.py` reuses the existing Rebel binary reader/converter/builders, with explicit CIS repairs. It writes only `Temp/CisStationImport/`.

```powershell
uv run --with pillow python -B Tools/Blender/CisSpaceStation/Prepare.py
unity command run_script --file Tools/Blender/CisSpaceStation/CaptureCisStationBaseline.cs --json
```

Inspect processes and port **9887** before starting. Reuse only this import's verified isolated session. Stop on an unrelated listener. If absent:

```powershell
Start-Process -FilePath "$env:LOCALAPPDATA/AI-Tools/Blender/blender-3.6.23-windows-x64/blender.exe" -ArgumentList '--python','F:/Private/empire-at-war/Temp/CisStationImport/Start.py' -WindowStyle Hidden
@{ tool='execute_blender_code'; arguments=@{ code='import bpy
print(bpy.app.version_string)
print(bpy.ops.import_mesh.alo.get_rna_type().identifier)
print(bpy.types.blendermcp_server.port)' } } | ConvertTo-Json -Depth 8 | Set-Content Temp/CisStationImport/Probe.json
uvx --from mcp-for-blender==2.1.3 python Temp/CisStationImport/Call.py Temp/CisStationImport/Probe.json
```

Convert one model per request. Upload normal data in **numeric** chunk order; alphabetical order places chunk 10 before chunk 2. Chunks remain below safe mode's 200,000-byte script limit. The converter clears only this isolated Blender process's objects.

```powershell
foreach ($stationLevel in 1..5) {
    $stationName = "CisSpaceStationLevel$stationLevel"
    Get-ChildItem "Temp/CisStationImport/${stationName}Normals*.json" |
        Sort-Object { [int]($_.BaseName -replace '^.*Normals','') } |
        ForEach-Object {
            uvx --from mcp-for-blender==2.1.3 python Temp/CisStationImport/Call.py $_.FullName 1> ($_.FullName + '.log') 2> ($_.FullName + '.mcp.log')
            if ($LASTEXITCODE -ne 0) { throw "Normal upload failed: $stationName" }
        }
    uvx --from mcp-for-blender==2.1.3 python Temp/CisStationImport/Call.py "Temp/CisStationImport/$stationName.json" 1> "Temp/CisStationImport/$stationName.log" 2> "Temp/CisStationImport/$stationName.mcp.log"
    if ($LASTEXITCODE -ne 0) { throw "Conversion failed: $stationName" }
}
uv run --with pillow python -B Tools/Blender/CisSpaceStation/Stage.py
unity command run_script --file Temp/CisStationImport/BuildArt.cs --timeout_ms 120000 --timeout 180 --json
unity command run_script --file Temp/CisStationImport/BuildView.cs --json
unity command run_script --file Tools/Blender/SpaceStations/BuildStationLevels.cs --args '["Separatist"]' --timeout_ms 120000 --timeout 180 --json
unity command eval --code 'EmpireAtWar.Editor.Rendering.ShipWreckBuilder.Build("Assets/Prefabs/Models/Stations/SeparatistSpaceStationView.prefab");' --json
unity command run_script --file Temp/CisStationImport/Verify.cs --json
uv run --with pillow python -B Temp/CisStationImport/VerifyGeometry.py
uv run --with pillow python -B Tools/Blender/CisSpaceStation/VerifyNormals.py
unity command run_script --file Tools/Blender/CisSpaceStation/VerifyCisStationMapping.cs --json
unity command run_script --file Temp/CisStationImport/Render.cs --timeout_ms 120000 --timeout 180 --json
unity command console --level error --json
```

Inspect **nested `data.result.success`**, not just the CLI exit code. Builders save changed assets and prefabs; verify reloads before delivery. Inspect all five silhouettes and contrasting ownership colors in `Temp/CisStationImport/Previews/`.

Before requested tests, run `unity command list_open_scenes --json`, inspect every scene, and follow `AGENTS.md` dirty-scene rules. Then:

```powershell
unity command run_tests --mode editor --filter Station --async_tests true --json
unity command test_status --json
```

Poll until finished and capture results immediately when the Editor is shared. Inspect the captured test names as well as the counts: another run can overwrite the shared status. Avoid additional Editor automation while a long synchronous test occupies the main thread.

## Verification — 2026-10-07

- Final `--filter Station` Edit Mode suite: **96/96 passed**, including CIS import, all faction level views, map clearance and wreck synchronization.
- Dedicated `CisStationImportTests`: **12/12 passed**, including distinct hulls, imported normals, bone counts, girder cutouts, CIS-only dependencies, embedded hardpoint damage and fixed level-5 wreck.
- `StationLevelViewTests`: **60/60 passed** across all four factions, including the real initial/upgrade handler, one active model, mounts, collider/marker, shields and faction routing.
- Five original ALO/DDS/XML hashes unchanged; five PNGs match decoded DDS pixels exactly.
- All six saved station prefabs reload without missing scripts or broken references. Original binary → Unity geometry, UVs, bone transforms and nondegenerate normals pass.
- Maximum raw vertex/bone displacement: **0.00001089 / 0.00001173 units**; UV delta **0.000000211**.
- Seven level transitions pass; shared station data and existing routing/profile identifiers remain unchanged. All **45** level/team/unowned previews rendered and all five silhouettes inspected.
- Compilation clean; no new Unity import/serialization errors after saving and final verification. No full battle playthrough was performed.
- Reports: `CisStationTests.json`, `StationLevelTests.json`, `VerifiedSourceGeometry.json`, `VerifiedNormals.json`, `VerifiedMapping.json` under `Temp/CisStationImport/`.

## Conversion and integration decisions

- Preserve original ALO/DDS/XML files. Audits record hashes; PNG pixels must match decoded DDS pixels exactly.
- Restore the verified identity `Root`, original parent indices and world bone matrices; restore repeated original bone names in Unity visual prefabs. FBX transport names remain unique.
- ALAMO skips source vertex normals. Restore them from the original binary before export; `VerifyNormals.py` checks Unity triangle corners independently.
- Material identifiers come from the complete source shader/parameter signature, shared across all five levels. Per-model slot numbering would assign girder/damage textures to the wrong material.
- Hull = `EmpireAtWar/Ship Lit`, original alpha team mask, `_TeamRimStrength=0`; normal map = linear, green flipped. Lights use the existing additive convention. Girders use their own alpha-clipped texture.
- Gameplay root = identity rotation/unit scale. The old CIS donor had its mesh on the gameplay root and used scale **9** / rotation **−90° X**; remove only those mesh components and preserve gameplay components/child placement.
- FBX scale **0.02**; common nested scale **9.379914**. Level 5's **380.5378**-unit maximum XZ diameter matches the prior CIS station. The checked-in target prevents shrinking on rebuild.
- Small source origin differences are corrected by aligning `HP01_SHG` across levels. Imported mesh/UV/normal data stays unchanged; only nested visual placement changes.
- Reuse `SpaceStation.ApplyLevel` and `StationLevelView`: swap one active model, collision, marker, shield, launch exit, hardpoint positions, explosion hull and ion bounds. Keep the entity and its gameplay state.
- `Tools/Blender/SpaceStations/Separatist.json` maps all **17** existing hardpoint IDs at their original unlock levels. Retain shared health, weapons, economy, hangar and existing wreck data.

## Limitations and evidence

- No separate `Model_To_Attach` artwork: turrets are part of one source hull mesh. `art: null` is deliberate. Damage affects gameplay but cannot hide a single embedded turret without splitting source geometry.
- Source has four missile-launcher locations, two ion locations and no proton-torpedo anchors. Existing proton IDs **8/10/15** explicitly reuse CCM anchors. Level-5 IDs **14/15/16** reuse earlier source locations; existing profiles/unlock levels remain intact.
- Source bone counts **98/181/183/183/183**; base hull triangles **3442/6364/7180/9603/12869**. Different models may share outer dimensions while gaining geometry.
- ALAMO simplifies shadow/collision helpers. Keep those in source FBXs, exclude them from exact topology/normal checks, and remove them from gameplay visual prefabs.
- Normal equality excludes only exact zero-area source triangles: **2/2/2/4/4** at levels 1–5. Their positions, topology and UVs remain in the geometry check; Blender cannot reproduce their split normals. All other source normals pass with maximum vector error **0.000671**.
- No ALA, proxy particles, animated light/destruction effects or source damaged/wreck models are converted.
- Existing fixed CIS wreck rebuilt from the imported **level-5 hull** at `Assets/Prefabs/Models/Wrecks/SeparatistSpaceStationWreckView.prefab`. `ShipWreckBuilder` explicitly selects the highest level; otherwise all five hulls would overlap. All station levels still use this single faction wreck; per-level wreck selection is not implemented. Existing wreck tuning and faction data stay unchanged.
- Reports, editable packed blends and previews: `Temp/CisStationImport/SourceAudit.json`, `ConversionReport.json`, `VerifiedSourceGeometry.json`, `VerifiedNormals.json`, `VerifiedMapping.json`, `Previews/`.
