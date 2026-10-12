# Endurance Fleet Carrier

## Source and balance

- Remake 4.0, Workshop `1770851727`; installed source under `D:/SteamLibrary/steamapps/workshop/content/32470/1770851727/Data/`.
- Art: `Art/Models/Endurance_Fleet_Carrier.ALO`; skirmish unit `Endurance_Fleet_Carrier_MP`. Actual texture directory: `Art/Textures/`.
- Full assembly: hull + command bridge + control tower + 12 turret pieces. Combat bridge belongs to another variant and is excluded.
- Project balance comes from vault `GameDesign/Rebel units.md`, section 9: hull 9,000; shields 7,000; speed 18; price 6,000; build time 65 s.
- User-approved complement: X-Wings only, six active plus six replacements.

## Gameplay mapping

| Mounts | Source attachments | Weapon profile |
| --- | --- | --- |
| 4 medium turbolasers | `EndurancePart02..05/FP_01` | `MediumTurboLaser` |
| 2 medium ion cannons | `EndurancePart06/FP_01`, `EndurancePart09/FP_01` | `MediumIonCannon` |
| 6 point defense | `EndurancePart10..13/FP_01`, `BL_02`, `BL_11` | `PointDefense` |
| 4 hangars | `Hanger`, `HB_02`, `HB_03`, `HB_04` | Independent bay hardpoints |
| Shield generator | `Shield_Gen` | Shield subsystem |
| 2 engines | Rear hull bounds, ±20% hull width | Engine subsystems |
| 2 tractor beams | `BL_05`, `BL_08` | Existing Tractor Beam ability |
| Command array | Command bridge `Core` | Disables Fighter Coordination when destroyed |

- 12 weapons + 10 systems = 22 targetable hardpoints with unique IDs, colliders and health entries. All bank with the hull.
- Extra source turret art remains visible. The simplified weapon count follows the balance note.
- Four hangars use active capacities `2/2/1/1`; stored reserves `4/4/2/2`. `HangarModel` consumes reserve for initial deployment → six replacements remain after six launches.
- Fighter Coordination: `+20%` accuracy and `+15%` speed for allied squadrons inside 750 units when activated; duration 10 s; recovery 45 s. Overlapping carriers retain one bonus until every effect ends.
- Command-array destruction, carrier destruction or ion disable ends coordination. Later arrivals are not added to an already active effect.
- Unique registrations: `ShipType.Endurance=322`, `ShipAbilityId.FighterCoordination=52`, `HardPointType.CommandArray=11`. Concurrent Liberator `321/51` registrations were verified separately.

## Project choices

- Visible size `93.462 × 43.440 × 180.000` project units; root scale `1`; banking range `-24.839 .. 21.727` at ±5°.
- Navigation radius 105; height tier, movement acceleration and radar settings inherited from Home One.
- Regeneration `11.667` shield points/s; initial launch delay 4 s; launch interval 8 s.
- Roster: level 4, limit 3, population 8. These values, size, command radius and subsystem health are project choices absent from the balance note.
- Team color selects authored red markings: mask strength `0`, livery strength `1`, hue `0`, hue range `0.06`, minimum saturation `0.5`. Source inverse alpha incorrectly selected neutral hull panels and is disabled on both live and wreck materials.
- Blue/green comparisons change `9.22%` of live hull pixels and `8.90%` of wreck pixels; neutral plating keeps its original color. Mixed opaque/effect submeshes remain split; original FBX geometry is intact.

## Verification

- 15 converted FBXs; maximum Blender geometry error `0.000089055` source units; bone error `0.000061510`.
- 40 source-file hashes unchanged; 52 texture copies pixel-identical to decoded source DDS/TGA.
- Imported meshes, UV presence, material slots, bone hierarchy, mandatory references, hardpoint setup, roster, Addressables, preview and icons checked: 10,197 saved-asset checks passed.
- Unity removed six zero-area triangles from `Hull Primary` and eight from `hull`; confirmed against the editable Blender source.
- Removed 72 helper meshes / 54,912 triangles from this ship's game prefabs; original FBXs keep helpers. Gameplay shield retained; zero remaining removable helpers.
- Transparent 512×512 icon and silhouette; alpha crop `[70,162,415,353]`. All eight live and wreck palettes differ. Blue/green, hologram and wreck renders inspected.
- Editor compilation/import completed; no Unity Console errors. Focused modifier regression tests added, not run. No automated test suite or Play Mode acceptance was run, per import-guide policy.
- Runtime firing/arcs, hangar launches, coordination, fog/selection and death timing still need gameplay acceptance. Static art uses existing project effects; source ALA/proxy animations are not recreated.

## Files and rebuild

- Art: `Assets/Art/{Models,Materials/Models,Textures/Models}/RebellionShips/Endurance/`.
- Visual/gameplay: `Assets/Prefabs/Models/Ships/Rebellion/Endurance.prefab`, `EnduranceShipView.prefab`.
- Wreck: `Assets/Prefabs/Models/Wrecks/EnduranceWreckView.prefab`; `Assets/Settings/Data/Models/Wrecks/Rebellion/EnduranceWreckData.asset`.
- Placement: `Assets/Prefabs/Ui/Reinforcement/EnduranceReinforcementView.prefab`.
- Data: `Assets/Settings/Data/Ship/Rebellion/EnduranceShipData.asset`; matchups/icons in their existing type-first directories.
- Evidence and renders: `Tools/Blender/Endurance/Evidence/`, `Previews/`. Editable piece `.blend` files: `Temp/EnduranceImport/<Piece>/<Piece>.blend`.

```powershell
uv run --with pillow python Tools/Blender/Endurance/Prepare.py
python Tools/Blender/Endurance/Convert.py
uv run --with pillow python Tools/Blender/Endurance/Stage.py
python Tools/Blender/Endurance/PrepareUnity.py
python Tools/Blender/Endurance/PrepareGameplay.py
python Tools/Blender/Endurance/PrepareRegistration.py
unity command run_script --file Tools/Blender/Endurance/BuildArt.cs --entry BuildEnduranceArt.Main --args '[true]' --timeout_ms 120000 --json
unity command run_script --file Tools/Blender/Endurance/BuildShip.cs --entry BuildEnduranceShip.Main --timeout_ms 120000 --json
unity command run_script --file Tools/Blender/Endurance/Render.cs --entry RenderEndurance.Main --timeout_ms 120000 --json
unity command run_script --file Tools/Blender/Endurance/Register.cs --entry RegisterEndurance.Main --timeout_ms 120000 --json
unity command run_script --file Tools/Blender/Endurance/Finalize.cs --entry FinalizeEndurance.Main --timeout_ms 120000 --json
unity command run_script --file Tools/Blender/Endurance/Verify.cs --entry VerifyEndurance.Main --timeout_ms 120000 --json
uv run --with pillow python Tools/Blender/Endurance/VerifySource.py
```

- Conversion skips pieces with an existing `ConversionReport.json`; use a fresh task directory for changed source inputs. Conversion operates on a saved Blender scene; preserve any unsaved work first.
