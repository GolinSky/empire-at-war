# Dreadnaught-class Heavy Cruiser replacement

- Source: Republic at War `1.5.5`, Workshop `1129810972`, installed at `D:/SteamLibrary/steamapps/workshop/content/32470/1129810972`.
- `Units_space_republic_dreadnaught.xml` selects `ReV_dreadnought_heavy_cruiser.alo`; `Hardpoints_Republic_Space.xml` attaches twelve `ReV_dreadnought_turret_00..11.alo` models to `b_attach_00..11`.
- Credits: mesh **Farseer**, textures **Farseer/z3r0x**, rigging/coding **z3r0x** (`credits.md`, Dreadnaught Cruiser entry).
- Vault: `AssetPipeline/Imports/Dreadnaught Import.md`; import rules: `AssetPipeline/Guides/ALO_MODEL_IMPORT_GUIDE.md`.
- Existing gameplay, reinforcement, icon, shield and wreck assets are updated in place. Old source art remains untouched; no roster, balance or Addressables structure changes.

## Conversion

1. Run `uv run --with pillow python Tools/Blender/HeavyDreadnought/Prepare.py`. Original ALO/DDS/XML files and SHA-256 hashes are preserved in `Temp/HeavyDreadnoughtImport/Source` and `SourceAudit.json`.
2. Through Blender MCP, create a fresh scene named `Dreadnaught Source` and import the hull with `bpy.ops.import_mesh.alo(filepath=..., importAnimations=False)`. Preserve other shared Blender scenes.
3. ALAMO's global helper cleanup stops at another scene's `Collision` object after importing this hull. Expected state: one 69-bone rig plus 15 meshes. Do not import it again into that scene.
4. Inject the parsed `SourceAudit.json` as `AUDIT`, then execute `Convert.py` through Blender MCP. It repairs audited parents/materials, imports and attaches twelve turrets, then joins their skeletons without moving meshes. Known cross-scene cleanup failures are accepted only with complete audited geometry/bones.
5. Capture `DREADNAUGHT_REPORT` as `ConversionReport.json` and `DREADNAUGHT_CORNERS` as `SourceGeometry.json` in `Temp/HeavyDreadnoughtImport`. Packed editable blend and FBX are in its `Output` folder. FBX contains only this ship; the editable blend preserves the shared session's other scenes.
6. Run `uv run python Tools/Blender/HeavyDreadnought/Stage.py` after Unity finishes any unrelated build.

## Unity integration

Run each file with `unity command run_script --file Tools/Blender/HeavyDreadnought/<file> --entry <entry>.Main --timeout_ms 60000 --json`:

| File | Entry | Result |
| --- | --- | --- |
| `BuildArt.cs` | `BuildHeavyDreadnoughtArt` | FBX import/remaps, source-red livery, geometry-only visual |
| `VerifyGeometry.cs` | `VerifyHeavyDreadnoughtUnityGeometry` | Source/FBX/Unity geometry, UVs, bones and slots |
| `Replace.cs` | `ReplaceHeavyDreadnought` | Gameplay references, hardpoints, shield, bounds, exhausts, wreck and placement |
| `Verify.cs` | `VerifyHeavyDreadnought` | Saved asset/reference verification |
| `Render.cs` | `RenderHeavyDreadnought` | Icon, placement, stern and eight live/wreck palettes |

- These are model-specific replacement scripts, not a general ship importer. `Replace.cs` assumes the existing ship's four engine effects and original model body; run once on that original prefab.
- Source hull alpha is entirely zero. Recolor existing red paint through hue `0`, range `0.07`, saturation minimum `0.4`, strength `1`; mask strength `0`. Wreck rim strength `0` preserves neutral panels.
- Actual old visible length `73.00743` units is retained. New size `13.58614 × 12.15677 × 73.00743`; centered, bow `+Z`, up `+Y`, root scale `1`.
- `Body` banks the visual, shield and all eight hardpoints. Six logical weapons retain existing types/counts/arcs; IDs `0..5` use muzzle mounts `06,07,04,05,00,01`. Six source engine-effect positions use the existing exhaust effect.
- Collider, shield shell, ion bounds, selection ring and hologram trigger fit the replacement. Banked height `-6.191002 .. 7.0018487`; navigation radius `40`.
- Hull/shields `3600/2500`, speed `18`, turn/acceleration `7.5/7.5`, bank `15°`, two each of weapon types `2/3/11`, ability `6` remain unchanged.
- Current `ShipWreckBuilder` copies the 25 visible opaque meshes and team livery. `UnitWreckService` pools the existing Addressable wreck; GPU cuts/drift/heat/dissolve remain active. Wreck GUID `757a79319c0388c469ac8ac2e2c80f92`, 2–3 parts, lifetime `30 s`, dissolve/glow `8 s` are unchanged. Source death-clone hull geometry is byte-identical; EaW's ALA death animation is not used.

## Verification — 2026-10-24

- Original source hashes unchanged. Binary material slots/shaders/triangle counts audited before conversion.
- Transport: **63 meshes, 40,221 triangles, 141 bones**. Runtime visual/placement/wreck: **25 meshes, 8,110 triangles**. Helpers and damage decals retain source fidelity in FBX/blend only.
- Blender compares every triangle corner/UV after FBX round trip: maximum position `0.00004578`, UV `0.00000603`, bone `0.00003510` source units.
- Unity verifies all meshes/material slots/triangles, 3,807 sampled corners and all bones at `0.00001` tolerance; maximum bone error `0.000000871` Unity units.
- Saved references, six weapon profiles/eight hardpoints, shield, ownership/fog, bounds, procedural wreck and placement checks pass.
- All eight live and wreck palettes produce distinct images. Blue/green differ on 1,725 live pixels and 7,460 wreck pixels. Icon is transparent `512 × 512` with no crop.
- No new Console warnings/errors; `MainMenuScene` remains clean. Automated Unity tests and Play Mode combat were not run.
- `Evidence/` contains recorded verification; `Previews/` contains blue/green living hull, wreck, hologram and stern renders.
