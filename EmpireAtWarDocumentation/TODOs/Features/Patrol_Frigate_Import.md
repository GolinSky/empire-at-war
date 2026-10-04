---
category: Features
status: in-progress
created: 2026-10-04
---
# Patrol Frigate Import

## Goal
- Import supplied `Recusant Patrol/CIS_Recusant_Patrol.ALO` as CIS **Patrol Frigate**, separate from Recusant.
- Complete [[Architecture/ALO_MODEL_IMPORT_GUIDE|ALO Model Import — Blender to Unity]] integration and acceptance.

## Decision
- Intact ALO; preserve original files and helper meshes. Source damaged ALO/death ALA are retained; static integration uses the project wreck flow.
- Reuse corvette model/entity/components; eight lasers, Power to Engines, no hangar. `ShipType.PatrolFrigate = 106`.
- User values: tech 1; cost 1,500; build 15 s; population 1; hull/shields/regen 900/700/15.
- User size: 60% of existing Recusant's 135.16174-unit length → 81.09705 units. Navigation radius scaled to 56.14411.
- Provisional project tuning: speed 36; turn 60; height 80; count limit 20. Details: [[GameDesign/Patrol Frigate Import]].

## Implementation
- [x] Audit source, textures, hierarchy and source hashes; all five originals unchanged.
- [x] Blender export and nondegenerate geometry/UV/attachment round-trip verification; nine meshes, 33 bones; identity Root restored.
- [x] Unity visual/gameplay/data, eight laser hardpoints + engine + shield; explicit shield/effects/fog/selection/team bindings.
- [x] Wreck, placement preview, transparent 512 px icon, eight team-color renders and all registrations.
- [x] Saved assets/reference/Console inspection: no missing scripts, broken references, donor model dependencies or new import/compile errors. Original import left MainMenuScene clean.
- [x] Resize to 60% of Recusant length; visual/gameplay/placement/wreck dimensions, hardpoint/effect anchors, shield/collider/ion/selection bounds, navigation and hull clearance saved and inspected. MainMenuScene was dirty before resizing and remains untouched.
- [ ] In-game acceptance: movement, laser targeting/arcs, Power to Engines, shields, selection/fog, placement, death/wreck.
- [ ] Provisional balance review. No Play Mode or automated tests run.

## Files
- [[GameDesign/Patrol Frigate Import]] — values, mappings, source caveats and verification.
- Exporter: `Tools/Blender/export_patrol_frigate.py`; evidence: `Temp/PatrolFrigateImport/`.
- Separate packed conversion: `C:/Users/golin/Documents/CIS_Space_Units2014_checked/CIS_Space_Units/Recusant Patrol-Converted/`.
