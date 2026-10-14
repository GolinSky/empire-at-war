# Tartan-class Patrol Cruiser

- Source: Empire at War Remake 4.0, Workshop `1770851727`; `Empire_Tartan.alo` plus `Empire_Tartan_Turret.ALO`. Source squadron `Tartan_Patrol_Cruiser` contains `Tartan_Gunship`; this project imports one corvette using the fleet-note balance.
- Empire ship `215`: hull/shields/speed `650/850/70`, price `1,450`, build `28 s`, level `1`. Project choices: population `1`, max count `20`, shield regeneration `8/s`, weapon hardpoint health `70`.
- Nine targetable weapons: four large repeating lasers (`90`, six-shot burst), one heavy laser (`28`), four point defense lasers (`14`). All bind to `HealthComponent.ShipUnits`; no engine, shield-generator or hangar hardpoint and no squadron bays.
- Existing Power to Weapons (`20`) replaces unavailable Laser Defense: double fire rate for `10 s`, speed `-25%`, damage taken `+50%`, recovery `60 s`. No new ability implementation.
- Scale: imported Imperial Venator visible length `120` units x `250/1137` = `26.3852`; unit root scale `1`. Navigation radius `16`, banking `+/-15 degrees`, bank-aware vertical envelope recorded in `GameplayBounds.json`.
- Source turret is attached to the hull's `Turret` bone. Repeating lasers use `HP_01/02/03/05`, original local +X projected into Unity XZ, and 150-degree cones. Heavy forward laser uses `MuzzleA_00`, 30 degrees. Point defense uses `HP_01/02/05/06`, forward/port/starboard/rear 180-degree coverage; these four mounts are project adaptations.
- Binary material audit: every source mesh has one slot. Opaque hull/normal maps use Ship Lit; additive lights retain effect intent. Source shadow/collision/shield helpers stay in FBX/blend and have no runtime rendering components. ALAMO welding removes degenerate shadow/collision faces; visible triangle counts match the source binary.
- Source atlases contain no usable alpha livery. Project-derived white mask recolors only the small superstructure; neutral hull keeps team rim shading. Wreck copies the saved mask. All eight live/wreck palettes and transparent icon/placement bounds verified.
- Conversion output: 13 meshes, 13,228 triangles, 32 bones; hull/turret source-to-FBX corner errors `0.000002862/0.000000596` source units and UV errors `0.000005722/0.000007868`. Unity geometry, material slots, bone positions and four prefab reference checks passed. Fifteen source hashes unchanged; PNG albedo/normal copies are lossless.
- Ten simulator seeds each against Venator, Victory II and MC80 completed; Tartan lost all 30. Simulator excludes movement, arcs, abilities, hangars and interception. No Play Mode combat acceptance is claimed. Source engine proxy particles/animations are not recreated.
- Final Tartan EditMode tests: **9/9 passed**, including collision, source muzzles/arcs, explicit hardpoint injection, stats, ability, registration and prefab references. Two Tartan wreck regression cases also passed in the observed broader run (`1,288/1,308`; 20 failures outside this unit). `TestResults.json` preserves that evidence.
- Editable output and previews: `output/remake-empire-units/Tartan-Converted/`. Original rights remain with Remake's creators; source credits are retained in `SOURCE_CREDITS.txt`.

## Rebuild

1. `python Tools/Blender/Tartan/Prepare.py` audits the installed source.
2. Launch portable Blender 3.6.23 with `--python Tools/Blender/Tartan/StartSession.py` in a hidden window. It uses isolated MCP port `9885`, leaving the main Blender session untouched.
3. `uvx --from mcp-for-blender==2.1.3 python Tools/Blender/Tartan/ConvertRequest.py` sends source data and code through MCP with safe mode enabled.
4. Run each saved blend through background Blender with `--python Tools/Blender/Tartan/VerifyGeometry.py`; verifies FBX corner/UV equivalence and trims unrelated datablocks.
5. `uv run --with pillow python Tools/Blender/Tartan/Stage.py`; then `unity command eval --code 'UnityEditor.AssetDatabase.Refresh();' --json`.
6. Run `BuildArt.cs`, `BuildShip.cs`, `Render.cs`, `Register.cs`, `Verify.cs`, `BattleChecks.cs` sequentially using `unity command run_script --file Tools/Blender/Tartan/<file> --entry <Class>.Main --timeout_ms 60000 --json`. Inspect nested `result.success` before continuing.
7. Inspect clean scenes, run `unity command run_tests --mode editor --filter TartanTests --async_tests true --json`, then poll `test_status`. Run relevant shared regressions when requested.
8. `uv run --with pillow python Tools/Blender/Tartan/Package.py` checks source hashes, live/wreck palettes and icon alpha bounds, then retains the editable output.

## Worn livery correction — 2026-10-14

- After rebuilding art/gameplay, run the shared [Empire fleet livery workflow](../EmpireFleetLivery/README.md). It supersedes the earlier team-mask/rim and stripe rendering steps.
- Neutral hull, small chipped hue-based stripes like Acclamator; live/wreck materials, ownership bindings, hologram and icons updated.
- Canonical current renders and verification: `Tools/Blender/EmpireFleetLivery/`. All four ship test suites passed (`39/39` total).
