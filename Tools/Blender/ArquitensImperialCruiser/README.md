# ArquitensImperialCruiser

- Source: AOTR 2.11.9 workshop `1397421866`, unit `E_Arquitens_Light_Cruiser`, `ev_arquitens.ALO`.
- Read vault `Architecture/ALO_MODEL_IMPORT_GUIDE` before conversion. Reference: `GameDesign/ArquitensImperialCruiser Import`.
- Use isolated Blender 3.6.23 + ALAMO MCP port `9882`; `Convert.py` and `VerifyGeometry.py` reject another port.
- `Prepare.py` audits installed XML, binary bones, DDS files and SHA-256 hashes into `Temp/ArquitensImperialCruiserImport/SourceAudit.json`.
- `Convert.py` restores the binary-verified identity Root, exports three unique FBXs and packed blends, and checks mesh/triangle/bone/UV-layer/bounds round trips. Capture `CONVERSION_JSON` as `ConversionReport.json`.
- `Stage.py` copies FBXs and converts DDS pixels to PNG; source Colorize alpha becomes the linear team mask. Run with `uv run --with pillow python Tools/Blender/ArquitensImperialCruiser/Stage.py`.
- Unity order: `BuildArt.cs` → `BuildShip.cs` → `Render.cs` → `Register.cs` → `Verify.cs`. Invoke with `unity command run_script --file <path> --entry <Class>.Main --timeout_ms 60000 --json`; inspect nested `result.success`.
- Construction/registration/rendering require Edit Mode and reject active or pending Play Mode. Read-only saved-asset verification does not start Play Mode.
- `BuildArt.cs`: scale `0.02`, no vertex welding/compression, source visibility, own materials, eight mounted turrets; centered length `40` project units, bow `+Z`, up `+Y`.
- `BuildShip.cs`: hull-only targeting, eight weapon bindings, own fitted shield, opaque-hull placement and shader-breaking wreck. `Render.cs`: actual-model icons, placement, eight live/wreck team palettes in isolated preview scenes.
- `VerifyGeometry.py` compares source/FBX vertex positions and UV corners. Capture `GEOMETRY_JSON` as `GeometryVerification.json`.
- `Package.py`: assert original hashes, inspect preview bounds/team response and retain blends/FBXs/textures/evidence in `output/aotr-empire-units/ArquitensImperialCruiser-Converted/`.
- IDs: ship `204`; light long-range turbolaser `39`; burst laser `40`; source-specific Boost Weapon Power `23`. Existing Republic Arquitens remains separate.
- Configured: hull/shields/speed `1,300/1,200/30`; four of each weapon, two shots per burst. Boost `20 s`, recovery `60 s`, fire delay ×`0.5`, speed ×`0.25`, shield regeneration ×`0`, incoming damage ×`1.5`.
- Source/project differences: raw source speed `3.0`; user speed `30`. Length/movement/economy/matchups are provisional; range maps at `1/8`, reload uses source midpoint. Turbolaser strikecraft exclusion, weapon-energy regeneration and source death ALA are unsupported by this static integration.
- Verification covers saved assets/imports/compilation/renders. No automated Unity tests or Play Mode run.
