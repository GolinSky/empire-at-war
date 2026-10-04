---
category: Features
status: in-progress
---
# Mandator Import

## Goal
- Import Pride of the Core using `ReV_Mandator.ALO`; use `ReV_Mandator_D.ALO` as the wreck source.
- Complete art, gameplay data, 64 listed hardpoints, Republic roster, icons, placement and team colors.

## Decision
- User-approved source correction: RaW XML assigns `_D` to `Mandator_Death_Clone`.
- User-approved provisional hangar substitution: Delta-7 + A-Wing.
- Length 2× existing Malevolence; measured on saved prefabs, preserving uniform proportions.
- User list: 56 weapons + 8 systems = 64 hardpoints. Local XML lists 14 point-defense lasers / 62 total; two supplemental mounts preserve the user's 16-laser loadout.

## Implementation
- [x] Read `ALO_MODEL_IMPORT_GUIDE`, `PROJECT_ORGANIZATION`, and `UI_UX_GUIDELINES`.
- [x] Audit ALO skeletons/textures; verify identity Roots (105 living / 70 damaged bones).
- [x] Use compatible Blender 3.6.23; retain existing Blender 4.2 file/scenes.
- [x] Export both packed Blender files and FBXs; verify triangle corners, UVs and bone hierarchy.
- [x] Create and register art/gameplay/wreck/placement/icon/data assets and Republic roster entry.
- [x] Verify saved references, exact length ratio, 64 bindings, all eight palettes and clean Unity import/compilation.
- [x] Retain conversion package beside sources; verify original ALO hashes unchanged.
- [ ] Implement Tractor Beam ability and Lucrehulk immunity; currently a destroyable structural mount.
- [ ] Review independent multi-engine/two-hangar damage behavior and shared shield-generator behavior.
- [ ] Accept combat, placement, hangar, ability and wreck behavior in a clean skirmish; review provisional balance.

## Important Values
- User hull / shields / regen: **9,000 / 8,000 / 100**.
- RaW cost / build / population / limit / tech: **70,000 / 90 s / 5 / 1 / 4**.
- Weapons: 8 quad + 4 heavy + 8 twin turbolasers, 8 ions, 16 point defense, 8 concussion missiles, 4 proton torpedoes.
- Length **956.803** vs Malevolence **478.402** project units; flight **Y=−520** through dedicated height data.
- `ShipType.Mandator=9`, `QuadTurboLaser=20`, `TractorBeam=7`; quad tuning provisionally copies dual-heavy with 8-shot salvo.
- Power to Weapons configured; Delta-7 **24 total / 4 active**, A-Wing **18 / 2**, initial / interval **4 / 6 s**.
- Movement, hardpoint health, firing arcs and wreck tuning are provisional project choices.

## Edge Cases
- Existing hangar binds `HP_SPAWN_01`; second hangar is structural. Existing movement observes first engine; either shield generator's destruction collapses shields.
- Matching DDS files resolve absent damaged-model `.tga` references. Static `_D` frame feeds procedural wreck behavior; ALA death animation is not converted.
- Retain dedicated `_D` wreck: generic intact-hull regeneration would overwrite it.
- MainMenuScene was dirty and remains untouched; no automated tests or Play Mode requested/run.

## Verification
- Blender round trip: living **15 meshes / 9,897 triangles / 105 bones**, damaged **24 / 23,279 / 70**; UV/geometry/hierarchy checks passed.
- Unity: **105 / 70** bones, no parent mismatches; maximum bone error **0.00000514 / 0.00001267** project units. Visible wreck triangles unchanged (**9,388**).
- Four saved prefabs have zero missing scripts/references; health/fog bindings **64**, weapons **56**, unique target IDs **64**.
- Original ALO hashes unchanged; final import/compile/save and Console checks clean; eight living/wreck palettes and transparent icon rendered.
- Import scope is saved and verified. Plan remains active for unsupported abilities/system behavior and battle acceptance; no completion date assigned.

## Files
- Reference: [[GameDesign/Mandator Import]].
- Exporter / procedure: `Tools/Blender/export_mandator.py`, `Tools/Blender/README.md`.
- Runtime prefab: `Assets/Prefabs/Models/Ships/MandatorShipView.prefab`.
- Package: `F:/EaW/Republic_at_War_121_MINIMAL/Mods/Republic_At_War/Data/Art/Models/ReV_Mandator-Converted/`.
- Evidence: `Temp/MandatorImport/`.
