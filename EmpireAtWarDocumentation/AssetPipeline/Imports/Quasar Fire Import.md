---
type: integration-record
updated: 2026-10-12
tags:
  - ship-import
  - rebellion
  - alo
system:
  - asset-pipeline
  - units
record_state: historical
reviewed: 2026-10-20
---
# Quasar Fire Import

> [!info] Import record
> Integration and verification evidence recorded at the dates below. Gameplay values are snapshots and may differ from current assets. Current rules/data ownership: [[GameDesign/Units_And_Factions]], [[GameDesign/Combat]] and [[GameDesign/Economy]]. Open acceptance work remains in its linked TODO plan.

## Decision

- Gameplay stats: [[GameDesign/Proposals/Rebel units]]; art/sockets: Remake workshop `1770851727`, `Rebel_Quasar.ALO`.
- User-approved hangar: X-Wing + Y-Wing; 1 active + 1 reserve each. Rule saved in `Rebel units`.
- Shared Tractor Beam ability operates the two tractor generators; Fighter Reinforcements launches one available replacement, cooldown `30 s`.

## Implementation

- `ShipType.QuasarFire = 320`; `ShipAbilityId.FighterReinforcements = 50`.
- Weapons: `2× TurboLaser` at `TL_01/TL_02`; `2× PointDefense` at `L_03/L_06`.
- Systems: engine at `Pe_Quasar`; hangars at `Hanger_01/Hanger_02`; tractors at `CM_01/CM_02`.
- Engine/tractor sockets are derived mappings; source supplies no dedicated engine/tractor bones.
- `9` targetable hardpoints, all bound to health/fog. Both hangars have separate launch points below the hull.
- Each serialized bay uses `reserve=2`, `maxActive=1`; reserve includes its initial launch → 1 active + 1 remaining reserve per type.
- Replacement requires a previously launched, vacant, surviving bay and remaining reserve; respects shared launch slots.
- Dedicated gameplay, placement, wreck, model icon, silhouette and registrations persisted. Hidden helpers remain in source FBX/blend, absent from game prefabs.
- Neutral source hull: preserve pixels; use team rim coloring. Source alpha is unsuitable as a selective paint mask.
- Engine/hangar shield surfaces use additive materials; source animated shaders/proxy particle emitters are not recreated.

## Important Values

- Hull `1700`; shields `1300`; speed `34`; price `2400`; build `35 s`.
- Project choices: length `70`, radius `43`, level `2`, population `3`, max count `3`; launch delays `4/8 s`; shield regen `13/s`, delay `3 s`.
- Hardpoint health choices: weapons `150`, engine `250`, hangars `200`, tractors `150`.
- Source: `9` meshes, `9684` triangles, `33` bones; geometry/UV/material slots/hierarchy preserved.
- FBX errors: geometry `0.0000186`, bones `0.0000100` source units. Unity attachment error `0.000000263` units before gameplay scale.
- Gameplay bounds approximately `47.25 × 9.28 × 70`; bank range `-5.764 .. 5.203` at ±5°.

## Files

- `Tools/Blender/QuasarFire/README.md` — workflow, provenance, mappings, limits.
- `output/eaw-rebel-ships/QuasarFire-Converted/` — editable blend/FBX/textures, source hash audit, conversion and registration evidence, previews.
- `Assets/Prefabs/Models/Ships/QuasarFireShipView.prefab`; `Assets/Settings/Data/Ship/QuasarFireShipData.asset`.
- `Assets/Prefabs/Ui/Reinforcement/QuasarFireReinforcementView.prefab`; `Assets/Prefabs/Models/Wrecks/QuasarFireWreckView.prefab`.
- `Assets/Scripts/Tests/Editor/QuasarFireHangarTests.cs` — replacement reserve/capacity/destroyed-bay regression tests, not run.

## TODO

- Saved-asset inspection passed on 2026-10-12: model fidelity, slots, hardpoint IDs/health, hangar bindings, Addressables, roster, icon, placement, missing scripts/references.
- Eight team palettes, icon, placement and wreck rendered; blue/green, placement and wreck visually inspected. All palette images differ; icon alpha bounds `(39,145)..(474,367)` in `512×512`, uncropped.
- `15` source files verified unchanged. Final Unity state: compilation/import complete, `scriptCompilationFailed=false`; no new Console errors after final compilation.
- Automated tests and Play Mode combat not run. Runtime launch/replacement, tractor beam, fog/selection and death remain unverified.
