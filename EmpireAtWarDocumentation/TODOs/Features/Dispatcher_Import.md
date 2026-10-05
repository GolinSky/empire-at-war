---
category: Features
status: in-progress
updated: 2026-10-05
---
# Dispatcher-class Frigate Import

## Goal

- Convert intact/damaged `CIS_TecnoDestroyer` ALOs and register the CIS frigate through [[Architecture/ALO_MODEL_IMPORT_GUIDE]].
- Preserve geometry, UVs, bones, hidden state, texture pixels and source hashes.
- Reference and exact mappings: [[GameDesign/Dispatcher Import]].

## Rules

- User name: Dispatcher-class Frigate; existing Munificent length `82.56805` project units.
- Hull/shields/regen `3,500/1,000/50`; cost/build/population/tech `3,400/33 s/3/2`.
- Explicit composition totals `20` weapons + shield/engine = `22` targets; supplied prose total differs from the listed sum.
- Existing Power to Weapons; XML speed `2.5`, duration `7 s`, cooldown `50 s` remain legacy provenance.
- No automated tests or Play Mode started for this task; concurrent shared-Editor activity is not this import's verification.

## Implementation

- [x] Audit source bones/materials/textures; convert intact/damaged models in separate Blender 3.6.23 processes.
- [x] Verify FBX geometry/UVs/hierarchy/hidden state; original hashes unchanged.
- [x] Import art and fit visual/gameplay/placement/dedicated wreck to Munificent length.
- [x] Configure 20 weapons, 22 health/fog targets, actual attachments, project offsets and existing Power to Weapons.
- [x] Register `Dispatcher = 108`, CIS roster, data/view/Addressables, icons, matchups and own placement.
- [x] Reload saved prefabs/data; verify root scales, bounds, IDs, references, weapon/audio profiles and tooltip sprite.
- [x] Inspect living/wreck blue/green palettes and uncropped transparent icon; import/compilation checks passed.
- [ ] Clean-skirmish acceptance: movement, firing arcs, Power to Weapons, system damage, placement, fog/selection and death/wreck behavior.
- [ ] Review provisional balance and inherited matchup categories.

## Important Values

- Source/FBX → Unity triangles: intact `6,411 → 6,407`, wreck `4,551 → 4,547`; exactly four zero-area faces removed per model, all other geometry/UVs retained.
- Intact/wreck bones `43/34`; Unity maximum bone/corner error `0.000001198 / 0.000001395` project units, no bone parent mismatches.
- Centered size `55.29128 × 32.17981 × 82.56805`; radius `42`; banked Y `−16.08990 .. +16.09021`.
- Provisional speed/yaw/turn acceleration/bank/limit `66/45/45/20°/10`; generic Frigate armor; existing shared weapon/audio profiles.
- Credits and packed assets: sibling `Tecno Destroyer-Converted/`; exact supplemental `bluethruster.dds` resolved from local RaW assets.

## Edge Cases

- Static damaged geometry is integrated; source death ALA and EaW animated shader/proxy effects are not converted.
- Shared Editor Console contained unrelated UI teardown exceptions and test warnings; no Dispatcher import/serialization error identified.
- Keep active until runtime and balance acceptance is complete; no unrun tests claimed.
