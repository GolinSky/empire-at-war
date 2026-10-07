# Imperial space station import

Dedicated guide: `Architecture/SPACE_STATION_MODEL_IMPORT_GUIDE.md` in `empire-vault`.

## Outputs

| Level | Original model | Visual prefab | Configuration element |
| --- | --- | --- | --- |
| 1 | `EB_STATION_01.ALO` | `EmpireSpaceStationLevel1.prefab` | `levelModels[0]` |
| 2 | `EB_STATION_02.ALO` | `EmpireSpaceStationLevel2.prefab` | `levelModels[1]` |
| 3 | `EB_STATION_03.ALO` | `EmpireSpaceStationLevel3.prefab` | `levelModels[2]` |
| 4 | `EB_STATION_04.ALO` | `EmpireSpaceStationLevel4.prefab` | `levelModels[3]` |
| 5 | `EB_STATION_05.ALO` | `EmpireSpaceStationLevel5.prefab` | `levelModels[4]` |

- Visual/gameplay prefabs: `Assets/Prefabs/Models/Stations/`.
- Level configuration: `StationLevelView.levelModels` on `EmpireSpaceStationView.prefab`.
- Routing: `Assets/Settings/AssetMappingData.asset`, key `EmpireSpaceStationView`; same address in the existing Addressables `View` group.
- Meshes: `Assets/Art/Models/SpaceStations/EmpireSpaceStation/LevelN/EmpireSpaceStationLevelN.fbx`; separate pieces in `Attachments/`.
- Materials/textures: `Assets/Art/{Materials,Textures}/Models/SpaceStations/EmpireSpaceStation/`.
- Shields: `Assets/Art/Models/Shields/EmpireSpaceStationLevelNShield.asset`.
- Packed editable blends, audits, FBXs, generated scripts, reports and renders: `Temp/EmpireStationImport/`.

## Rebuild

Use the repository root, official Unity CLI and installed Blender 3.6.23 with ALAMO and `mcp-for-blender==2.1.3`. Read `AGENTS.md`, the station guide and `PROJECT_ORGANIZATION` through `empire-vault`. Work in Edit Mode. Keep Blender MCP safe mode enabled.

`Prepare.py` reuses the binary reader, MEG extraction and reviewed conversion/build scripts in `Tools/Blender/RebelSpaceStation/`. It creates Imperial adapters in `Temp`, leaving the Rebel source scripts intact. It extracts only referenced Imperial attachment models and any missing textures.

```powershell
uv run --with pillow python -B Tools/Blender/EmpireSpaceStation/Prepare.py
unity command run_script --file Tools/Blender/EmpireSpaceStation/VerifyEmpireStationMapping.cs --args '[true]' --json
```

Inspect processes and port `9886` first. Reuse only a verified session belonging to this import; stop on an unrelated listener. If absent:

```powershell
Start-Process -FilePath "$env:LOCALAPPDATA/AI-Tools/Blender/blender-3.6.23-windows-x64/blender.exe" -ArgumentList '--python','F:/Private/empire-at-war/Temp/EmpireStationImport/Start.py' -WindowStyle Hidden
```

Confirm version, `bpy.ops.import_mesh.alo` and port `9886` through the generated MCP client before conversion. The converter clears only this isolated session's scene. Submit one model per request:

```powershell
$audit = Get-Content Temp/EmpireStationImport/AttachmentAudit.json -Raw | ConvertFrom-Json
$names = @(1..5 | ForEach-Object { "EmpireSpaceStationLevel$_" }) + @($audit.models.PSObject.Properties.Name)
foreach ($name in $names) {
    uvx --from mcp-for-blender==2.1.3 python Temp/EmpireStationImport/Call.py "Temp/EmpireStationImport/$name.json" 1> "Temp/EmpireStationImport/$name.log" 2> "Temp/EmpireStationImport/$name.mcp.log"
    if ($LASTEXITCODE -ne 0) { throw "Conversion failed: $name" }
}
uv run --with pillow python -B Tools/Blender/EmpireSpaceStation/Stage.py
unity command run_script --file Temp/EmpireStationImport/BuildEmpireStationArt.cs --json
unity command run_script --file Temp/EmpireStationImport/BuildEmpireStationAttachments.cs --json
unity command run_script --file Temp/EmpireStationImport/BuildEmpireStationView.cs --json
unity command run_script --file Tools/Blender/SpaceStations/BuildStationLevels.cs --args '["Empire"]' --json
unity command run_script --file Temp/EmpireStationImport/VerifyEmpireStations.cs --json
uv run --with pillow python -B Temp/EmpireStationImport/VerifyGeometry.py
unity command run_script --file Tools/Blender/EmpireSpaceStation/VerifyEmpireStationMapping.cs --args '[false]' --json
unity command run_script --file Temp/EmpireStationImport/RenderEmpireStations.cs --json
uv run --with pillow python -B Tools/Blender/EmpireSpaceStation/VerifyTeamColors.py
unity command console --level Error --json
```

