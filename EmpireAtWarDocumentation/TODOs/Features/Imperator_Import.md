---
category: Features
status: in-progress
date: 2026-10-05
---
# Imperator Import

## Goal
- Import `EV_StarDestroyer_MKI.ALO` as the Republic Imperator-class Star Destroyer.
- Replace Tractor Beam with Laser Beam; integrate data, weapons, hangar, wreck, placement, icons and team colors.

## Rules
- Follow [[Architecture/ALO_MODEL_IMPORT_GUIDE]]; preserve original source files and attachment hierarchy.
- No automated tests requested. No Play Mode started; runtime acceptance remains unverified.
- User approved ARC-170/Delta-7 substitutes; ARC-170 selected for the fighter bay.

## Implementation
- [x] Read guide, organization/UI rules, backlog and live ship/ability sources.
- [x] Inspect Blender 3.6.23 / Unity 6000.4.7f1; audit both source variants and verify missing identity Roots.
- [x] Export/verify intact and damaged models; preserve all 32 meshes, attachments and visible UVs; convert eight DDS losslessly.
- [x] Build geometry/gameplay/placement/wreck prefabs, shield, ship data and 27 weapons / 30 health targets.
- [x] Register Republic roster, mappings, existing Addressables entries and all icon consumers.
- [x] Replace Tractor Beam with Laser Beam catalog/audio binding; configure 8 Y-Wing launches / 2 simultaneous.
- [x] Verify saved references, bounds, eight living/wreck team palettes, uncropped icons and import/compile status.
- [x] Save source-side conversion bundle with editable packed blends, FBXs, textures, reports, previews and credits.
- [x] Apply approved ARC-170 substitute: 14 total launches / 4 simultaneous; saved view/data mappings resolve.
- [x] Disable StarDestroyer2 by removing its Republic build-roster entry; player UI and AI both consume that roster.
- [ ] Runtime combat/ability/hangar/placement/destruction acceptance and provisional balance review.

## Decision
- User values: hull/shields/regen `7,500/3,500/50 per second`; cost/build/population/tech/limit `8,000/90 s/4/3/1`.
- Weapons: 6 heavy twin turbolasers, 2 heavy twin ions, 14 turbolasers, 5 ions; no point defense.
- Local XML differs: ARC-170 bombers and limit 3; user Y-Wings and limit 1 take precedence.
- V-Wing missing from project. User approved ARC-170/Delta-7 substitution; use 14 ARC-170 launches / 4 active alongside 8 Y-Wings / 2 active.
- User requested StarDestroyer2 disabled; its Republic roster entry is removed. Existing Imperator entry remains.
- Beam uses existing implementation/project tuning `6,000 damage / 8 s active / 50 s recovery / 500 range`; no new combat logic.

## Edge Cases
- Static inspections passed: intact `26 meshes / 34,974 triangles / 168 bones`; wreck `6 / 36,119 / 75`; all parents match.
- Unity bone/geometry error ≤`0.000013604 / 0.000004053` project units; visible UV tolerance `0.00001`. Source ALO/DDS hashes unchanged; PNG pixels identical.
- Four saved prefabs: no missing scripts, broken references or donor-model dependencies. Health/fog/weapon bindings `30/30/27`; all targets bank correctly.
- Unity remaps UVs on disabled shadow helpers; authored UVs retained in blend/FBX. Missing `ISDI_shiplights.dds` affects only disabled damage helper.
- Static conversion excludes death ALA and EaW animated effects. No new Unity import/serialization/compilation errors; no automated tests or Play Mode run.

## Files
- Reference and exact values: [[GameDesign/Imperator Import]].
- Exporter and mapping: `Tools/Blender/export_imperator.py`, `Tools/Blender/README.md`.
- Bundle: `F:/EaW/Republic_at_War_121_MINIMAL/Mods/Republic_At_War/Data/Art/Models/EV_StarDestroyer_MKI-Converted/`.
