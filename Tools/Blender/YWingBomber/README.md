# BTL-A4 Y-Wing Bomber import

- AOTR 2.11.9 unit `R_Y_Wing`; model-map source `RV_Y_WING.ALO` plus `RV_Y_WING_TURRET.ALO` at `Turretbase`.
- Rebellion squadron `YWingBomber = 301`. Republic `YWing = 3` remains the separate BTL-B.
- Source root: `D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data/ART`.
- Follow vault `Architecture/ALO_MODEL_IMPORT_GUIDE.md`; preserve original sources and Blender's other scenes. Safe mode stays enabled.

## Conversion

1. Run `uv run --with pillow python Tools/Blender/prepare_ywing_bomber.py` to audit the two ALOs and convert three DDS textures losslessly.
2. Import each ALO through the existing Blender MCP into its fresh `YWingBomber Source` / `YWingBomberTurret Source` scene. Static import; no animations.
3. Run `Tools/Blender/export_ywing_bomber.py` through Blender MCP once per fresh source; `VARIANT` is `Hull` or `Turret`. It completes scoped ALAMO cleanup, preserves parenting and verifies FBX geometry/UVs/bones.
- Both sources already contain `Root`; no root repair is needed. `Collision` and `Shadow` are retained as disabled helpers. Four engine-glow meshes remain visible.
- Hull alpha is all zero; hull material is opaque. Team paint uses yellow hue `0.145`, range `0.06`, minimum saturation `0.35`, strength `1`; no alpha team mask.
- Blender editable files are retained in `Temp/YWingBomberImport/Output`. They contain the connected session's other scenes; use the named source scene. The scoped archive excludes these session snapshots.

## Unity integration

- `YWingBomberArt.cs`, `YWingBomberGameplay.cs`, `YWingBomberRegister.cs` record this import's one-time construction. Run through official `unity command run_script --file <file> --entry <Class>.Main --json` only on a fresh reviewed integration; they are not idempotent rebuild tools.
- Safe readback: `YWingBomberVerify.Main` and `YWingBomberArt.Inspect`. `YWingBomberArt.RenderAll` renders isolated geometry previews without changing open scenes or the global team palette.
- `verify_saved.py` checks persisted bindings and recorded live/conversion reports in `Temp/YWingBomberImport`; run with `uv run --with pillow python Tools/Blender/YWingBomber/verify_saved.py`.
- New visual/gameplay assets: `Assets/Prefabs/Models/Squadrons/YWingBomber.prefab`, `YWingBomberSquadronView.prefab`; configuration: `Assets/Settings/Data/Squadron/YWingBomberSquadronData.asset`.
- Six craft, 24 uniquely numbered weapons: laser → `MuzzleA_00`, ion → turret `MuzzleB_00`, two torpedoes → `MuzzleC_00/01`. Only craft are damage targets.
- Hull/shields/speed `30/30/30` per craft. Shield regeneration `0.15/s`; laser shield multiplier `0.7` on top of the project damage matrix. Other squadrons retain multiplier `1`.
- Existing Ion Shot: arrival disables target for `3 s`, recovery `20 s`, range `100`. Ordinary ion mounts deal project Ion damage; AOTR's passive `7.5 s` speed/fire-rate slowdown is not ported.
- Cost/build/tech/population/cap `500/15 s/1/1/10`, weapon damage/ranges and movement tuning remain project balance choices. Three-craft flights are not a separate roster entry.

## Verification

- Unity: eight hull meshes / `5,404` triangles, two turret meshes / `272` triangles; UVs on all vertices. All `17 + 6` bone names/parents match. Maximum Unity bone-position error `8.01e-7` before gameplay scaling.
- Opaque hull plus turret bounds `2.11971 × 0.60260 × 4.00000` project units, centered at zero; root scale `1`.
- Saved/live readback: six health/flight members, 24 weapon/fog bindings, 24 team renderers, zero missing scripts/broken references. Rebellion-only roster, mappings, Addressables, icon consumers and placement verified.
- Transparent 512px model icon, all eight distinct team renders, blue/green paint and six-craft deployment preview checked. Source hashes unchanged. No automated tests or combat Play Mode started by this task.
- Scoped archive: `output/aotr-rebel-units/YWingBomber-Converted/`; includes FBXs, textures, reports, previews and the source `Credits_and_Permissions.pdf`.
- Vault reference: `GameDesign/Y-Wing Bomber Import.md`; acceptance plan: `TODOs/Features/YWingBomber_Import.md`.
