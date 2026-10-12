---
type: reference
updated: 2026-10-12
---
# MC30c Frigate Import

## Decision

- Art: Workshop `1770851727`, `Rebel_Mon_Calamari_MC_30.ALO`; textures resolve in `Data/Art/Textures`. Supplied `Texturesc` path is absent.
- Gameplay: [[GameDesign/Rebel units]] → hull `1,800`, shields `2,400`, speed `42`, price `2,500`, build `32 s`.
- Rebellion ship `307`; Torpedo Salvo `36`; cluster-bomb weapon `81`.
- User approved existing burst projectiles for cluster bombs: six-shot missile burst, no dedicated area damage.
- No hangar. Source death ALO `RV_MC30CMONCAL_D.ALO` is missing → generated project wreck from live hull.

## Implementation

- Fourteen weapon mounts: heavy proton torpedo ×2, medium turbolaser ×4, dual laser ×4, point defense ×2, cluster bomb ×2.
- All weapons plus engine/shield generator → `16` targetable hardpoints; unique ordered IDs, health and fog bindings.
- Torpedoes → `HP_TORP01/02_FP01`; turbolasers → `HP_TBL_01/02_FP01/02`; dual lasers → `HP_L_01/02_FP1/2`.
- Cluster/point defense → `HP_TORP03/04_FP01/02`; shield → `Shield`; engine → derived stern center. Remake XML has fewer mounts; balance mapping deliberately uses additional source sockets.
- Torpedo Salvo: torpedo-only shot/reload delay ×`2/3` → rate +50%; active `6 s`, recovery `30 s`. Existing lifecycle resets the modifier.
- Own visual/gameplay/placement/wreck/data/icon assets; Rebellion roster, ShipUi, tooltip, reinforcement, ShipsData, asset mapping, existing Addressable groups, ability/audio and icon-generator registrations saved.
- Binary Hull slot repair retains both opaque material definitions; additive windows/engines separate. Source inverse-alpha hull mask enables team paint; neutral decal material excluded.
- Root scale `1`, bow `+Z`, up `+Y`; size `11.652357 × 7.838146 × 80` project units; ±5° bank range `−3.925948 .. 3.944732`.

## Important Values

- Provisional: length `80`, nav radius `46`, level `2`, population `2`, max count `3`, regeneration `16 shields / 3 s`, weapon health `180`, engine/shield health `360`; donor movement/effect defaults and chosen arcs.
- FBX/source: `6` meshes, `11,860` triangles, `44` bones; three hidden helpers stay in conversion and are removed from game prefabs.
- Blender geometry error `0.000488733` source units; weapon sockets <`0.001`. Unused particle-helper bones reach `0.00704209` → helper tolerance `0.01`.
- Unity bone error `0.00006992456` project units; weapon sockets `0.000000355624`; gameplay socket alignment <`0.00001`.
- Saved verification: `1,851` checks passed; `14` source files unchanged; `9` texture copies pixel-identical.
- Transparent icon `512×512`; all eight live/wreck palettes distinct; blue/green/icon/wreck previews inspected. Imports/compilation successful; captured Console has no errors.

## Edge Cases

- No automated tests or Play Mode run. Torpedo-only modifier/reset NUnit regression source added, unrun.
- Initial closeout had unrelated `RapidDeploymentAbility.cs` compile errors; compilation is now successful.
- Hangar binding fix, 2026-10-12: remove donor `HangarComponent` GameObject. Zero bays → installer omits `HangarModel`; Zenject still injects leftover components. Builder/verifier enforce absence; saved/reimported prefab and revised builder passed 1,838 asset checks. Prefab regression test added, unrun.
- Runtime combat, expiry/cooldown, fog/selection, placement and wreck lifecycle remain unverified; provisional balance needs playtesting.
- Static ALO art retained; animated EaW proxies/shaders and source ALA behavior are not recreated.
- Repeat conversion uses isolated Blender MCP port `9890`; shared scenes must remain untouched.

## Files

- `Tools/Blender/MC30c/README.md` — commands, mapping, decisions and verification.
- `Tools/Blender/MC30c/Evidence/` — audited source, round trip, materials, sockets and saved-asset checks.
- `output/remake-rebel-units/MC30c-Converted/` — editable packed blend/FBX, textures, previews and reports.
- `Assets/Prefabs/Models/Ships/MC30cShipView.prefab`; `Assets/Settings/Data/Ship/MC30cShipData.asset`.
- [[Architecture/ALO_MODEL_IMPORT_GUIDE]]; [[Done/Features/MC30c_Import]].
