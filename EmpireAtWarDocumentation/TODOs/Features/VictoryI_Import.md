---
category: Features
status: in-progress
updated: 2026-10-07
---
# Victory I Import

## Goal

- Import and register the requested Victory I Star Destroyer as Empire ship `202`.
- Verify the saved loadout, art and dependencies; retain a separate runtime acceptance record.

## Implementation

- [x] Read [[Architecture/ALO_MODEL_IMPORT_GUIDE]], project placement and UI guidance; verify live model map/XML.
- [x] Audit original hull, six turret sources, fighter, textures and bone hierarchy.
- [x] Convert through Blender 3.6.23 MCP; preserve hidden state/UVs; validate FBX geometry, parents and attachment positions.
- [x] Import materials/textures/FBX; save geometry, gameplay, shield/collision/selection/nav bounds, own wreck and preview.
- [x] Configure `12,000 / 8,000 / 175`, 16 weapons and exactly 10 health targets; non-targetable hangar and TIE-Interceptor complement.
- [x] Register ship `202`, Empire roster, Addressables, data/view/icon/tooltip/reinforcement mappings.
- [x] Verify source hashes, Unity geometry/UVs/attachments, saved references, import/compile checks and icon/eight palette renders.
- [x] 2026-10-07: remove the Advanced suffix; use `VictoryI` across code, assets, tooling, conversion archive and documentation; preserve ship ID `202` and current balance.
- [ ] Clean-skirmish acceptance: combat/hardpoints/abilities/fighter launch/fog/selection/placement/death.
- [ ] Review provisional balance and source-to-project differences listed in [[GameDesign/Victory I Import]].

## Important Values

- 2026-10-07 rename: all 38 moved asset/folder GUIDs preserved; 3,381 object geometry/transform/material/visibility records unchanged. Saved ship checks passed with `5,500/2,200/24`; five tooling scripts compile; reinforcement prefab test `1/1` passed. No new import/serialization errors; no Play Mode run.

- 2026-10-06 team-color fix: replaced all-white hull masks with four mirrored stripes; hull/turret rim strength `0`; turret mask strength `0`, including wreck materials. Red/blue isolated Unity renders checked; existing GUIDs and linear imports retained; no new import/serialization errors. No tests or Play Mode run by this fix.

- Static integration verified `2026-10-06`; no automated tests or Play Mode run.
- Saved checks: 10 targetable hardpoints; 16 weapons; 4-shot artillery / 6-shot barrage; no missing scripts or broken references.
- Visible bounds `67.5321 × 38.1539 × 110`; navigation radius `70`.
- Source/verification package: `output/aotr-empire-units/VictoryI-Converted/`.
- Shared Tractor Beam and TIE-Interceptor assets reused; source difference/provisional values recorded in the reference.
- 2026-10-06 rebalance: `12,000/8,000/175` → `5,500/2,200/24`; range `250` → `500`; price/build/capacity `12,250/245 s/7` → `5,000/30 s/4`. Team color list `45` → `81` renderers.
- 2026-10-06 also: ISD I `20,000/16,000/250`, range `250` → `8,000/4,000/15`, range `500`; `22,000/440 s/8` → `8,500/60 s/5`; team color `51` → `89`. TIE Avenger `650/10 s`, Interceptor `450/8 s`, Punisher `800/12 s`.
- Wreck tests pass; firing still not Play Mode verified.

## Files

- [[GameDesign/Victory I Import]]
- `Assets/Prefabs/Models/Ships/VictoryIShipView.prefab`
- `Assets/Settings/Data/Ship/VictoryIShipData.asset`
