# MC30c Frigate import

## Decision

- Rebellion `ShipType.MC30c = 307`; `MC30cTorpedoSalvo = 36`; cluster-bomb profile `81`.
- Source art: Workshop `1770851727`, `Data/Art/Models/Rebel_Mon_Calamari_MC_30.ALO`.
- Requested `Texturesc` directory is absent; all nine original references resolve in `Data/Art/Textures`.
- Gameplay authority: vault `GameDesign/Rebel units`; Remake XML stats are retained as source evidence, not applied as balance.
- Hull/shields/speed `1,800/2,400/42`; cost `2,500`; build `32 s`; torpedo rate +50%, active `6 s`, recovery `30 s`.
- User approved burst-projectile cluster bombs. Six-shot medium barrage-missile bursts use the existing taxonomy and visuals; dedicated area damage is not implemented.
- No hangar. XML death clone `RV_MC30CMONCAL_D.ALO` is absent; project wreck derives from the live hull.
- Hangar binding fix (2026-10-12): remove the donor `HangarComponent` GameObject completely. Zero bays makes `ShipInstaller` skip `HangarModel`; Zenject still injects disabled/unbound prefab components. The builder and verifier enforce its absence; `MC30cPrefabTests` covers this regression.

## Implementation

- Blender 3.6.23, ALAMO and MCP protocol 13; source `.blend`, FBX and packed textures retained in `output/remake-rebel-units/MC30c-Converted/SourceConversion/`.
- Binary material audit reconstructs both Hull slots and separates additive effects from opaque surfaces. Helpers remain in source conversion; game prefabs exclude Collision, Shadow and Shield meshes.
- Ship Lit opaque hull; additive window/engine effects; linear normal maps with green flip. Source texture pixels unchanged. Source inverse-alpha hull mask supplies team paint; neutral decal material is excluded.
- Unit roots have scale `1`, bow `+Z`, up `+Y`; size `11.652357 × 7.838146 × 80` project units. Collider/ion/shield/selection/placement fitted to hull. Bank ±5° → Y `−3.925948 .. 3.944732`.
- Frigate class and Light Frigate height tier. Provisional values: length `80`, navigation radius `46`, tech level `2`, population `2`, max count `3`, shield regeneration `16 / 3 s`, weapon HP `180`, engine/shield HP `360`, donor movement/effect defaults and firing arcs.
- Torpedo Salvo scales torpedo shot interval and reload by `2/3`; other families are unchanged. Existing ability lifecycle owns expiry/reset.
- Own gameplay/visual/preview/wreck/data/icon assets; faction, ShipUi, tooltip, reinforcements, ship data, mapping, existing View/Data Addressable groups, ability/audio and icon-generator registrations.

| Weapons/system | Count | Socket mapping | Profile |
| --- | ---: | --- | ---: |
| Heavy proton torpedo | 2 | `HP_TORP01/02_FP01` | 66 |
| Medium turbolaser | 4 | `HP_TBL_01/02_FP01/02` | 4 |
| Dual laser cannon | 4 | `HP_L_01/02_FP1/2` | 7 |
| Cluster bomb launcher | 2 | `HP_TORP03/04_FP01` | 81 |
| Point defense laser | 2 | `HP_TORP03/04_FP02` | 14 |
| Shield generator | 1 | `Shield` bone | — |
| Engine | 1 | Derived stern center, 6% length inside hull | — |

- All 16 targets have unique ordered IDs and health/fog bindings. Shared paired muzzles become separate gameplay mounts to match the requested balance composition. Remake's XML lists only four weapons; the project mapping deliberately uses additional authored sockets.

## Verification

- Six meshes, `11,860` triangles and `44` bones; all UVs and repaired source material slots preserved in Unity.
- Blender round trip: geometry error `0.000488733` source units; weapon sockets `<0.001`; maximum helper-bone error `0.00704209` on unused `Particle View` bones. This model uses a documented `0.01` tolerance for those helpers, retaining the `0.001` socket bound.
- Unity maximum bone error `0.00006992456` project units; weapon bone error `0.000000355624`; gameplay socket mappings `<0.00001`.
- Saved verifier: `1,851` checks passed. Four prefabs have no broken references or missing scripts; all 14 weapons targetable; own hull data, icons, placement, ability, taxonomy/audio and registrations resolved.
- Fourteen original source hashes unchanged; nine PNG references pixel-identical; icon `512×512`, transparent alpha box `(90,163)..(419,341)`; eight live/wreck palettes distinct and blue/green previews inspected.
- Imports/compilation idle and successful; no error entries in captured Console. NUnit regression source added for torpedo-only rate/reset; automated tests and Play Mode were not run.
- Closeout: after the successful MC30c verification, concurrent `RapidDeploymentAbility.cs` work introduced CS0535/CS0246 errors. The final Editor compilation-failed flag is true; MC30c assets/check results remain recorded, and unrelated code was preserved.
- Hangar-fix verification: saved/reimported prefab contains zero `HangarComponent`s; `1,838` saved-asset checks passed, revised builder compiles, source/texture/package checks passed. Editor compilation is now successful. Automated tests and Play Mode remain unrun.
- Runtime acceptance remains unrun: combat arcs, ability expiry/cooldown, fog/selection, reinforcement placement and death lifecycle. EaW animation/proxy shader behavior is not recreated.

## Files

- `Evidence/`: source, conversion, art, mount, Unity and package verification reports.
- Active assets: `Assets/Art/{Models,Materials/Models,Textures/Models}/RebellionShips/MC30c/`; prefabs/data/icon named `MC30c`.
- Rebuild: `Prepare.py` → `Convert.py` → `Stage.py` → Unity `BuildArt.cs`, `BuildGameplay.cs`, `Persist.cs`, `Render.cs`, `Register.cs`, `Verify.cs` → `Package.py`.
- Python dependencies: `uv run --with pillow python Tools/Blender/MC30c/<Script>.py`.
- Before a repeat conversion, launch portable Blender 3.6.23 in a separate hidden process with `--python Tools/Blender/MC30c/Start.py`; confirm port `9890` belongs to that process. `Call.py` pins the dedicated MCP port. Existing shared Blender scenes must remain untouched.
- Conversion skips existing completed reports; remove only this import's conversion report when deliberately rebuilding. Original source inputs stay read-only.
- `GenerateBuilders.py` captures the initial Battlecruiser asset-builder adaptation; committed C# builders are the reviewed rebuild source.
- Unity invocation: `unity command run_script --file Tools/Blender/MC30c/<Script>.cs --timeout_ms 60000 --json`; inspect nested `result.success`.
