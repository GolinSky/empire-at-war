# MC50 Medium Cruiser

- Guide: vault `Architecture/ALO_MODEL_IMPORT_GUIDE`; balance: `GameDesign/Rebel units`, MC50 section.
- Source: Workshop `1770851727`, `Data/Art/Models/Rebel_Mon_Calamari_MC_50.alo`. Textures are in `Data/Art/Textures`; `Texturesc` does not exist.
- Rebellion ship ID `311`; Hit and Run ability ID `40`. IDs deliberately avoid concurrent imports.
- Hull/shields/speed: `4,200 / 3,000 / 27`; price/build time: `3,400 / 42 s`.
- Weapons: `2` HeavyTurboLaser (`2`), `4` DualMediumTurboLaser (`5`), `2` MediumIonCannon (`32`), `4` Laser (`8`), `2` PointDefense (`14`). Reuses existing profiles without altering their shared balance.
- `14` weapons + engine + shield generator = `16` targetable hardpoints, IDs `0..15`. Every weapon is in health/fog collections; health data covers all three used hardpoint types.
- Socket mappings are in `output/remake-rebel-ships/MC50-Converted/HardpointMapping.json`. The requested 14-mount composition differs from Remake's five gun groups; extra gameplay mounts use actual firing/damage/effect sockets. Mappings/arcs are project choices, not Remake weapons.
- No hangar or squadrons: the requested balance note specifies neither. Source hangar art stays visible.
- Hit and Run uses existing `BoostEnginePowerSettings`: speed ×`1.25`, all other multipliers `1`; active `8 s`, recovery `35 s` after expiry. Dedicated definition/icon/audio registration.
- Provisional values: level `2`, population `4`, max count `3`, range `600`, navigation radius `65`, ±`5°` banking, weapon/system health `300/700/600`, shield regeneration `20` every `3 s`, matchup entries and arcs ±`85°`.
- Class: Cruiser (`8`). Root scale `1`, bow `+Z`, up `+Y`; visible size including effects `41.470 × 19.965 × 110.000`; opaque hull/preview/wreck length `109.348`. Bank envelope `−10.016 .. 9.983`.
- Source FBX/blend retain all ten meshes and 51 bones. Visual/gameplay prefabs remove authored shadow/collision/shield helpers. Three opaque surfaces feed wreck/explosion/team ownership; separate engine/window/hangar effects remain visible on the living ship.
- Hull source texture has broad inverse-alpha colorization coverage. Project team-mask strength `0.25` preserves neutral panel detail; team rim `0.6`. Wreck materials retain the same settings. All eight live/wreck palettes are distinct.
- Source `MeshShield` engine distortion is approximated by additive rendering; EaW shader animation/refraction and ALA animations are not ported.

## Rebuild

1. Run `uv run --with pillow python Tools/Blender/MC50/Prepare.py`.
2. Start portable Blender `3.6.23` with `--python F:/Private/empire-at-war/Tools/Blender/MC50/Start.py` in a hidden process. Dedicated MCP port `9891` retains configured safe mode and disabled telemetry; shared Blender stays separate.
3. Run `python Tools/Blender/MC50/Convert.py`, then `uv run --with pillow python Tools/Blender/MC50/Stage.py`. Conversion expects a fresh instance and repairs source material slots before export.
4. For an initial build, run `unity command run_script --file Tools/Blender/MC50/<File>.cs --timeout_ms 60000 --json` in order: `BuildMC50Art`, `BuildMC50Gameplay`, `PersistMC50`, `RenderMC50`, `RegisterMC50`, `VerifyMC50`. Inspect nested `result.success`.
5. Run `uv run --with pillow python Tools/Blender/MC50/Package.py`.

## Verification

- Blender triangle corners/UVs/bone hierarchy round trip passed; maximum geometry error `0.00004645`, bone displacement `0.00012642` source units.
- Unity: `10` meshes, `35,592` triangles, `51` bones; attachment error ≤`0.000000563` units. Source material-slot assignments/triangle totals/UVs preserved.
- `2,296` saved-asset checks passed: four prefabs, references, dimensions, targetable mounts, socket positions, requested data, icon consumers, ability, placement, asset mappings and existing Addressables groups.
- `16` source hashes unchanged; `11` texture references lossless; `50` asset/metadata pairs verified.
- Transparent `512 × 512` icon alpha bounds: `(69,128)..(438,354)`; no clipping. Icon, silhouette, placement, wreck and eight live/wreck palette renders saved.
- No automated Unity test suite or combat Play Mode run by this import. Firing arcs, target destruction, ability timing, selection/fog, reinforcement and death behavior need runtime acceptance.
- Evidence: `ImportEvidence.json`; editable conversion package: `output/remake-rebel-ships/MC50-Converted/`.
