# Carrack-class Cruiser

- Source: Republic at War Workshop `1129810972`, `Data/Art/Models/ReV_carrack.ALO`.
- Guides read through empire-vault: `AssetPipeline/Guides/ALO_MODEL_IMPORT_GUIDE.md`, `Architecture/PROJECT_ORGANIZATION.md`, `Rules/UI_UX_GUIDELINES.md`.
- Original ALO/DDS unchanged. Source audit and hashes: `Temp/CarrackImport/SourceAudit.json`.
- Blender MCP 3.6.23: four source meshes, 23,393 triangles, 28 bones. Runtime hull: one mesh, 4,638 triangles. Helpers remain in FBX; their transforms remain in gameplay prefabs without renderers.

## Reproduction

1. `uv run --with pillow python Tools/Blender/Carrack/Prepare.py` audits the binary and converts both DDS textures; inverse hull alpha supplies the team mask.
2. Import the audited ALO into a fresh `Carrack Source` scene through Blender MCP. Preserve all other scenes. The importer may stop during its global helper cleanup; `Convert.py` completes this model's cleanup and restores audited bone parents.
3. Run `Convert.py` through Blender MCP. Capture `CARRACK_REPORT` JSON into `Temp/CarrackImport/ConversionReport.json`. The script embeds the audited source facts because Blender MCP safe mode cannot read JSON files. Update that literal after a deliberate source update.
4. Run `VerifyGeometry.py` through Blender MCP. It compares every source/FBX triangle corner and UV. `SourceGeometry.json` used by the Unity verifier samples every 32nd source triangle corner from its `Geometry` function.
5. `uv run python Tools/Blender/Carrack/Stage.py` stages the FBX and three textures.
6. Run the following with `unity command run_script --file Tools/Blender/Carrack/<file> --entry <entry> --json`: `BuildArt.cs` → `BuildCarrackArt.Main`; `VerifyUnityGeometry.cs` → `VerifyCarrackUnityGeometry.Main`; `BuildShip.cs` → `BuildCarrackShip.Main`; `Render.cs` → `RenderCarrack.Main`; `Register.cs` → `RegisterCarrack.Main`; `Verify.cs` → `VerifyCarrack.Main`.

## Configuration

- `ShipType.Carrack = 15`; Republic level 2; hull/shields `1600/1000`; cost/population `1600/1`; build `20 s`; cap `10`.
- Twelve `DualLaser` weapon mounts at `MuzzleA_00..11`, engine at `Thruster`, generator at `Shieldgen`; 14 unique targetable hardpoints. No hangar.
- Existing Power to Weapons `20`: active/recovery `10/60 s`, firing delay ×0.5, speed ×0.75, incoming damage ×1.5. These project settings differ from RaW XML.
- Retained project defaults: speed `40`, turn/acceleration `60/60`, full-circle arcs, range `300`, bank `15°`, height tier `7`; shield regeneration `1000/120` after `1 s`. Gameplay length `24` units.
- `UnitRenderStateComponent` owns hull/shield rendering. Linear inverse-alpha mask strength `0.35` is retained by the current shader-based wreck; lifetime/dissolve `30/8 s`.
- Own icon, silhouette, placement hologram, shield mesh, wreck, matchups, all runtime mappings and existing Addressables groups are registered.

## Verification — 2026-10-24

- Blender source/FBX: all material slots, triangle corners/UVs, bone names/parents pass; maximum position/UV/bone error `0.000015691/0.0000007153/0.000014306` source units.
- Unity: triangle/material counts, 28 bone positions/parents and sampled source corners pass; maximum position/bone error `0.000000466/0.000000365` units.
- Saved-asset verifier passes requested values, source sockets, all hardpoint bindings, all eight ownership values, collider, fog, wreck references, placement, icons and registration. No missing/broken references or donor visual dependencies.
- All eight living/wreck palettes rendered. Model/icon, green team color, wreck cuts and placement visually inspected. No Carrack import/serialization/compile errors.
- No automated Unity test suite or Play Mode combat run. Runtime firing, ability and death behavior remain untested.
