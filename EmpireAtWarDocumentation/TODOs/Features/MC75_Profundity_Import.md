---
category: Features
status: in-progress
---
# MC75 Profundity Import

## Goal
- Integrate requested MC75 into Rebellion with the authored hardpoints, approved speed/fighters and Full Salvo.
- Complete static import checks; retain runtime acceptance work explicitly.

## Implementation
- [x] Read model map, ALO guide, placement and UI notes; audit installed AOTR model/XML and unchanged source hashes.
- [x] Import intact model, textures, authored attachment hierarchy and owner colors; preserve 17 meshes, all triangles and 147 bones.
- [x] Save visual/gameplay/placement/wreck/shield assets with 12 targetable hardpoints and 30 weapons.
- [x] Set hull/shields/speed `18,000/13,000/22.5`; X-Wing/Y-Wing bays; implement Full Salvo `20 s/60 s` and selective firing delays.
- [x] Register Rebellion roster, ship data/view, existing Addressables groups, icons/tooltips, placement, ability and audio.
- [x] Reload saved assets and inspect hierarchy, references, counts, muzzle alignment, collider clearance, renders, import and compilation. No automated tests or Play Mode run.
- [ ] Clean-skirmish acceptance: build/placement, team/fog rendering, targeting and all weapon categories.
- [ ] Verify fighter launches, shield/engine/hangar destruction, Full Salvo expiry/recovery and wreck lifecycle.
- [ ] Review provisional scale, flight, weapon/range, economy and build limit.

## Files
- [[GameDesign/MC75 Profundity Import|Source values, decisions and inspection evidence]]
- `Tools/Blender/MC75Profundity/`; ignored reports `Temp/MC75ProfundityImport/`.

## Important Values
- 2026-10-06: import and static integration complete; no missing scripts/broken saved references/donor geometry. Source ALO/seven DDS unchanged.
- Unity bones `147`, maximum displacement `0.000001585` units, no parent mismatches; hardpoint errors `0`, launch clearance `8` units.
- Active plan remains for unverified runtime acceptance and provisional balance review.
- 2026-10-06 rebalance: hull/shields `18,000/13,000` → `8,000/3,500`; range `400` → `500` (enemies at 500 outranged/out-saw it); price/build/capacity `13,000/260 s/8` → `5,500/40 s/4`.
- 2026-10-06 wreck rebuilt via `ShipWreckBuilder.Build`: was 17 parts incl. engine glows, lens flares, lights, hidden shadow meshes → `Engines`, `Hull`. Team color list `2` → `18` renderers.
- Edit Mode: 1043/1044 pass; only failure is concurrent `ArquitensAdvancedShipView` team color. Firing still not Play Mode verified.
