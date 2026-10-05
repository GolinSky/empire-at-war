---
category: Features
status: in-progress
updated: 2026-10-05
---
# V-Wing Integration

## Goal

- Integrate the supplied local OBJ as Republic squadron `VWing = 6`.
- Five craft; per fighter hull/shields/regen `80/25/3`; four laser cannons; cost/build/population `600/15 s/1`.

## Rules

- Follow [[Architecture/ALO_MODEL_IMPORT_GUIDE]] integration steps; skip ALO/Blender acquisition and conversion.
- Preserve source model/MTL/texture bytes and unrelated user changes. No automated tests requested.
- Open MainMenu scene is dirty; use isolated preview scenes.

## Decision

- Reuse existing squadron Hunt order and passive repair support; repair `1 HP/s` is provisional, matching Y-Wing.
- Adapt RaW speed `6.0/5.5`, turn `3.0` to project flight units; record actual values. Tech `5`, max count `10` provisional.
- No automatic changes to other ships' hangars or prior approved substitutions.

## Implementation

- [x] Read guide, placement/UI rules, backlog and live squadron references.
- [x] Create materials, centered 4-unit geometry prefab, five-member gameplay prefab with twenty lasers and ten trails, fitted dedicated placement preview.
- [x] Save data, enum `VWing = 6`, Republic roster, mappings, existing View/Data Addressables, transparent icons and matchups.
- [x] Saved-reference/geometry/import/compile verification passed; five health targets, twenty unique body-mounted weapons, bound fog/team/preview arrays, own icon/placement mappings, no missing scripts or donor dependencies. Eight team renders produced; blue/green inspected.
- Source hashes: nine original model/MTL/texture files unchanged. No new Console errors; existing warnings only. No automated tests or Play Mode run.
- Final source/project values and exact paths: [[GameDesign/V-Wing Import]].
- [ ] Clean-skirmish acceptance and provisional balance review.

## Files

- Source: `Assets/Art/Models/RepublicShips/Vwing/V-wing.obj`.
- Reference: [[GameDesign/V-Wing Import]].
