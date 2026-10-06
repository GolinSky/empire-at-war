# Imperial III Star Destroyer

- Empire `ShipType.ImperialIIIStarDestroyer = 206`; hull 28,000, shields 18,000, speed 250.
- AOTR source `E_Imperial_Star_Destroyer_3`, model `EV_ISD3.ALO`, Workshop `1397421866`.
- Read vault `ALO_MODEL_IMPORT_GUIDE`, `PROJECT_ORGANIZATION`, `UI_UX_GUIDELINES`, and `UI_CODE_BUILD_GUIDE` before rebuilding.
- Editable art, previews, source hashes, and verification: `output/aotr-empire-units/ImperialIIIStarDestroyer-Converted/`.

## Rebuild

1. Run `python Tools/Blender/ImperialIIIStarDestroyer/Prepare.py` to audit source XML, bones, material references, and hashes.
2. Use a dedicated Blender 3.6.23 process with ALAMO and Blender MCP on port 9883. Keep safe mode enabled. Set scene `blendermcp_auto_start_server = False`, scene `blendermcp_port = 9883`, then start the server. Preserve the user's primary Blender process and preferences.
3. Execute `Convert.py` through Blender MCP `execute_blender_code`. Prepare `Temp/ImperialIIIStarDestroyerImport/Output/Textures` first. Extract the `CONVERSION_JSON` line into `ConversionReport.json`.
4. Run `uv run --with pillow python Tools/Blender/ImperialIIIStarDestroyer/Stage.py` to stage FBX and lossless PNG textures and update material mappings.
5. Execute `BuildArt.cs`, `BuildShip.cs`, `Render.cs`, then `Register.cs` through `unity command run_script --file <path> --entry <type>.Main --timeout_ms 60000 --json`. Entry types: `BuildImperialIIIStarDestroyerArt`, `BuildImperialIIIStarDestroyerShip`, `RenderImperialIIIStarDestroyer`, `RegisterImperialIIIStarDestroyer`. Inspect nested `data.result.success`.
6. Execute `VerifyGeometry.py` through isolated Blender MCP. Save each `SOURCE_GEOMETRY` record's `geometry` as `<name>SourceGeometry.json`, and `GEOMETRY_JSON` as `GeometryReport.json` in the temporary import directory.
7. Execute `InspectGeometry.cs` with entry `InspectImperialIIIStarDestroyerGeometry.Main`, then `Verify.cs` with entry `VerifyImperialIIIStarDestroyer.Main`. These inspect assets in Edit Mode without running Unity tests or entering Play Mode.
8. Run `uv run --with pillow python Tools/Blender/ImperialIIIStarDestroyer/Package.py`. Recheck Editor import/serialization diagnostics.

## Art

- Five unique model families: hull, heavy dual turret, medium triple turret, ion turret, composite turret. Eighteen mounted turrets follow XML attachment and muzzle bones.
- Binary-backed identity `Root` is restored; mesh visibility, bone parenting, UVs, textures, and source material metadata are retained.
- Hull: 102 meshes, 52,380 triangles, 202 bones. Turret mesh/triangle/bone counts: heavy dual 6/1,402/8; medium triple 3/574/7; ion 7/896/11; composite 6/2,406/9.
- Blender's FBX reimport retriangulates hidden collision polygons to 17,709 triangles from 17,725. Source corner positions and UVs match; Unity retains all 17,725 triangles. Unity geometry validation checks UV correspondence on visible surfaces and positions on hidden collision/shadow surfaces.
- Raw Unity import scale 0.02; gameplay root scale 1; bow +Z, up +Y. Mounted bounds 115.89669 × 64.00111 × 192.90494; navigation radius 118; bank ±8°; banked vertical range -35.03626..32.75116.
- Source `Hangar.tga` is resolved explicitly because the ALO importer labels it `Hangar.dds`.
- Source alpha remains in albedo PNGs; inverse alpha supplies team masks. Normals are linear with green-channel inversion. Opaque source meshes use `EmpireAtWar/Ship Lit`; authored glows use additive materials. Hidden collision/shadow meshes remain disabled.
- Own placement hologram, wreck materials, transparent 512×512 icon/silhouette, and eight live/wreck team previews are saved.

## Gameplay

- Targetable: 10 heavy dual turbolasers, 2 heavy long-range 2-burst turbo-ions, 2 shield generators, 3 engines, 1 hangar, 1 tractor beam. IDs 0..18.
- Non-targetable: 5 medium 3-burst turbolasers, 4 medium turbolasers, 4 medium turbo-ions, 6 heavy lasers, 1 ability-only composite beam. IDs 19..38. Automatic weapon collection contains 31 guns.
- `Fire Composite Beam` ability 25: 6 seconds, 45-second recovery, range 375, 1,000 raw damage; hull armor coefficient 1, shield coefficient 0.1. Frigates, capitals, and heavy capitals qualify; corvettes, strikecraft, and structures do not.
- Beam uses an explicitly serialized muzzle through `ICompositeBeamFacade`; it follows the banking body and stops on target destruction or caster ion disable/death. Shared Tractor Beam ability 22 supplies movement restriction.
- User-selected complement: TIE Avengers (3 total launches, 1 active) and TIE Punishers (2 total launches, 1 active). Initial delay 4 seconds; launch interval 30 seconds. Punishers substitute for unavailable source Scimitars.
- Source campaign economy: 26,000 credits, 520-second build, tech 4, capacity 9. Limit 10, size, shared weapon/system tuning, and copied matchups are provisional.
- Registration covers Empire faction, ship/data mapping, existing Addressables groups, battle/tooltip/faction icons, placement, weapon/audio profiles, ability catalog, and damage matrix.
- Automated Unity tests and Play Mode acceptance were not run. Combat timing, AI use, moving-target beam rendering, and launch clearance still require runtime acceptance.
