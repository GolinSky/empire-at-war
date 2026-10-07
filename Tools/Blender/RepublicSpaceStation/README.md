# Republic Station Import

Dedicated setup/rebuild guide: [Republic Space Station Model Import Guide](../../../EmpireAtWarDocumentation/Architecture/REPUBLIC_SPACE_STATION_MODEL_IMPORT_GUIDE.md).

- Source: Republic at War, Workshop `1129810972`, `ReB_Shipyard_Level_01–05.ALO`.
- Faction: Republic only. Source XML names `Empire_Star_Base_1–5` are Republic units.
- Prefabs: `Assets/Prefabs/Models/Stations/RepublicSpaceStationLevel1–5.prefab`.
- Runtime mapping: `RepublicSpaceStationView.prefab`, `StationLevelView.levelModels[0..4]`.
- Build mapping: [Republic.json](../SpaceStations/Republic.json).
- Reuses the Rebel binary reader/converter and shared station builder. Uses a dedicated Blender MCP process on port `9897` with safe-mode validation.

Run from the project root in the guide's order:

1. `Prepare.py` audits original models/XML/textures and generates conversion/build/verification scripts in `Temp/RepublicStationImport`.
2. `VerifyMapping.cs [true]` captures faction routing, maximum station size, hardpoint profiles, and shared-asset hashes.
3. Start `Temp/RepublicStationImport/Start.py` in a separate Blender 3.6.23 process and verify its listener PID.
4. `ConvertLevels.ps1` converts each model and validates its FBX geometry, UVs, and hierarchy.
5. `RestoreNormals.py` restores exact ALO normals using Blender's native FBX parser/encoder; run with `uv run --with numpy --with pillow python`.
6. `Stage.py` copies validated FBXs/PNG pixels; generated `BuildArt.cs`, `BuildView.cs`, shared `BuildStationLevels.cs ["Republic"]`, and `BuildWreck.cs` save all Unity assets.
7. Generated `Verify.cs`, `VerifyGeometry.py`, `VerifyMapping.cs [false]`, and `Render.cs` verify source fidelity, faction isolation, saved references, level changes, and appearance.

Embedded weapon art cannot hide independently when destroyed. Static conversion excludes ALA animations and EaW shield/destruction effects. The existing single station wreck uses the final-level hull.
