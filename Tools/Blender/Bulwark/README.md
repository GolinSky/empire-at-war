# Bulwark Mk1

- CIS `ShipType.Bulwark = 112`; unique from concurrent Wavecrest `111`.
- Source unit `CIS_Bulwark`, Republic at War workshop `1129810972`.
- XML: `D:/SteamLibrary/steamapps/workshop/content/32470/1129810972/Data/XML/Units_space_cis_bulwark.xml`.
- Intact model `Art/Models/SeV_bulwark.ALO`; original ALO/DDS/XML files remain unchanged.
- Workflow: vault `AssetPipeline/Guides/ALO_MODEL_IMPORT_GUIDE`.

## Gameplay

- Capital; hull `8500`, shields `2500`; cost `6500`, build `85 s`, population `3`, available level `4`. Maximum count `10` is provisional.
- `16` targetable hardpoints: `3` quad turbolasers, `8` turbolasers, `2` proton launchers, shield, engine, hangar.
- Every weapon uses existing Medium definitions (`90`, `4`, `48`). Shared family profiles define damage, range and salvo timing; XML weapon timings remain provenance.
- Capital HP pattern: weapons/hangar `400`, engines/shield `600`; hull damage multiplier `1`; no per-mount HP overrides.
- User-approved substitution: `2` active Vulture + `2` replacements; `1` active Advanced Droid Bomber + `1` replacement. Hangar total pools are `4` and `2`, including starting launches.
- One destroyable hangar; launch follows `SPAWN_00` X/Z and is moved below the hull. Shared timing `4 s` initial / `8 s` interval.
- Project capital presets: speed `18 units/s`, turn/acceleration `7.5`, height tier `3`, shield regeneration `180/s`. These replace incompatible XML movement units and XML refresh `50`.
- Provisional length `114.238441` project units, matching measured saved Providence. Root scale is identity; bow `+Z`.
- Armor strings, required planets and separate starbase prerequisite have no equivalent in the current ship data. XML tech-threshold switching is represented by the approved fixed complement.
- No source ability is inferred; ability list is empty. Matchup display uses the existing Providence capital matchup asset as its template.

## Mounts

| Role | Source attachment |
| --- | --- |
| Left/right/center front quad | `HP_Bulwark_F1`, `HP_Bulwark_F0`, `HP_F_MAIN` |
| Left/right proton | `HP_Bulwark_L1`, `HP_Bulwark_R1` |
| Eight turbolasers | `HP_Bulwark_L2..L5`, `HP_Bulwark_R2..R5` |
| Shield / engine / hangar | `HP_Bulwark_Shield_Generator`, `HP_Bulwark_Engines`, `SPAWN_00` |

- Firing arcs `200°`: front quads centered forward; port/starboard weapons centered `-90°` / `90°`.

## Art

- Preserve visible `objHULL_LOD02`, `objGOBLINS02`, `lightmesh`, `thrusters`; never remove the main hull by its LOD name.
- Source hull alpha is the paint mask. Extract to `Bulwark_SeV_bulwark_TeamMask.png`; use explicit team mask, keeping yellow safety markings unchanged.
- Opaque hull uses `EmpireAtWar/Ship Lit`; source additive light/engine surfaces use separate additive materials.
- Static Unity meshes are baked from verified transport geometry; hidden collision/shadow helpers have no runtime renderers.
- Project dynamic wreck is built from intact visible hull geometry. Source damaged model and death animations are not used.

## Rebuild

1. `uv run --with pillow python Tools/Blender/Bulwark/Prepare.py`.
2. Inspect Blender sessions/version/add-ons using the import guide. Launch a fresh portable Blender 3.6.23 process with `--python F:/Private/empire-at-war/Tools/Blender/Bulwark/Start.py` and `Start-Process -WindowStyle Hidden`. This startup script clears its own fresh scene and opens isolated MCP port `9898`; never execute it in a user's existing session.
3. `python Tools/Blender/Bulwark/RunConversion.py` uses `Call.py` and the configured MCP SDK with port `9898`. The task's helper was closed after verification; shared port `9876` was preserved.
4. `python Tools/Blender/Bulwark/VerifyGeometry.py`, then `python Tools/Blender/Bulwark/Stage.py`.
5. Run builders serially: `unity command run_script --file Tools/Blender/Bulwark/<File>.cs --entry <Class>.Main --timeout_ms 60000 --json`.
6. Order: `BuildArt.cs` / `BuildBulwarkArt`, `BuildShip.cs` / `BuildBulwarkShip`, `Render.cs` / `RenderBulwark`, `Register.cs` / `RegisterBulwark`, `Verify.cs` / `VerifyBulwark`.

## Verification

- `2026-10-27`: production compilation and saved-asset verification passed. Geometry, references, 16 hardpoints, HP, 13 medium mounts, hangar pools, shield containment, icons, CIS roster, audio, Addressables, placement and wreck checked.
- Converted intact transport: `9` meshes, `82` bones, `13739` triangles. Source visible triangle counts and material assignments verified; hidden helper welding and six degenerate hidden faces are recorded in conversion evidence.
- Maximum source→FBX corner error `0.000106812` source units; UV error `0.00000584126`; raw source bone error `0.00069822`; parents match.
- Inspected ship icon, team colors, placement hologram and dynamic wreck renders. Temporary evidence: `Temp/BulwarkImport/`; compact retained evidence: `ImportEvidence.json`.
- Unity source-LOD naming warning (`_LOD2` without `_LOD1`) is benign; saved visual removes automatic LOD groups and preserves the visible hull. No new import/serialization errors.
- Automated Unity tests and Play Mode were not run. Runtime behavior and balance remain unverified.
