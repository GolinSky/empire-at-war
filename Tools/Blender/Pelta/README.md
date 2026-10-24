# Pelta-class Frigate

- Source: Republic at War Workshop `1129810972`; `Pelta_Frigate` → `Data/Art/Models/ReV_Pelta.ALO`. Original files and source hashes are preserved.
- Vault reference: `AssetPipeline/Imports/Pelta Import.md`; workflow: `AssetPipeline/Guides/ALO_MODEL_IMPORT_GUIDE.md`.
- Unity FBX: `Assets/Art/Models/RepublicShips/Pelta/Pelta.fbx`; gameplay prefab: `Assets/Prefabs/Models/Ships/Republic/PeltaShipView.prefab`.
- Republic `ShipType.Pelta = 14`; hull `2200`, shields `1000`, price `1800`, population `2`, build `20 s`; four turbolasers, four lasers, two ion cannons.
- Geometry: 11 source meshes, 18,818 triangles, 45 bones; six visible meshes in game, including three opaque hull pieces totaling 2668 triangles. Source helpers remain only in the transport FBX/blend.
- Team paint uses source-red HSV selection; neutral hull/yellow details remain intact. All eight live/wreck palettes render through the current ownership system.
- Current wreck builder uses three hull meshes with GPU splitting/drift/dissolve. Wreck data references its Addressable prefab; no hard wreck-prefab dependency is introduced.
- Ten logical weapons retain all source attachment bones. The four paired turbolasers use socket-pair midpoints because the existing weapon view exposes one emitter per weapon. No combat API changes.
- Unspecified gameplay choices: length `45`, speed `30`, turn `45`, bank `15`, range `500`, level `2`, cap `20`, regeneration `1000/120` per second after `1 s`. No abilities or hangar.

## Conversion

1. `uv run --with pillow python Tools/Blender/Pelta/Prepare.py` audits binary meshes/materials/bones, decodes textures and preserves source hashes.
2. Through Blender MCP, create/select `Pelta Source` and import `ReV_Pelta.ALO` with `importAnimations=False`. The upstream importer's global shadow cleanup can stop on objects belonging to another scene; do not retry the import into the same scene.
3. Run `Convert.py` through Blender MCP with `AUDIT` injected as the parsed `Temp/PeltaImport/SourceAudit.json` object. Safe mode prevents reading files from the Blender script. The converter completes this scene's hierarchy/material repair, exports FBX/blend and imports a separate validation copy. Save its `PELTA_REPORT` JSON as `Temp/PeltaImport/ConversionReport.json`.
4. Run `VerifyGeometry.py` through Blender MCP for full source/FBX UV-corner comparison. `VerifyUnityGeometry.cs` consumes source corners stored in `Temp/PeltaImport/SourceGeometry.json`; recorded verification used every opaque corner and sampled helper corners.
5. `uv run python Tools/Blender/Pelta/Stage.py` stages the FBX and manifest-listed PNGs.

## Unity integration

Run with `unity command run_script --file Tools/Blender/Pelta/<File>.cs --entry <Entry>.Main --timeout_ms 60000 --json`:

| File | Entry |
| --- | --- |
| BuildArt | BuildPeltaArt |
| VerifyUnityGeometry | VerifyPeltaUnityGeometry |
| BuildShip | BuildPeltaShip |
| Render | RenderPelta |
| Register | RegisterPelta |
| Verify | VerifyPelta |

- Check nested `data.result.success`; the outer CLI envelope can succeed when a script fails.
- Builders save/import assets directly. Rendering uses isolated preview scenes and restores global palette/ambient settings.
- `Evidence/` records source, conversion, Unity geometry, saved-reference and regression-test results.
- Saved Pelta verification and both automatically discovered Pelta wreck tests pass. Team-color tests `2/2`, placement tests `3/3` and weapon-loadout tests `5/5` pass. Broader wreck filter `135/138`: unrelated Raider/Tartan tests reference a retired wreck field; MiningFacility has existing livery differences.
- Source deployment/death animations and EaW animated shader behavior are not converted. Play Mode combat has not been run.
