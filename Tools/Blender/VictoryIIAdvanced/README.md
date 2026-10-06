# Victory II Star Destroyer — Advanced Loadout

Empire `ShipType.VictoryIIAdvanced = 203`; hull/shields/speed 12,000/10,000/20.

- Source unit: AOTR `E_Victory_Star_Destroyer_2_Fighters`; `EV_VSD_II.ALO`.
- Sources remain in Workshop `1397421866`; audit with `python Tools/Blender/prepare_victory_ii_advanced.py`.
- Read vault `Architecture/ALO_MODEL_IMPORT_GUIDE`, `Architecture/PROJECT_ORGANIZATION`, and `Rules/UI_UX_GUIDELINES` before rebuilding assets.
- Run Blender conversion only in a dedicated Blender 3.6.23 MCP process on port 9880, with ALAMO enabled and safe mode on. `Convert.py` and `VerifyGeometry.py` fail if connected to another port; they clear only the isolated process.
- Launch the isolated process with a Python startup script that sets `bpy.context.scene.blendermcp_auto_start_server = False`, `blendermcp_port = 9880`, then calls `bpy.ops.blendermcp.start_server()`. Do not change the primary MCP connection or Blender preferences.
- Execute `Convert.py` through the existing MCP SDK client configured temporarily for port 9880. Redirect output to `Temp/VictoryIIAdvancedImport/ConversionLog.txt`; create `Output/Textures` first.
- Run `uv run --with pillow python Tools/Blender/VictoryIIAdvanced/StageArt.py`. It stages only this ship and the two distinct turret variants, verifies decoded PNG pixels, and writes material mappings.
- Through the official `unity command run_script --file <path> --json`, execute `BuildVictoryIIAdvancedArt.cs`, `BuildVictoryIIAdvancedShip.cs`, `RenderVictoryIIAdvanced.cs`, and `RegisterVictoryIIAdvanced.cs` in that order. No scene opening or Play Mode is required.
- Execute `VerifyGeometry.py` through isolated Blender MCP; retain `SOURCE_GEOMETRY` JSON lines as `<name>SourceGeometry.json` and `GEOMETRY_JSON` as `GeometryReport.json`. Then execute `InspectVictoryIIAdvancedGeometry.cs` and `VerifyVictoryIIAdvanced.cs` through Unity.
- Saved assets use existing type-first Empire folders; Addressables entries stay within existing Data/View groups. Builders preserve existing GUIDs and upsert this ship's registrations.

Ten targets: 4 medium dual turbo-ions, 2 medium turbo-ions, shield generator, 2 engines, tractor beam. Other systems: 6 medium 3-burst turbolasers, 6 heavy lasers, non-targetable hangar. Shared TIE-Interceptor: 2 total launches, 1 active; 4/30 s launch timing. Shared abilities: Victory Boost Weapon Power 12 and Tractor Beam 22.

Six turrets follow T_01..06. Variant 01 is converted separately; 02..06 share a source hash. Cancel the nested turret **rig** rotation and scale when parenting; the preserved FBX wrapper is identity and must not be used for this compensation.

Source/FBX/Unity counts: hull 40 meshes / 24,024 triangles / 180 bones; each turret 4 / 1,447 / 6. UVs and bone parents match. Identity Root restoration is binary-backed. Non-lens Unity geometry error <=0.000002101 raw import units; near-coincident engine-effect chains yield up to 0.008128 raw lens-plane error (~0.088 gameplay units). Gameplay hardpoint positions are checked independently. Source death clone and its ALA are not converted.

Visible bounds: 65.43347 x 42.26736 x 110; bank +/-8 degrees; vertical range -24.06689..21.44096; navigation radius 69. Gameplay root scale 1, bow +Z, up +Y. Placement disables additive glow planes; wreck contains only opaque hull/turrets. Icons are transparent 512x512; all eight team palettes and placement/wreck were rendered.

Integration evidence and packed sources: `output/aotr-empire-units/VictoryIIAdvanced-Converted/`. Vault reference: `GameDesign/Victory II Advanced Import`; active runtime acceptance: `TODOs/Features/VictoryIIAdvanced_Import`. No automated Unity tests or Play Mode run. Economy derives from source advanced XML; size, movement, weapon/system balance, limit and copied matchups are provisional.
