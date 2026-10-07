# Rebel space station import

Dedicated guide: [SPACE_STATION_MODEL_IMPORT_GUIDE](../../../EmpireAtWarDocumentation/Architecture/SPACE_STATION_MODEL_IMPORT_GUIDE.md).

These scripts rebuild the five **vanilla Rebel** station models and their Rebellion-only level mapping. They are model-specific; other stations require a fresh binary audit and explicit attachment mapping.

**Known issue (2026-10-07):** the user still sees the reported albedo/surface problem after the UV repair attempt. Current assets are retained at the user's request. Passing structural tests and isolated preview renders do not establish that the visual issue is resolved.

## Inputs and outputs

- Source: `output/aotr-space-stations/preview-selection/Vanilla/Data/ART/MODELS/RB_STATION_01.ALO` through `RB_STATION_05.ALO`.
- Textures: sibling `TEXTURES` folder; only `RB_Station`, `RB_Station_Bump`, `RB_Stationlights`, and `W_blast00` are referenced.
- Working files, editable blends, source hashes, conversion reports and previews: `Temp/RebelStationImport/`.
- FBXs: `Assets/Art/Models/SpaceStations/RebelSpaceStation/Level1/` through `Level5/`.
- Separate hardpoint FBXs: `Assets/Art/Models/SpaceStations/RebelSpaceStation/Attachments/` (20 original models).
- Corrected display meshes: `LevelN/RebelSpaceStationLevelNHull.asset`; original FBX UVs remain unchanged.
- Visual prefabs: `Assets/Prefabs/Models/Stations/RebelSpaceStationLevel1.prefab` through `RebelSpaceStationLevel5.prefab`.
- Gameplay prefab: `Assets/Prefabs/Models/Stations/RebellionSpaceStationView.prefab`.
- Material/texture folders: `Assets/Art/{Materials,Textures}/Models/SpaceStations/RebelSpaceStation/`.
- Shields: `Assets/Art/Models/Shields/RebelSpaceStationLevel1Shield.asset` through `RebelSpaceStationLevel5Shield.asset`.

## Rebuild

Run from the repository root. Follow `AGENTS.md` and the vault ALO guide. Use the installed Blender 3.6.23, ALAMO importer and `mcp-for-blender==2.1.3`. Keep MCP safe mode enabled. `Call.py` reads the existing project MCP configuration; it changes only the child process's Blender port to `9885`.

1. Audit originals and stage lossless PNGs:

```powershell
uv run --with pillow python Tools/Blender/RebelSpaceStation/Prepare.py
```

2. Inspect port `9885` before starting. Reuse this import's dedicated session if present; do not start a competing listener. Otherwise launch an isolated Blender process:

```powershell
Start-Process -FilePath "$env:LOCALAPPDATA/AI-Tools/Blender/blender-3.6.23-windows-x64/blender.exe" -ArgumentList '--python','F:/Private/empire-at-war/Tools/Blender/RebelSpaceStation/Start.py' -WindowStyle Hidden
```

3. Verify the dedicated session's version, ALAMO operator and scene through MCP before conversion. `Convert.py` clears that session's current scene. The normal shared Blender session on port `9876` is not used. Submit one level per request to stay within MCP's AST limit:

```powershell
1..5 | ForEach-Object {
    python Tools/Blender/RebelSpaceStation/ConvertRequest.py $_
    uvx --from mcp-for-blender==2.1.3 python Tools/Blender/RebelSpaceStation/Call.py "Temp/RebelStationImport/ConvertRequest$_.json" 1> "Temp/RebelStationImport/Conversion$_.log" 2> "Temp/RebelStationImport/Mcp$_.log"
    if ($LASTEXITCODE -ne 0) { throw "Station conversion $_ failed" }
}
uv run --with pillow python Tools/Blender/RebelSpaceStation/Stage.py
```

4. Extract the station's separate artwork from the original game's `models.meg`, following `STARBASES.XML` → `HARDPOINTS.XML` in `config.meg`. `PrepareAttachments.py` uses `D:/SteamLibrary/steamapps/common/Star Wars Empire at War/GameData/Data`; adjust its `GAME` path for another installation. It produces the explicit checked-in `Attachments.json` level/bone mapping. Convert each of the 20 unique pieces separately:

```powershell
uv run --with pillow python -B Tools/Blender/RebelSpaceStation/PrepareAttachments.py
Get-ChildItem -LiteralPath Temp/RebelStationImport/Attachments -Filter RB_Station*.json | ForEach-Object {
    $request = $_
    uvx --from mcp-for-blender==2.1.3 python Tools/Blender/RebelSpaceStation/Call.py $request.FullName 1> "Temp/RebelStationImport/Attachments/$($request.BaseName).log" 2> "Temp/RebelStationImport/Attachments/$($request.BaseName).mcp.log"
    if ($LASTEXITCODE -ne 0) { throw "Attachment conversion failed: $($request.Name)" }
}
python -B Tools/Blender/RebelSpaceStation/StageAttachments.py
```

