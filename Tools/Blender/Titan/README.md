# Titan-class Battlecruiser

## Decision

- Empire `ShipType.Titan = 214`; Remake 4.0 Workshop `1770851727`, `Titan_Star_Destroyer_MP`.
- Source hull `Empire_Titan_Star_Destroyer.ALO`; all 106 XML-attached turret models remain visible. Weapon counts are gameplay choices from the vault `Empire Fleet Import` note.
- Hull/shields/speed `28,000 / 18,000 / 16`; price/build time `13,000 / 100 s`; level `5`, population `12`, maximum count `1`, HeavyCapital class.
- Zero squadron bays and gameplay hangar hardpoints. Launch dependency remains explicitly bound; hangar destruction is disabled.
- Existing Power to Weapons `5` substitutes for the proposed cloak: current cloak does not implement the requested damage activation restriction. Existing Tractor Beam `22` retains its shared timing and 25% target speed; shared abilities are unchanged.

## Implementation

- Weapons: 12 heavy long-range quad turbolasers / `88` (Titan-owned copy of `20`), six medium dual turbolasers / `5`, four heavy ion cannons / `11`, two point defense lasers / `14`.
- Muzzles use source `QTL_01..12` attachment `FP_01`, small turret attachments `01..06` / `FP_01`, heavy ion attachments `01..04` / `FB_01`, defensive turret attachments `07..08` / `FP_01`.
- 33 unique target IDs: 24 weapons, four engines, three shield generators, one tractor, one sensor array. Matching health entries; all weapons are targetable.
- Engine targets derive from stern bounds; three shield targets derive from `Shield_Generator` with lateral offsets. Tractor uses `HP_Trac_Bone`; sensor uses `Superstructure_Top`. `Evidence/HardpointMapping.json` records all positions.
- Provisional health: weapons `1,000`, engines `2,000`, shield generators `1,500`, tractor `1,500`, sensors `1,000`. Regeneration `25/s` after `3 s`; weapon yaw ±85°.
- Measured Venator opaque length `119.3834`; Titan length `472.49353` = Venator × `4,500/1,137`. Provisional physical length remains unconfirmed.
- Bounds `207.054 × 80.11231 × 472.49353`; navigation radius `265.9348`; bank ±5°, Y envelope `−40.056156 .. 40.95625`.
- Blender `3.6.23`, ALAMO, safe-mode MCP on isolated port `9886`; source hashes, UVs, source hidden flags and material-slot triangle assignments preserved.
- Source XML lacks a comma between `HP_Titan_Hanger_03` and `HP_TSD_OTL_01`; Titan audit tokenizes whitespace and commas without changing source XML.
- Source shadow/shield helpers remain in FBX/blend and are removed from gameplay geometry. Source Stealth surface is disabled in game art; animated EaW cloak distortion is unsupported.
- `EV_PHANTOM_SCAN_LINES.DDS` comes from base-game `corruption/Data/textures.meg`; `W_te_rock_f_02_b.DDS` is absent from installed Remake/base-game archives and recorded as an unresolved optional distortion texture.
- Source alpha masks cover nearly the whole hull. Five derived source-albedo/UV stripe overlays add 724 triangles for team colors; original surface UVs and textures remain unchanged.

## Verification

- 107 FBXs passed Blender geometry/UV/bone round-trip verification. Maximum hull geometry error `0.0001705585`; bone error `0.0001781100` source units.
- Saved Unity verifier: 52,482 checks passed; maximum Unity bone error `0.0000038147`; compilation failed flag `false`.
- 136 source hashes unchanged; 229 converted/staged texture copies pixel-identical. Icon `512×512`, alpha bounds `(53,159)..(416,356)`.
- Eight live and wreck team palettes rendered; blue/green, placement and wreck images visually inspected.
- Titan EditMode `12/12` passed after reproducing/fixing two integration failures. Two Titan wreck checks passed in the earlier broader run. Actual project/battle/local/AI scopes spawned Titan and verified health, voice, independent state and teardown.
- Fixed stale donor injectables (33 targets + team-color view), bound every MeshRenderer, copied the Mandator quad profile into Titan-owned `88`, and set radar to the longest gun `877.49994`.
- Earlier broader suite: `1,288/1,308`; 20 failures across concurrent ship imports, balance validation and Editor timeout logs. No full-suite green claim.
- Manual skirmish balance, targeting arcs, tractor range against corvettes/frigates and long-duration combat remain acceptance work. No distribution-rights claim is made for Remake assets.

## Files

- Art: `Assets/Art/{Models,Materials/Models,Textures/Models}/EmpireShips/Titan/`.
- Gameplay/visual: `Assets/Prefabs/Models/Ships/Empire/TitanShipView.prefab`, `Titan.prefab`.
- Dedicated reinforcement, wreck, icons, matchups, ship data and existing-group Addressable entries.
- Editable local conversion package: `output/remake-empire-units/Titan-Converted/`; source shader parameters stored in every blend.

## Rebuild

1. `uv run --with pillow python Tools/Blender/Titan/Prepare.py`; use a fresh isolated Blender on `9886` with `Tools/Blender/ISDIRemake/Start.py`.
2. `uv run python Tools/Blender/Titan/Convert.py`; `uv run --with pillow python Tools/Blender/Titan/Stage.py`; `uv run python Tools/Blender/Titan/PreserveParameters.py`.
3. `uv run python Tools/Blender/Titan/GenerateBuilders.py`; create the white `2×2` stripe mask at `Assets/Art/Textures/Models/EmpireShips/Titan/Titan_TeamStripes_TeamMask.png` if missing.
4. Run `BuildArt.cs`, `BuildStripes.cs`, `BuildGameplay.cs`, `Persist.cs`, `Finalize.cs`, `FixIntegration.cs`, `Render.cs`, `Register.cs`, `Verify.cs` using `unity command run_script --file Tools/Blender/Titan/<file> --timeout_ms 120000 --timeout 150 --json`; inspect nested `result.success`.
5. Preserve Blender shader metadata with `PreserveBlendMetadata.py` and its generated MCP request through `ISDIRemake/Call.py`. Run `uv run --with pillow python Tools/Blender/Titan/Preserve.py` before Unity clears temporary files.
6. Inspect open scenes; run asynchronous `TitanShipTests` and poll `test_status` to completion.

## Worn livery correction — 2026-10-14

- After rebuilding art/gameplay, run the shared [Empire fleet livery workflow](../EmpireFleetLivery/README.md). It supersedes the earlier team-mask/rim and stripe rendering steps.
- Neutral hull, small chipped hue-based stripes like Acclamator; live/wreck materials, ownership bindings, hologram and icons updated.
- Canonical current renders and verification: `Tools/Blender/EmpireFleetLivery/`. All four ship test suites passed (`39/39` total).
- After rebuilding art, run [Empire engine glow fix](../EmpireEngineGlow/README.md) to replace the imported detached glow rings with fitted nozzle glow.
