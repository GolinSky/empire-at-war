---
category: Features
status: done
completed: 2026-10-12
---
# MC30c Frigate Import

## Goal

- Import and register the Workshop MC30c using [[GameDesign/Proposals/Rebel units]] balance and [[AssetPipeline/Guides/ALO_MODEL_IMPORT_GUIDE]].

## Implementation

- [x] Audit source, reconstruct materials, convert and verify Blender/FBX geometry and sockets.
- [x] Create own art, gameplay, data, 16 targetable hardpoints, Torpedo Salvo and approved burst cluster bombs.
- [x] Save placement, wreck, icon, team colors and every ship registration.
- [x] Verify persisted assets, unchanged inputs, texture copies, previews, imports and compilation.
- [x] Record evidence and package editable conversion in [[AssetPipeline/Imports/MC30c Frigate Import]].

## Decision

- Completed 2026-10-12: Rebellion ship `307`, 14 weapons/16 targets, requested stats/economy, approved burst cluster bombs and torpedo-only Salvo.
- Verification: 1,851 saved-asset checks, 14 source hashes, nine lossless textures, icon and eight live/wreck palettes passed; Unity compilation successful.

## Edge Cases

- Runtime combat/placement/ability/death acceptance and balance tuning are deferred; no automated tests or Play Mode run.
- Initial unrelated `RapidDeploymentAbility.cs` compilation failure is resolved; current compilation successful.
- Hangar fix, 2026-10-12: removed leftover donor hangar object that Zenject injected without `HangarModel` because MC30c has zero bays. Builder/verifier and prefab regression source updated; saved/reimported asset passed 1,838 checks. No automated tests or Play Mode run.
- Missing source death model → live-hull project wreck. Cluster bombs use the user-approved existing burst-projectile system.