5. In Unity Edit Mode, execute each command and inspect its nested `result.success` before continuing. Always run **all five builders in this order**: recreate base art, attach missing source pieces, repair the dome UV strip, rebuild the gameplay view, then apply the shared level pivot, hardpoint mounts and fitted shields.

```powershell
unity command run_script --file Tools/Blender/RebelSpaceStation/BuildArt.cs --json
unity command run_script --file Tools/Blender/RebelSpaceStation/BuildAttachments.cs --json
unity command run_script --file Tools/Blender/RebelSpaceStation/RepairDomeUv.cs --json
unity command run_script --file Tools/Blender/RebelSpaceStation/BuildView.cs --json
unity command run_script --file Tools/Blender/SpaceStations/BuildStationLevels.cs --args '["Rebellion"]' --json
unity command run_script --file Tools/Blender/RebelSpaceStation/Verify.cs --json
uv run --with pillow python Tools/Blender/RebelSpaceStation/VerifyGeometry.py
unity command run_script --file Tools/Blender/RebelSpaceStation/Render.cs --json
```

6. Inspect the rendered levels, `LevelNCloseup.png` dome views, and contrasting team colors. All builders save their assets; no manual Editor save is required. Inspect the Console for new import/serialization errors.
7. For authorized tests, inspect open scenes first. Save named dirty scenes and inspect again; an untitled dirty scene blocks testing. Then run asynchronously and poll to completion:

```powershell
unity command list_open_scenes --json
unity command run_tests --mode editor --filter Station --async_tests true --json
unity command test_status --json
```

## Conversion decisions

- Restore the verified identity `Root`; reconstruct bone parents and matrices from **binary indices**. ALAMO resolves repeated placeholder names ambiguously, and Blender enumerates bones in hierarchy order rather than source order.
- FBX requires unique bone names. Store the source-name map and restore original repeated names in the Unity visual prefabs without flattening hierarchy.
- Rebuild material slots from original shaders and parameters. Retain authored hidden damage overlays in the FBX and disable them in gameplay. Strip only the disabled shadow-volume renderer leaf from visual prefabs.
- Base station ALOs are incomplete by themselves. Add **5, 8, 11, 14, 18** original hardpoint models for levels 1–5, using each XML `Attachment_Bone`. Compute poses from original bone matrices with the source-to-Unity basis, not FBX bone rotation (which includes Blender bone-axis correction). Retain full rigs/helpers in source FBXs; copy only visible mesh instances under existing named anchors in game prefabs.
- The reported dark dome strip was already present in the original UV layout. `RepairDomeUv.cs` changes **15 UV vertices / 16 triangles** in a derived mesh per level, copying the identical curved plating at +90 degrees. Positions, indices, normals, texture pixels and original FBXs remain unchanged; tangents are recalculated. This is an explicit visual repair, not a lossless source-UV conversion claim.
- ALAMO welds shadow-volume doubles and removes degenerate triangles. Visible hull/light/damage geometry is checked directly against the original ALO; shadow topology is excluded from equality assertions.
- FBX import scale `0.02`; common nested scale approximately `12.3978834`; unit-scale gameplay root. Level 5's XZ diameter matches the previous station's `299.302856` units. The other levels retain their own source dimensions.
- Hull material: `EmpireAtWar/Ship Lit`, direct source-alpha team mask, normal-map green flip. Additive lights use the existing project light-material convention. The alpha mask is **not inverted**.
- Existing 17 gameplay hardpoints and combat/unlock profiles are retained. `Tools/Blender/SpaceStations/Rebellion.json` records their exact source attachment mapping; locked hardpoints have no mount until their unlock level. `GameplayLaunchExit` preserves source `Spawn_00` X/Z and sits 8 units below the current collider.
- Only `AssetMappingData`'s `RebellionSpaceStationView` entry changes faction routing. The existing Addressables `View` group is reused. No new level/economy configuration is introduced.
- No Rebel damaged/wreck ALO or ALA animation was supplied. The obsolete Republic wreck fallback is removed for Rebellion; explosion behavior remains. EaW destruction shaders, animated lights and proxy particles are not recreated.

## Integrity checks

`Verify.cs` reloads all six prefabs, checks references, preserves bone counts/parents, and exports raw Unity vertices/UVs. `VerifyGeometry.py` independently reads binary vertices, UVs and bone matrices, then compares every non-shadow triangle corner against Unity. It also verifies unchanged source hashes and exact decoded DDS-to-PNG pixels.

Expected base source bones by level: `88, 103, 121, 141, 165`. Base hull triangles: `3656, 4235, 4641, 6001, 7407`, plus attachment art. Source file/texture hashes and measured tolerances are saved in `Temp/RebelStationImport/SourceAudit.json` and `VerifiedSourceGeometry.json`.

2026-10-07 surface repair: **38/38 station tests passed**, including five geometry-preserving UV repair cases and attachment count/parent/material checks at every level. All six prefabs reload without broken references; no new import/serialization errors. Source-binary checks apply to raw FBXs; the deliberately corrected display UVs are covered separately by `StationLevelViewTests`.
