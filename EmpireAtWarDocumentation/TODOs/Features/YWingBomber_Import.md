---
category: Features
status: in-progress
created: 2026-10-06
updated: 2026-10-06
---
# BTL-A4 Y-Wing Bomber Import

## Goal

- Add the AOTR BTL-A4 to **Rebellion only**, with six bombers at hull/shields/speed `30/30/30` each.
- Preserve source geometry/UVs/attachments and the existing Republic BTL-B.

## Decision

- New squadron `YWingBomber = 301`; source `RV_Y_WING.ALO` plus `RV_Y_WING_TURRET.ALO`.
- Reference: [[GameDesign/Y-Wing Bomber Import]]; workflow: [[Architecture/ALO_MODEL_IMPORT_GUIDE]].
- Use existing Ion Shot; weapon/economy/flight tuning remains provisional. Source passive ion slowdown is not ported.

## Implementation

- [x] Read model map and import/placement/UI rules; verify installed AOTR source and attachments.
- [x] Convert through connected Blender MCP; preserve originals; verify geometry/UV/bone FBX round trips.
- [x] Save Unity art, hull/turret visual prefab, six-craft gameplay and dedicated placement prefab.
- [x] Configure `30/30/30`, slow shield regeneration, anti-laser multiplier, 24 weapons and Ion Shot.
- [x] Register Rebellion roster, mappings, Addressables, icon consumers, placement, matchups and audio.
- [x] Verify persisted/live references, dimensions, mesh/UV/bone parity, icon and all team colors.
- [x] Preserve scoped conversion reports/previews/credits and model-specific tools.
- [ ] Clean-skirmish acceptance: deployment, six craft, flight/attack arcs, weapon impacts, shields, Ion Shot, fog/selection and teardown.
- [ ] Review provisional economy/movement/weapon values and documented source behavior differences.

## Important Values

- Six craft; 24 unique weapons; hull/shields totals `180/180`; shields `0.15/s`, laser factor `0.7`.
- Weapons/craft: one dual laser, one dual ion mount, two proton-torpedo launchers; no separately targetable systems.
- Existing Ion Shot: `3 s` disable on arrival / `20 s` recovery. Cost/build/tech/pop/cap `500/15 s/1/1/10`.
- Opaque hull+turret length `4` project units; unit root scale `1`.

## Verification

- 2026-10-06: source hashes unchanged; Blender position/UV checks and Unity mesh/triangle/UV/bone-parent parity passed.
- Loaded/saved assets: six health/flight members, 24 weapon/fog bindings, 24 team renderers, zero missing scripts/broken references. Only Rebellion owns squadron `301`.
- Eight distinct team renders, blue/green livery and six-craft placement checked; transparent 512px actual-model icon verified.
- Import/compile/readback checks passed; current console had no errors. No automated tests or combat Play Mode started by this task.
- Evidence: `output/aotr-rebel-units/YWingBomber-Converted/Reports/`; commands/tools: `Tools/Blender/YWingBomber/README.md`.

## Edge Cases

- Passive AOTR ion-stunner slowdown is not implemented; ordinary mounts deal Ion damage. Active Ion Shot uses project timing.
- Three-craft flight entry, repair and animated source effects are outside this import.
- Keep this plan active until runtime acceptance and balance/source-difference review are complete.
