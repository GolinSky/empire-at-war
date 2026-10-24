# Centax-class Heavy Frigate

## Source

- Republic at War workshop `1129810972`; installed under `D:/SteamLibrary/steamapps/workshop/content/32470/1129810972/`.
- Intact `ReV_centax_frigate.ALO`; damaged `ReV_centax_frigate_d.ALO`; XML `Units_Space_republic_centax.xml` and `Hardpoints_Republic_Space.xml`.
- Credits: mesh/textures **Evillejedi**, rigging **z3r0x** (`credits.md`, Centax entry). Original ALO, DDS, XML and ALA files remain unchanged; hashes are in `Evidence/SourceAudit.json`.
- Read through `empire-vault`: `AssetPipeline/Guides/ALO_MODEL_IMPORT_GUIDE`, `Architecture/PROJECT_ORGANIZATION`, `Rules/UI_UX_GUIDELINES`, `Rules/UI_CODE_BUILD_GUIDE`.
- `Shield_color.dds` is absent from the mod. It was extracted from the installed game's `GameData/Data/textures.meg` for the preserved source shield shell. Runtime uses the project's own baked shield.

## Conversion

1. Run `uv run --with pillow python Tools/Blender/Centax/Prepare.py` to audit both binaries, preserve source files and convert DDS textures to pixel-identical PNGs.
2. Use Blender MCP 3.6.23, ALAMO `import_mesh.alo`, animations disabled. Import into separate `Centax Source` and `CentaxWreck Source` scenes. Preserve other shared scenes.
3. In this session ALAMO completed geometry import, then failed in its global helper cleanup because another scene owns `Collision`. Both rigs retained their identity `Root`. `Convert.py` finishes only the Centax cleanup, verifies bone counts, restores binary parents by index and repairs material definitions from the binary audit. It expects that inspected import state; it is not a general ALO converter.
4. Inject `AUDIT` from `SourceAudit.json` with the chosen model's properties plus the shared `textures` dictionary. Execute `Convert.py` through Blender MCP. For the wreck, replace `Centax` with `CentaxWreck` in the script, then restore the output directory to `Temp/CentaxImport/Output`.
5. Capture `CENTAX_REPORT` into `ConversionReport.json` / `WreckConversionReport.json`. Editable packed blends and FBXs are in `Temp/CentaxImport/Output`. The blends retain the shared Blender session's other scenes; each FBX exports only its active Centax source scene.
6. Run `VerifyGeometry.py` through MCP for each variant (replace the scene-name prefix for the wreck). It compares every triangle corner and UV. Capture `before` geometry sampled every 32nd corner as `<Variant>SourceGeometry.json` for the Unity check.
7. Run `python Tools/Blender/Centax/Stage.py`.
8. Use `unity command run_script --file Tools/Blender/Centax/<file> --timeout_ms 60000 --json`: `BuildArt.cs` → `BuildShip.cs` → `Render.cs` → `Register.cs` → `VerifyUnityGeometry.cs` → `Verify.cs`.

## Configuration

- `ShipType.Centax = 16`; Republic; hull/shields `2000/2000`; shield refresh `62` each `1 s`; population `2`; build `25 s`; build level `2`; price `2600`.
- User shield value overrides this mod's XML value `1300`. Requested GC cost `2400`, tech level `2` and starbase level `2` are retained in `Evidence/RequestedStats.json`. The current game has one price and one station-level build gate; separate GC pricing and an independent tech gate have no runtime fields.
- Eleven weapons plus engine and generator: IDs `0..7` lasers at `HP01..08`, `8..9` proton launchers at `HP09/10`, `10` dual turbolaser at the midpoint of `HP11_00/01`, `11` engine at `HP_E`, `12` generator at `HP_S`.
- Four laser mounts per side: port yaw `-177.5..-2.5°`, starboard `2.5..177.5°`. Forward torpedo arcs `±90°`; forward dual battery `±50°`. Profiles: `Laser=8`, `ProtonTorpedo=6`, `DualMediumTurboLaser=5`. Health per weapon/engine/generator `350/325/475`; all 13 are targetable and bank with the body.
- Existing `PowerToWeapons=20`: active `10 s`, recovery `60 s`, firing delay ×`0.5`, movement ×`0.75`, incoming damage ×`1.5`. No hangar.
- Provisional project tuning: length `28` units, speed `30`, turn/acceleration `45/45`, bank `15°`, radar `500`, height tier `7`, build cap `10`, Frigate armor class. Shared weapon profiles remain unchanged.
- Living model: two opaque meshes, `4582` visible triangles; five helper meshes retained only in FBX/blend. Root scale `1`, centered bounds approximately `4.75 × 4.63 × 28` units; bow `+Z`, up `+Y`; navigation radius `17`.
- Team mask uses **direct source hull alpha**, not inverse alpha: four authored stripe areas, `0.87824%` nonzero coverage, linear import, strength `1`. `UnitRenderStateComponent` owns hull/shield renderers.
- Damaged ALO supplies all three wreck meshes (`13100` triangles), aligned with the intact source transform. Current `UnitWreckService` pools Addressable `UnitWreckView` instances, applies owner color and procedural cuts, separation and dissolve. Dedicated wreck materials retain the same mask. Wreck lifetime/dissolve `30/8 s`; data has no hard reference to the wreck prefab.
- Dedicated gameplay, visual, damaged visual, wreck, reinforcement, icon and matchup assets. Registered in Republic roster, ship data lookup, asset mapping, existing View/Data Addressables groups, HUD/tooltip icons, placement lookup and icon generator.

## Verification — 2026-10-24

- Intact source/FBX: `7` meshes, `21361` triangles, `55` bones. Maximum corner/UV/bone error `0.000083924 / 0.000000984 / 0.000057221` source units.
- Damaged source/FBX: `3` meshes, `13100` triangles, `19` bones. Maximum corner/UV/bone error `0.000072480 / 0.000000984 / 0.000220066` source units.
- Unity: all triangle/material-slot counts, bone names/parents and sampled source corners pass. Maximum corner/bone error: intact `0.000002871 / 0.000001312`; wreck `0.000002043 / 0.000003439` project units before gameplay scaling.
- Saved-reference checks pass: requested supported stats, all 13 sockets/health entries, weapon arcs, explicit injection/ownership/fog bindings, shield, collider, own Addressable wreck and placement, icon consumers and roster mappings. No missing scripts, broken references or donor visual dependencies.
- Rendered all eight living/wreck palettes, transparent `512 × 512` icon, silhouette, top/stern and hologram previews. Inspected contrasting colors, model, placement and wreck cuts.
- No automated Unity test suite or Play Mode combat run. Firing, ability activation and death behavior are configured against existing systems; runtime acceptance and provisional balance remain unverified.
- Source death ALA, EaW particles and animated shader effects are preserved as source references; they are not converted to Unity animation.
