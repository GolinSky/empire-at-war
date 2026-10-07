# Executor Star Dreadnought

## Source

- Imported 2026-10-07 with Blender 3.6.23, ALAMO importer and Blender MCP; Unity 6000.4.7f1.
- `Temp/ALO_MODEL_MAP.txt` was absent. `output/aotr-empire-units/ship-catalog.csv` identifies `E_Executor_Star_Destroyer` as `Empire_Executor_Super_Star_Destroyer.ALO`.
- Source root: `D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data`.
- Hull plus ten distinct `Empire_Executor_Super_Star_Destroyer_HTL_01.ALO` through `_10.ALO` turrets. XML: `XML/SpaceUnitsSupers.xml`, `XML/Hardpoints_Supers.xml`, inherited projectile definitions.
- Original ALO, DDS and XML files remain unchanged; SHA-256 evidence is in `ImportEvidence.json`.

## Implementation

- Empire `ShipType.Executor = 207`; requested hull/shields `200000/200000`, speed `15`, Laser Beam and Tractor Beam.
- `51` targetable weapons: `10` heavy long-range turbolasers, `23` heavy dual turbolasers, `8` heavy long-range dual turbo-ions, `10` artillery-rocket launchers. Dedicated weapon profiles `49..52` retain `2/2/2/8` salvo counts.
- `123` integrated weapons: `69` medium long-range dual turbolasers, `40` heavy lasers, `14` repeating point-defense mounts. Profiles `53`, existing `28`, and `54` respectively. Medium salvo count overrides source `1` to requested `2`.
- Integrated weapons are intentionally excluded from health targeting, as explicitly requested. `59` targets: `51` weapons, `4` shield generators, `2` engines, `2` hangars. All `182` hardpoint IDs are unique.
- Split the source `20` dual heavy-laser and `7` dual point-defense pairs at their A/B fire bones to match the requested mount counts. Correct source `HP_EX_RPD_04` references `WTFP_8/9` to existing `WTFP_08/09` without editing the source XML.
- Shield generators use `SG_01..04`. Added two engine targets at the left/right engine bounds; two hangars use `Spawn_00/01`. Separate exits sit `8` units below the banked hull bottom.
- Two project fighter bays: TIE Fighter reserve `6`, TIE Bomber reserve `7`; each has `1` simultaneous slot, initial/launch interval `7.5 s`. The source provides one starting bomber plus six reserves; adding the fighter bay and counting all launches in each reserve are project choices.
- Shared hangar integration supports individual bay destruction and legacy prefabs with one launch point. Shared ship integration monitors every engine and interpolates to `MinMoveCoefficient` when all engines are destroyed.
- Own visual, gameplay, placement and wreck prefabs; own icon, silhouette, ship/wreck data and matchups; complete faction, Addressables, UI and audio registrations.

## Balance choices

- Source economy retained: price `125000`, build time `2500 s`, population `40`, maximum `1`; project availability level `5`.
- Gameplay length `1900` units, bank angle `±3°`, navigation radius `1009`; dimensions and banked hull range are recorded in `ImportEvidence.json`.
- Weapon target health `2500`; generators, engines and hangars `4000`. Source generator health is `4000`; weapon health is normalized and the added systems use project values.
- Shield regeneration `200/s` follows source rate; delay `1 s`, turning `1.5`, height tier `7`, weapon range conversion, projectile presentation and donor settings are provisional project balance.
- Original hull colors remain intact. Source alpha masks cover almost the entire hull, so recoloring is disabled for those masks; ownership uses the shared team-color rim at strength `0.3`. Eight owned palette variants were rendered.

## Conversion and verification

- ALO → FBX → Unity: `11` models, `377` meshes, `450857` triangles, `579` bones; UV layers, bone names, hierarchy and authored visibility retained. Identity `Root` restored only after verifying it in each ALO.
- `39` disabled collision/shadow/shield helpers (`207113` triangles) stripped from game prefabs only. Source FBX and editable Blender copies retain helpers; gameplay `ShieldSurface` remains.
- Unity attachment displacement, final dimensions, saved references, test results and known suite failures are recorded in `ImportEvidence.json`.
- Geometry-only icon, top/stern and owned-color renders are under `Temp/ExecutorImport/Previews`; local editable `.blend` files are under `Temp/ExecutorImport/Output`.
- Static weapon geometry imported. EaW animation, proxy particles and custom shader effects were not recreated. Automated data/model tests do not establish live combat balance.

## Rebuild

These scripts are specific to Executor and this source layout. Run from the repository root; keep the Editor in Edit Mode and use an isolated empty Blender session. Do not run conversion in a Blender session containing other work.

1. `python Tools/Blender/Executor/AuditSource.py`; create `Temp/ExecutorImport/Output/Textures`.
2. Execute `Convert.py` through `Tools/Blender/mcp_call.py` using its `code_file` request field. Save the JSON following `CONVERSION_JSON` in the log as `Temp/ExecutorImport/ConversionReport.json`.
3. `uv run --with pillow python Tools/Blender/Executor/Stage.py`.
4. `unity command eval --code 'UnityEditor.AssetDatabase.Refresh();' --json`.
5. Run each class below with `unity command run_script --file Tools/Blender/Executor/<Class>.cs --entry <Class>.Main --timeout_ms 60000 --json`:
   - `BuildExecutorArt`, `BuildExecutorShip`, `RenderExecutor`, `RegisterExecutor`, `StripExecutorHelpers`, `VerifyExecutorGeometry`.
6. Inspect saved assets, renders and Console. For authorized tests, inspect `unity command list_open_scenes --json`, then run `unity command run_tests --mode editor --filter ExecutorShipTests --async_tests true --timeout 300 --json` and poll `unity command test_status --json`.

The builders save assets through Unity APIs and upsert only Executor registrations. Rebuilding art/gameplay requires running helper stripping again.
