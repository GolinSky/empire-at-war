# Acclamator Assault Ship — Imperial Loadout

- Integrated on 2026-10-07 using `Architecture/ALO_MODEL_IMPORT_GUIDE.md`.
- `Temp/ALO_MODEL_MAP.txt` was absent. `output/aotr-empire-units/ship-catalog.csv` identifies `E_Acclamator_Assault_Ship_Fighters` → `ev_acclamator.ALO`.
- Source directory: `D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data/`.
- Main source: `ART/MODELS/ev_acclamator.ALO`; six separate models: `T_DTL_Acc_01.ALO` through `T_DTL_Acc_06.ALO`. No duplicate turret geometry added.

## Gameplay

- Empire `ShipType.AcclamatorAssault = 209`; hull/shields/speed `5,000 / 2,500 / 40` supplied by the user.
- Exactly four health targets: missile launchers `PTL_00/01` (750 HP each), shield generator `SG` (500 HP), engine `ENG` (750 HP). Target IDs `0..3` match health order.
- Four non-targetable shared `HeavyLaser` mounts: `MuzzleA_00..03`, IDs `4..7`.
- Six non-targetable `MissileInterceptorHardPoint` mounts at `QTL_01..06_Turret/FB_02`, IDs `8..13`. Reuse the existing explicit beam bindings, range `40`, reload `0.6 s`, interception chance `50%`; no separate strikecraft weapon was added.
- `WeaponType.AssaultMissile = 63`: four shots, `1 s` spacing, `15 s` reload, damage `30`, range `250`; shared concussion missile projectile/audio/category. Projectile speed `35` and range conversion are project choices. The source projectile's defense debuff is outside this unit's requested loadout.
- `ShipAbilityId.AcclamatorBoostWeaponPower = 31`: source duration/recovery `20 / 60 s`, fire delay ×`0.5`, speed ×`0.25`, shield regeneration ×`0`, incoming damage ×`1.5`. Independent settings; shared abilities remain unchanged.
- TIE-Fighter: `3` total launches / `1` active; TIE-Bomber: `2` total / `1` active. Totals include the source's starting squadron and replacement reserves. `isDestroyable=false`; no hangar hardpoint or health entry.
- Launch delays `4 / 8 s`, turn/acceleration `30 / 30`, bank `20°`, height tier `4`, queue limit `10`, matchup hints and intact-geometry wreck remain project choices/donor tuning.
- Source-backed roster cost/build/tech/population: `4,300 / 86 s / 3 / 4`. Shield regeneration `4.16/s` uses the source value; regeneration timing `1 s` is a project choice.

## Art and registrations

- Living geometry `40.38952 × 17.104553 × 70` project units; bow `+Z`, up `+Y`. Banked Y range `−9.749215..8.552272`; navigation radius `43`. All mounts follow the banking body; launch exit sits `8` units below its banked lower bound.
- Living, placement and wreck prefabs have dedicated references. The placement root is identity; it uses kinematic/no-gravity trigger physics and the shared hologram material.
- Registered data/view mappings, existing Data/View Addressables groups, Empire roster, HUD/tooltip/roster sprite, reinforcement `UnitSpawnView`, matchups and `ShipIconGenerator.MAPPINGS`.
- Source hull/turret textures contain no nonzero alpha team mask or colored livery. User requested two team-color hull stripes on 2026-10-07; `Livery.cs` bakes a separate `2048 × 2048` linear mask from the dorsal hull's existing UVs (V tile `−1..0`). Original albedo/normal pixels stay unchanged.
- Stripes follow `abs(x) = 0.22 × (35 − z)`, half-width `0.85`, between Z `−16..28`; feathered edges preserve panel detail. Mask coverage `47,100` pixels (`1.123%` of the atlas), assigned to both living/wreck hull materials. Other surfaces keep their source masks.
- Brightness correction: all `14` living/wreck opaque materials use `_BaseColor = (0.5, 0.5, 0.5, 1)` and `_TeamRimStrength = 0`. The albedo was already assigned; the previous default `0.6` team rim added excessive emission. `TeamColorView` retains all `29` visible/shield renderer bindings; all eight live/wreck palettes and owned top views were rendered.
- Icon and silhouette: actual model, transparent `512 × 512`. Hidden collision/shadow helpers stay disabled; wreck/placement contain only `13` opaque hull/turret meshes.

## Verification

- Source files unchanged: SHA-256 comparison passed for all `7` ALO and `7` DDS files.
- Hull: `12` meshes, `17,643` triangles, `125` bones. Each turret: `3` meshes, `1,616` triangles, `7` bones. Source roots already survived the importer cleanup interruption; no root repair applied.
- Blender round trip: maximum corner displacement `0.000035479` source units, UV difference `0`, bone displacement `0.000026703`. Counts, parents and hidden states match.
- Unity: matching mesh/triangle/bone totals, UVs on all meshes, maximum attachment displacement `0.000000642` project units.
- Saved asset readback passed: exact targetability, mount counts/IDs, bays, ability, weapon profile, bindings, all registrations, placement dimensions/identity, wreck references. Zero missing scripts, broken serialized references or donor model/prefab dependencies.
- Builder, renderer, registration and verification scripts compile and execute through official `unity command run_script`. No asset import/serialization errors observed. Concurrent CLI diagnostic errors were unrelated to these assets.
- No automated Unity tests or Play Mode combat check run. Fire arcs, launch flow, missile interception, ability execution, placement and death behavior need runtime acceptance; provisional balance needs playtesting.

## Reproduction

1. Run `python Tools/Blender/AcclamatorAssault/Prepare.py` to audit source files and create the safe-mode MCP request.
2. Use the existing Blender 3.6.23 MCP connection: `uvx --from mcp-for-blender==2.1.3 python Tools/Blender/mcp_call.py Temp/AcclamatorAssaultImport/ConvertRequest.json`. The script creates new scenes and preserves other open work. Save its `CONVERSION_JSON` output as `Temp/AcclamatorAssaultImport/ConversionReport.json`.
3. Run `uv run --with pillow python Tools/Blender/AcclamatorAssault/Stage.py`, then `unity command eval --code 'UnityEditor.AssetDatabase.Refresh();' --timeout 15000 --json`.
4. Run `unity command run_script --file Tools/Blender/AcclamatorAssault/BuildArt.cs --entry BuildAcclamatorAssaultArt.Main --timeout_ms 60000 --json`.
5. Run the same command form for `BuildShip.cs` / `BuildAcclamatorAssaultShip.Main`, `Livery.cs` / `ApplyAcclamatorAssaultLivery.Main`, `Render.cs` / `RenderAcclamatorAssault.Main`, `Register.cs` / `RegisterAcclamatorAssault.Main`, then `Verify.cs` / `VerifyAcclamatorAssault.Main`.
6. `VerifyGeometry.py` compares the retained source/validation Blender scenes through MCP. Reports, editable blends, FBX exports and previews live in ignored `Temp/AcclamatorAssaultImport/`.

- The upstream importer scans every Blender scene during helper cleanup. Its specific `removeShadowDoubles` failure is completed only on the newly imported scene; the installed add-on and other scenes remain unchanged.
- The gameplay builder removes donor hardpoints outside the visual pivot and unpacks the old model pivot. Both are necessary to remove old hangar/weapons and the Republic model dependency.
- Livery edits only material/texture assets; rendering instantiates geometry in an isolated preview scene. Both can run while the user's match is in Play Mode; they preserve the active scene and restore temporary palette/ambient state.
