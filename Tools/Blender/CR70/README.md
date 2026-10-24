# CR70 Charger Corvette

- Source: **Republic at War Workshop `1129810972`**, installed version `1.5.5`.
- Correct hull: `Data/Art/Models/ReV_light_frigate.ALO`. `Units_Space_republic_light_frigate.xml` identifies the C-70; `Hardpoints_Republic_Space.xml` labels its section “Charger c70 (Light Frigate).”
- Model and all three DDS textures come directly from this mod. No base-game/Forces of Corruption archive fallback. The earlier vanilla `RV_CORVETTE.ALO` substitution was rejected and replaced; its 11 unused material/texture assets were removed through Unity.
- Guides: vault `AssetPipeline/Guides/ALO_MODEL_IMPORT_GUIDE.md`, `Architecture/PROJECT_ORGANIZATION.md`. Corrected evidence/editable blend: `Temp/CR70ChargerImport/`.

## Conversion

1. Run `uv run --with pillow python Tools/Blender/CR70/Prepare.py`.
2. Import the mod ALO into a fresh `CR70 Charger Source` Blender scene through MCP; run `Convert.py` through MCP. ALAMO may fail during global helper cleanup with other scenes open; the converter completes cleanup locally.
3. Capture printed `CHARGER_REPORT` JSON as `Temp/CR70ChargerImport/ConversionReport.json`. Run `VerifyGeometry.py` through MCP and capture `before` triangle corners as `SourceGeometry.json` for the Unity check.
4. Run `uv run python Tools/Blender/CR70/Stage.py`; it stages only manifest-listed model textures.
5. Run the Unity scripts below with `unity command run_script --file Tools/Blender/CR70/<File>.cs --entry <Class>.Main --json`.

- Source has `5` meshes / `4,222` triangles / `14` bones. Opaque `Hull` + `Object02`: `4,161` triangles; visible additive `Engine`: `48`; hidden `Engines` + `Collision`: `13`.
- Binary-backed bone parents restored after the importer stopped during cleanup. Identity Root already exists; no extra bone added.
- Source shaders/material slots audited independently; exact five material assignments restored before FBX export. Engine additive tint preserves source RGB `1/0.996078/0.549020`.
- DDS→PNG conversions are pixel-identical. Hull/normal `1024²`, engine emission `64²`; inverse hull alpha supplies the team mask. Team-mask strength is `0.35` on living and wreck materials, reduced from `1` per user feedback. Normal import flips green; mask uses linear sampling.
- Gameplay visual: `9.223910 × 2.717261 × 15.749785` units, centered, bow `+Z`/up `+Y`, root identity. Uniform length matches the current Corvette gameplay scale.

## Integration

1. `BuildArt.cs` → `BuildCR70Art.Main`.
2. `VerifyUnityGeometry.cs` → `VerifyCR70UnityGeometry.Main`.
3. `BuildShip.cs` → `BuildCR70Ship.Main`.
4. `Render.cs` → `RenderCR70.Main`.
5. `Register.cs` → `RegisterCR70.Main`.
6. `CleanupRejectedHull.cs` → `CleanupCR70RejectedHull.Main` (idempotent cleanup of the rejected import only).
7. `Verify.cs` → `VerifyCR70.Main`.

- `ShipType.CR70 = 13`; existing CR70 prefab/data/icon GUIDs retained. Republic roster, data/view Addressables, icons, matchups and own reinforcement mapping remain registered.
- Requested stats: hull/shields `750/650`, cost/population `1,250/2`. Source values differ; the user's requested configuration wins.
- Twelve targetable IDs: eight logical lasers = two per authored `HP01..HP04`; two PD at `HP05/HP06`; engine at `Engines`; generator fitted to the actual dorsal hull above its `Hull` bone. No vanilla mount positions retained.
- Source socket represents a paired gun mount; paired emitters share that authored socket. The generator has no source socket, so its fitted surface point is a project choice.
- Existing Power to Engines `14`: active/recovery `20/50 s`, speed ×2, weapon delay ×3, regeneration ×0.
- Existing gameplay defaults retained: speed `40`, turn/acceleration `60/60`, range `300`, bank `15°`, height tier `7`, regeneration `650/120 = 5.416667/s` after `1 s`; build `15 s`, level `2`, roster cap `10`.
- `UnitRenderStateComponent.meshRenderers` owns the new hull/effect/shield renderers. Current shader-based wreck copies the two opaque hull meshes and retains team masks; lifetime/dissolve `30/8 s`.
- Shield, collision, selection, ion bounds, VFX anchors, placement hologram and 512-pixel icon/silhouette are fitted to the Charger.

## Verification

- Blender source→FBX corners: position error ≤`0.000018120` source units; UV error ≤`0.000001908`; bone error ≤`0.000019074`.
- Unity: all five mesh triangle counts, UV corners, material slots/remaps and 14 bone positions/parents pass. Geometry/bone error ≤`0.000000457` / `0.000000299` project units.
- Saved gameplay verification passes: exact stats, 12 hardpoints, own-model sockets, ownership values 1–8, wreck, preview, icons, registrations, missing/broken references and absence of vanilla Corvette visual dependencies.
- All eight living/wreck palettes rendered from the configured linear palette. Blue/green, three-engine stern, placement and wreck inspected.
- Unity import/production compilation complete with no new Console errors on `2026-10-24`. The temporary verifier has one obsolete-API warning.
- No automated Unity tests or Play Mode run; runtime combat remains untested.
