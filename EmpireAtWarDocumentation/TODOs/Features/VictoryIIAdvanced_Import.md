---
category: Features
status: in-progress
updated: 2026-10-06
---
# Victory II Advanced Import

## Goal

- Import and register Victory II Star Destroyer — Advanced Loadout as Empire ship `203`.
- Verify saved assets and retain runtime acceptance separately.

## Implementation

- [x] Read import/placement/UI guidance; resolve map against live AOTR advanced XML.
- [x] Audit hull, six turret files, fighter dependency, textures, bone hierarchy and source hashes.
- [x] Convert hull and both distinct turret sources through isolated Blender 3.6.23 MCP; compare FBX geometry, UVs and parents.
- [x] Save art, gameplay, collision/shield/ion/selection/navigation bounds, own wreck and opaque-hull placement.
- [x] Configure `12,000 / 10,000 / 20`, 18 weapons, exactly 10 health targets, non-targetable hangar and TIE-Interceptors.
- [x] Register ship `203`, Empire roster, view/data Addressables, three icon consumers and reinforcement preview.
- [x] Verify source hashes, Unity geometry, saved references, metadata, import/compile checks and eight palettes/placement/wreck/icon renders.
- [ ] Clean-skirmish acceptance: combat, targetable systems, abilities, fighter launches, fog/selection, placement and destruction.
- [ ] Review provisional balance and source-to-project differences in [[GameDesign/Victory II Advanced Import]].

## Important Values

- 2026-10-06 team-color fix: replaced all-white hull masks with four mirrored stripes; hull/turret rim strength `0`; turret mask strength `0`, including wreck materials. Red/blue isolated Unity renders checked; existing GUIDs and linear imports retained; no new import/serialization errors. No tests or Play Mode run by this fix.

- Static integration verified `2026-10-06`; no automated Unity tests or Play Mode run.
- Source hull: 40 meshes / 24,024 triangles / 180 bones; two turret variants: each 4 / 1,447 / 6.
- Saved bindings: 10 health targets, 18 weapons, 23 fog hardpoints; no missing scripts or broken references.
- Visible bounds `65.43347 × 42.26736 × 110`; navigation radius `69`; ±8° banking.
- Near-coincident engine-effect chains introduce ≤0.088 gameplay-unit lens-plane displacement. Non-lens geometry error ≤0.000002101 raw import units; UVs and parents retained.
- Source hashes unchanged; existing shared registry/profile entries preserved.

## Files

- [[GameDesign/Victory II Advanced Import]]
- `Assets/Prefabs/Models/Ships/VictoryIIAdvancedShipView.prefab`
- `Assets/Settings/Data/Ship/VictoryIIAdvancedShipData.asset`
- `output/aotr-empire-units/VictoryIIAdvanced-Converted/`
