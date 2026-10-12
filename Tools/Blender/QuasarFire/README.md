# Quasar Fire import

- Source: Remake workshop `1770851727`, `Data/Art/Models/Rebel_Quasar.ALO`; textures from `Data/Art/Textures`.
- Gameplay reference: vault `GameDesign/Rebel units.md`. Remake XML supplies sockets and material provenance, not balance.
- `ShipType.QuasarFire = 320`; `ShipAbilityId.FighterReinforcements = 50`. IDs avoid concurrent MC30c imports.
- Hull/shields/speed: `1700 / 1300 / 34`; price/build time: `2400 / 35 s`.
- Nine targetable hardpoints: two light turbolasers (`TurboLaser`), two point defense, one engine, two hangars, two tractor generators.
- Socket mapping: turbolasers `TL_01/TL_02`; point defense `L_03/L_06`; hangars `Hanger_01/Hanger_02`.
- Derived system mapping: engine at `Pe_Quasar`; tractor generators at `CM_01/CM_02`. No canonical engine/tractor sockets exist in this model.
- X-Wing and Y-Wing: one active + one replacement each. Serialized `reserve=2` counts all launches, including the initial squadron.
- Fighter Reinforcements consumes one remaining reserve from a surviving, vacant bay after its initial squadron has launched. Cooldown `30 s`; respects the shared launch slot. Tractor generators use the existing Tractor Beam ability.
- Project choices: length `70`, navigation radius `43`, level `2`, population `3`, maximum count `3`, launch delays `4/8 s`, shield regen `13/s` after `3 s`.
- Hardpoint health choices: weapons `150`, engine `250`, hangars `200`, tractors `150`.
- Visible bounds: `47.250 × 9.281 × 70.000`; bank envelope at ±5°: `-5.764 .. 5.203`.
- Source fidelity: nine meshes, `9684` triangles, `33` bones, preserved UVs/material slots/hierarchy. FBX geometry error `0.0000186`, bone error `0.0000100` source units; Unity bone error `0.000000263` units before gameplay scaling.
- Source `.blend`/FBX retain authored helpers. Visual/gameplay/preview/wreck prefabs omit hidden `Shield`, `Shadow`, `Col`; gameplay shield has a separately baked hull.
- `MeshShield` engine/hangar surfaces use additive transport materials. EaW animated shader effects and proxy particle emitters are not recreated.
- Source hull is neutral; inverse alpha covers approximately `99.999%` of the atlas and is unsuitable as a selective team mask. Use team rim coloring; preserve hull pixels.
- Editable package: `output/eaw-rebel-ships/QuasarFire-Converted/`; Unity art: `Assets/Art/{Models,Materials,Textures}/.../RebellionShips/QuasarFire/`.
- Saved registrations, material slots, hardpoints, hangar bindings, references, prefab root scales and model fidelity passed `Inspect.cs` on 2026-10-12. Eight palette renders, icon, silhouette, placement and wreck produced; blue/green, placement and wreck inspected.
- Icon alpha bounds `(39,145)..(474,367)` in `512×512`; all eight team renders differ. Final Unity compilation/import complete, `scriptCompilationFailed=false`; no new Console errors after final compilation. Source hash verification: `15` unchanged files.
- Added `QuasarFireHangarTests`; automated tests and Play Mode combat were not run. Verify launch/replacement, tractor beam, fog/selection and death in an authorized runtime session.

## Rebuild

1. In a fresh compatible Blender session, run `uv run --with pillow python Tools/Blender/QuasarFire/Prepare.py`, then `Convert.py`. Conversion deletes scene objects; do not run against another import's live scene.
2. Stage the FBX and PNGs into the Quasar art folders. `BuildArt.cs` expects a fresh art import.
3. Run `BuildArt.cs → BuildGameplay.cs → Persist.cs → Render.cs → Register.cs → Inspect.cs` with `unity command run_script --file Tools/Blender/QuasarFire/<File>.cs --timeout_ms 60000 --json`. Inspect nested `result.success`.
4. Run `Package.py`; it verifies source hashes and saves the editable outputs/reports.