Inspect nested `data.result.success` after every Unity script. Inspect all five renders and contrasting team colors. Builders save all assets; no manual Editor save is required. Run Unity tests only when requested, following the scene safety rules in `AGENTS.md`.

## Decisions

- Each base uses its actual `EB_STATION_0N.ALO`, with original `EB_Station`, `EB_Station_bump`, `EB_stationlights` and `W_blast00` textures.
- XML resolves 18 unique attachment ALOs and 5/8/11/14/18 placed pieces at levels 1-5. Source files are preserved; `AttachmentAudit.json` records hashes and bone names.
- Verified identity root restoration and index-based bone repair reuse the existing workflow. Unique FBX transport names are restored to original repeated names in visual prefabs.
- Source UVs are retained, with no Rebel dome repair. Hull uses `EmpireAtWar/Ship Lit`, direct source-alpha team mask, green-flipped normal and existing additive light convention. Team mask coverage: 0.640869%. Set `_TeamRimStrength=0` so team ownership recolors the authored stripes without washing the gray hull in team color.
- FBX scale `0.02`; common nested scale `9.116956`; gameplay/visual roots scale 1. Level 5 base XZ diameter matches the previous station's `299.3029` units; complete attachment-inclusive diameter is `302.8595` units.
- Preserve 17 existing gameplay hardpoint profiles/unlock levels and the hangar exit. Imperial anchors (`Tools/Blender/SpaceStations/Empire.json`) use `CM`, retain `HP01_SHG_Bone` at every level and retain `FP02_TBL_00` at level 5. Exit is 8 units below each collider.
- Shared station levels, economy, combat data and other faction prefab/routing references remain unchanged. Existing Empire wreck configuration remains unchanged; no Imperial wreck was imported.

## Verification And Limits

- 2026-10-07: all 23 FBXs pass original-binary mesh/UV/bone comparisons; six saved prefabs reload without missing scripts or broken references.
- Base bone counts: 63/87/110/134/162. Base hull triangles: 2405/2901/3741/4679/8201, plus lights/damage and separate artwork.
- Maximum raw Unity vertex/bone displacement: 0.000006174/0.000005185 units. Maximum UV delta: 0.000034060, below 0.018 pixels at 512px.
- All original ALO/DDS hashes unchanged; four staged PNGs equal decoded DDS pixels exactly.
- Mapping verifier exercises levels 1/5/2/3/4/5/1, validates model identity, collision/attachment updates, shields, fog/team bindings, original hardpoint IDs/unlock levels and unchanged shared configuration.
- All five levels rendered in eight palettes plus an unowned reference (`LevelNTeam-1.png`); each level and blue/green ownership inspected. No new import/serialization errors; compilation ready. Unity test suite and full battle were not run.
- 2026-10-07 team-color correction: isolated mask/rim renders reproduced the broad tint from `_TeamRimStrength=0.6`; the GPU mask red-channel mean correctly matched source alpha coverage. Disable the rim only on the Imperial hull material. Apply to existing imports with `unity command run_script --file Tools/Blender/EmpireSpaceStation/FixEmpireStationTeamColors.cs --json`; future `BuildEmpireStationArt.cs` runs set the same value.
- All 40 owned renders change only 0.468-4.200% of visible pixels compared with unowned references (channel delta >4/255); at least 95.8% of the hull remains unchanged. `VerifyTeamColors.py` rejects missing recoloring or changes over 10% of visible pixels.
- ALAMO welds shadow/collision helpers and drops degenerate faces. Those helpers stay in the source FBXs and are excluded from source-topology equality checks; gameplay uses its existing box collider and new fitted shields.
- No ALA animation or damaged/wreck conversion. Source damage overlays remain disabled; EaW destruction shaders, animated lights and proxy particles are not recreated. Attached hardpoint art remains static.
