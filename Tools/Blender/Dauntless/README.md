# Dauntless Heavy Cruiser

- Guide: vault `Architecture/ALO_MODEL_IMPORT_GUIDE`; import record: `GameDesign/Dauntless Import`.
- Source: Workshop `1770851727`, `Neutral_Dauntless_Cruiser.ALO`. Correct texture folder ends in `Data/Art/Textures`, not `Texturesc`.
- Project balance from `Rebel units`: hull 10,000; shields 5,500; speed 17; price 5,600; build 60 s. Remake XML balance is retained as source evidence.
- Loadout: 8 heavy turbolasers, 4 medium dual turbolasers, 4 heavy ion cannons, 2 point-defense lasers; 2 engines, shield, hangar, sensor. All 23 targets have explicit health and fog bindings.
- User-approved X-Wing/Y-Wing: one active per type, two total launches per type (initial + replacement). Launch delay/interval 4/8 s are provisional.
- `ShipType.Dauntless=310`, `DauntlessSensorJamming=39`, `SensorArray=10`. Jamming reduces hostile accuracy by 20% for 8 s, recovers in 40 s; provisional radius 500 units. Effects follow range and stop on sensor/caster destruction. Overlapping jammers do not compound the penalty.
- Converted 13 meshes, 102 bones, 29,040 triangles including helpers. Source-equal visible geometry/UVs/material slots; helper geometry reflects ALAMO shadow welding. Editable blend/FBX preserves helpers; four Unity prefabs remove them.
- Unity visible bounds 32.69578 × 42.56418 × 160 units; roots have unit scale; bank envelope −21.282095..21.282091 at ±5°. Derived sensor position uses the dorsal hull; weapons use HP_01_FP_01..HP_18_FP_01.
- Missing source damaged-model texture `destruction_for_dcs.dds`: wreck uses the verified live hull and project wreck materials. Animated ALA and EaW wave/distortion shaders are not ported.
- Verification: 3,694 saved-asset checks; Unity bone error ≤0.000001508 units; 18 source hashes unchanged; 13 texture references pixel-identical; transparent 512×512 icon and all eight live/wreck palettes checked. No automated Unity tests or Play Mode run. In-battle combat/ability/hangar acceptance remains unrun.
- Evidence: `Evidence/`; editable package and previews: `output/remake-rebel-units/Dauntless-Converted/`.

## Rebuild

1. `uv run --with pillow python Tools/Blender/Dauntless/Prepare.py`
2. `uv run python Tools/Blender/Dauntless/Convert.py` through the existing Blender 3.6 MCP connection; use a fresh import and preserve other scenes.
3. `uv run python Tools/Blender/Dauntless/Stage.py`, then `GenerateBuilders.py`.
4. Unity: `BuildArt.cs`, `BuildGameplay.cs`, `Persist.cs`, `Render.cs`, `Register.cs`, `StripHelpers.cs`, `Inspect.cs`.
5. `uv run --with pillow python Tools/Blender/Dauntless/Package.py`.

Unity command: `unity command run_script --file Tools/Blender/Dauntless/<File>.cs --timeout_ms 60000 --timeout 70 --json`. Check nested `result.success` before the next step. Builders save assets and preserve unrelated Editor scenes. Never run the donor migration scripts.
