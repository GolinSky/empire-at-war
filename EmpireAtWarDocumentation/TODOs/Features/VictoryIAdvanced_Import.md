---
category: Features
status: in-progress
updated: 2026-10-06
---
# Victory I Advanced Import

## Goal

- Import and register the requested Victory I Star Destroyer — Advanced Loadout as Empire ship `202`.
- Verify the saved loadout, art and dependencies; retain a separate runtime acceptance record.

## Implementation

- [x] Read [[Architecture/ALO_MODEL_IMPORT_GUIDE]], project placement and UI guidance; verify live model map/XML.
- [x] Audit original hull, six turret sources, fighter, textures and bone hierarchy.
- [x] Convert through Blender 3.6.23 MCP; preserve hidden state/UVs; validate FBX geometry, parents and attachment positions.
- [x] Import materials/textures/FBX; save geometry, gameplay, shield/collision/selection/nav bounds, own wreck and preview.
- [x] Configure `12,000 / 8,000 / 175`, 16 weapons and exactly 10 health targets; non-targetable hangar and TIE-Interceptor complement.
- [x] Register ship `202`, Empire roster, Addressables, data/view/icon/tooltip/reinforcement mappings.
- [x] Verify source hashes, Unity geometry/UVs/attachments, saved references, import/compile checks and icon/eight palette renders.
- [ ] Clean-skirmish acceptance: combat/hardpoints/abilities/fighter launch/fog/selection/placement/death.
- [ ] Review provisional balance and source-to-project differences listed in [[GameDesign/Victory I Advanced Import]].

## Important Values

- Static integration verified `2026-10-06`; no automated tests or Play Mode run.
- Saved checks: 10 targetable hardpoints; 16 weapons; 4-shot artillery / 6-shot barrage; no missing scripts or broken references.
- Visible bounds `67.5321 × 38.1539 × 110`; navigation radius `70`.
- Source/verification package: `output/aotr-empire-units/VictoryIAdvanced-Converted/`.
- Shared Tractor Beam and TIE-Interceptor assets reused; source difference/provisional values recorded in the reference.

## Files

- [[GameDesign/Victory I Advanced Import]]
- `Assets/Prefabs/Models/Ships/VictoryIAdvancedShipView.prefab`
- `Assets/Settings/Data/Ship/VictoryIAdvancedShipData.asset`
