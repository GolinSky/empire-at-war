# Bothan Assault Cruiser

- Source: Remake 4.0, Workshop `1770851727`, `Neutral_Bothan_Cruiser.ALO`; resolved from `Bothan_Assault_Cruiser_MP`. Textures are under `Data/Art/Textures`.
- Balance: vault `GameDesign/Rebel units`, not Remake XML. Hull `7,200`, shields `6,500`, speed `22`, price `5,200`, build `58 s`.
- Workflow: `uv run --with pillow python Tools/Blender/BothanAssaultCruiser/Prepare.py` → `python Tools/Blender/BothanAssaultCruiser/Convert.py` → `uv run --with pillow python Tools/Blender/BothanAssaultCruiser/Stage.py`.
- Unity: `unity command run_script --file Tools/Blender/BothanAssaultCruiser/<File>.cs --json` for `BuildArt`, `BuildGameplay`, `Persist`, `Render`, `Register`, `Verify`. Inspect nested `result.success`.
- Package: `uv run --with pillow python Tools/Blender/BothanAssaultCruiser/Package.py`; output `output/remake-rebel-units/BothanAssaultCruiser-Converted`.
- Blender `3.6.23`, MCP protocol `13`, safe mode. Fresh Bothan source scene; export keeps six source meshes and 42 bones. Existing Blender work is retained. The editable blend is a saved conversion workspace; choose `BothanAssaultCruiser Source` for editing/export. Other imported scenes can remain in that workspace copy.
- Source binary material assignments are rebuilt per shader and slot. Source-equal triangle/UV round trip: geometry error `0.00004419`, bone error `0.00002369` source units. Unity error `0.00000056` units.
- Source model: `10,946` triangles including hidden shadow/shield; live visual: four renderers, `5,334` triangles. Hidden helpers remain in FBX and are removed from game prefabs. Wreck/placement use two opaque renderers.
- Visual length `160` project units, bounds `47.582 × 43.794 × 160`, root scale `1`, bow `+Z`, up `+Y`. Own shield mesh/collision/ion bounds, selection extent and launch points. Bank envelope `−21.992 .. 21.897` at `±5°`.
- IDs: ship `312`, Coordinated Assault `42`, existing Tractor Beam `22`. No existing enum values are renumbered.
- Weapon profiles: heavy dual turbolaser `3` ×4; medium turbolaser `4` ×4; medium ion cannon `32` ×2; medium proton torpedo `48` ×4; light point defense `14` ×2.
- Derived mounts: paired weapons at `HP_01/02` (heavy), `HP_07/08` (medium), `HP_05/06` (torpedo), with `Z ±3` units; ion `HP_03/04`; point defense `HP_07/08 + Y3`; engines `HP_Engine + X±6`; hangars `HP_Hangar + X±6`; shield `HP_Shield`; tractor `HP_Shield + (0,−3,10)`. `HardpointMapping.json` records every mount. All 22 hardpoints are targetable and have health entries.
- User-approved hangars: one active X-Wing `300`, one active Y-Wing `3`, one replacement each. Each bay has `reserve=2` because initial launches consume reserves; total four launches. Independent destruction bindings, `8`-unit clearance below hull, initial/interval `4/8 s`.
- Coordinated Assault reuses `TorpedoSalvoSettings` with delay multiplier `0.8` (rate ×1.25), `8 s` duration and `40 s` recovery. Tractor Beam uses existing corvette/frigate targeting.
- Provisional values: size, level `4`, population `8`, limit `3`, navigation radius `90`, weapon arcs, hardpoint health, launch timing, matchups and shields regenerating `43.333` every `3 s` (`14.444/s`). Level/population derive from Remake; remaining choices follow project donors.
- Verified saved model/material/UV/bone fidelity, four prefabs, stats, mounts, hangars, references, Addressables/mappings, roster/HUD/tooltip icon, placement, all eight live/wreck team palettes, transparent icon crop and original source hashes. Unity console contained no captured errors.
- No automated tests or Play Mode were run. Combat, launches/replacements, destruction, ability timing, fog/selection, reinforcement and wreck lifecycle need runtime verification. EaW particle proxies/ALA animations are not ported.
