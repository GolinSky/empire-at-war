---
type: engineering-reference
system:
  - addressables
  - asset-compression
  - battle-cpu
  - battle-rendering
  - vfx
verified: 2026-11-06
verification: Inventoried both Optimization folders, backlog, acceptance queue
  and related fixes. Checked saved groups, loaders, pooled effects, render code,
  effective StandaloneOSX formats, live camera and available captures.
  Historical results retain their original targets.
tags:
  - optimization
---
# Empire at War optimization cookbook

## Rules

- Start with the observed symptom. Download/install size → content layout. First reinforcement/death hitch → preload and pool timing. Slow battle → frame intervals plus CPU/GPU attribution. Excess smoke/bolts → live effect census and fixed-view capture.
- Use [[Rules/UNITY_OPTIMIZATION_COOKBOOK]] for the comparison procedure. Use [[Artifacts/Audits/Optimization_2026-11-06/Optimization_Audit]] for findings at the November 6 revision. The plan links below retain their own implementation and acceptance status.

## Implementation

### Addressables, textures and audio

1. Place faction main/wreck/placement entries together in `EmpireUnitViews`, `RepublicUnitViews`, `SeparatistUnitViews` or `RebellionUnitViews`. Shared facilities/platforms belong in `View`. Keep shared icons, ship art, shaders, fonts and VFX in their lifetime owners. Preserve GUID, address, aliases and labels. Verify all four faction schemas remain `PackTogether`, CRC off under the loading plan. [[AssetPipeline/Guides/ADDRESSABLES_BUNDLE_GUIDE]].
2. Trace `ReinforcementData` preview references and `BattleAssetPreload` human/hosted selection. Start from a real address and inspect its transitive art. An entry in the default group after Add Selected needs an ownership review. Rebuild mappings only after verifying aliases.
3. Compare matching content builds. October 24 Windows bundles fell from 1,534,762,737 B after the ownership split → 1,025,898,126 B after texture/mesh screening → 989,838,944 B after the approved mesh pass. The final historical layout recorded 18 bundles and zero duplicated payload. Establish a fresh same-target baseline for new changes.
4. Pilot model textures by use. Linear RGB RMSE ≤2/255, maximum alpha error ≤8/255. Decoded normal mean ≤1.5°, p99 ≤6°. Compare all eight team colors and close/distant views. Restore sharp-alpha glow, exact-copy station hulls and other exceptions that fail the screen. Mesh gates are position ≤0.05% of bounds diagonal, UV ≤1/4096 and approved maximum normal error 3°. Evidence and per-asset exceptions live in `Tools/DesktopAssetCompression/Evidence/`.
5. Read back **effective** default/Standalone format, imported texture dimensions and GPU format. Audit CPU readers before changing Read/Write. For music, check menu/battle load lifetime, first play and crossfade. For short repeated effects, check DSP CPU, concurrency and latency. Measure RAM/VRAM separately from bundle bytes.

### Loading, battle CPU and UI

- `BattleAssetPreload` runs dependent batches before Running. `BattleAssetWarmup` starts deferred views/wrecks at Running with four in flight. Restore/simulation preloads those keys earlier. Preload exactly the consumer’s `(type, key)`. Trace a cold `ShipSpawnClearance` lookup separately from shot creation.
- `ShotPoolPrewarmer` shares storage by prefab. Roster and completion drain a 4 ms loading budget. Runtime prewarm caps at one creation/frame. Mark individual prefab/Awake spikes. Keep in-flight leases valid on mount release. Cancel them on battle teardown.
- Squadron contexts reuse by player/type. Validate health, radar, orders, registration, owner effects and event subscriptions across respawn. `ShipFactory.Create` constructs a child scope. The separate ship-pooling proposal still needs those reset contracts.
- Keep target snapshots and due events in stable order. Preserve stale-target rejection and impact timing. `SquadronOrderScheduler` caps 64 visits/32 updates. Ordinary capture sampling caps four visits/two area refreshes, with forced next-tick refresh on add/invalidate. For jobs, measure gather and completion alongside worker time. [[Rules/UNITY_JOBS_AND_THREADING_RULES]].
- Route top-level UI by update frequency and layer. Cache camera projection once per icon layer, preserve selection hit testing and fog. Compare layout/batching, allocations, and UI visibility with crowded fleets and the same camera. [[Rules/UI_UX_GUIDELINES]].

### Rendering and effects

- Decor uses 1,000-unit cells, transformed source/LOD bounds, instanced batches of up to 511 and per-size distance limits. Field rocks keep gameplay objects, colliders and fog. Verify silhouette, edge crossings, nearest/distant LOD and asteroid material. [[Rules/BATTLE_PERFORMANCE_GUIDELINES]].
- Nebula uses a 128-sample bake for the normal camera and a live volume beyond 1° of the bake axis. Check both paths, the visibility-off startup path and runtime quality switches. Hazard tiers use 2/2/3/5/5/5 sheets. Preserve the SDF boundary and both noise textures. For the separate FPS recovery scenarios, the historical targets are PC Cinematic ≥190 FPS, M2 Performant A ≥90/B ≥45 FPS and ≤3 M triangles in a typical view.
- At Performant, check the effective camera volume for quarter-resolution bloom, three iterations, HQ off and motion blur zero. Mac first-run 77%/FSR 1 and PC 100%/Bilinear are defaults. Use **100%** for the M2 plan’s 2560×1440 budget experiment. It targets ≥60 FPS, nebula ≤1 ms and visible hazards ≤1.5 ms in its specified views.
- Compare muzzle, bolt, trail, sustained beam, hardpoint smoke and death explosion separately. Log material identity, active renderers, emission, off-camera particles and GPU submission. Keep particle shadow casting off. Mesh casters have separate rules. Preserve shader blending, vertex tint, UV wrap, fades and draw order.
- Exercise 1/2/4/8 bolts, faction and override colors, shield/armor hits, misses, interception, owner death, pool reuse, pause and scene exit. `LaserProjectile` now has five heavy-beam systems. The saved `ProtonBeamView` still has a 50/s smoke override. Scope a new reduction to the actual effect that owns the cost.
- Benchmark fixed camera/seed/preset/scale, 1× then accelerated combat, at least three matched firing windows of ≥10 s per variant. Capture p95/p99, maximum and hitch rows as well as median. Check CPU, render submission and GPU counters separately. Render the composited Game view for HUD/decor acceptance.

## Files

### Active plans

- [[TODOs/Optimization/Optimization_Audit_Fix_Plan|November 6 fix order]] — current priorities and verification gates across the existing plans.

- [[TODOs/Optimization/Addressables_Bundle_Remediation_Plan|Bundle remediation]] · [[TODOs/Optimization/Addressables_Bundle_Remediation_Plan - Research|Research]] — In progress. Faction split, alias repair and preview references landed. Packed-player placement, mixer and early-use checks remain.
- [[TODOs/Optimization/Desktop_Asset_Compression_Plan|Desktop compression]] — In progress. Retained screened texture and mesh batches. Compare closest-zoom art, clip playback and working set on both desktops.
- [[TODOs/Optimization/Asset_Size_Followup_Plan|Asset size follow-up]] — To do. Owns format exceptions, unused alpha, UI, audio, CPU-readable meshes and whole-Player size.
- [[TODOs/Optimization/Loading_Freeze_Plan|Loading freezes]] · [[TODOs/Optimization/Loading_Freeze_Plan - Research|Research]] — In progress. Concurrent preload batches, additive load, CRC policy and staged warmup. Measure first use and teardown in a packed Player.
- [[TODOs/Optimization/Battle_CPU_Performance_Plan|Battle CPU]] · [[TODOs/Optimization/Battle_CPU_Performance_Plan - Research|Research]] — In progress. Owns targeting, due events, navigation, Burst and matched workload attribution.
- [[TODOs/Optimization/Late_Battle_CPU_Fix_Plan|Late-battle CPU fixes]] — In progress. Bounded schedulers, projectile prewarm, origin snapshots, icons and allocations. Historical Mac causes and matched runs remain open.
- [[TODOs/Optimization/Ship_Pooling_Plan|Ship pooling]] — To do. Full ship scopes still need a lifecycle/reset decision. Historical spawn spikes require confirmation on the current factory.
- [[TODOs/Optimization/UI_Canvas_Routing_Optimization_Plan|UI canvas routing]] · [[TODOs/Optimization/UI_Canvas_Routing_Optimization_Plan - Research|Research]] — In progress. Screen-space routing and capture-site panel implemented. Interaction, canvas rebuild and Player acceptance remain.
- [[TODOs/Optimization/Battle_FPS_Recovery_Plan|Battle FPS recovery]] · [[TODOs/Optimization/Battle_FPS_Recovery_Plan - Research|Research]] — In progress. Owns decor/field LOD, particles and projectile experiments. Check matched timings and combat visuals.
- [[TODOs/Optimization/Mac_Nebula_Hazard_GPU_Plan|Mac nebula and hazards]] — In progress. Owns fixed-pose hazard/nebula budgets, per-draw attribution and visual acceptance.

### Archived plans

- [[Done/Optimization/Battle_Attack_Optimization_Plan|Attack and projectile reuse]] · [[Done/Optimization/Battle_Attack_Optimization_Plan - Research|Research]] — Merged into Battle CPU. Preserve target/due ordering, shot timing and pool drain.
- [[Done/Optimization/Battle_CPU_Fixes_Plan|Battle CPU fixes]] · [[Done/Optimization/Battle_CPU_Fixes_Plan - Research|Research]] — Merged into Battle CPU. Carries targeting, steering, audio and deferred acceptance.
- [[Done/Optimization/Burst_Jobs_Optimization_Plan|Burst jobs]] · [[Done/Optimization/Burst_Jobs_Optimization_Plan - Research|Research]] — Merged into Battle CPU. Preserve parity and job ownership. Verify kernel and whole-frame changes on each target.
- [[Done/Optimization/Battle_Rendering_Performance_Audit|Rendering audit]] · [[Done/Optimization/Battle_Rendering_Performance_Audit - Research|Research]] — Merged into FPS recovery. Decor enabled/off/restored captures established the original rendering issue.
- [[Done/Optimization/Battle_Particle_Rendering_Optimization_Plan|Battle particles]] · [[Done/Optimization/Battle_Particle_Rendering_Optimization_Plan - Research|Research]] — Merged into FPS recovery. Indicator consolidation and historical beam smoke reduction. Submission and combat appearance still need isolation.
- [[Done/Optimization/Projectile_VFX_PC_Optimization_Plan|Projectile VFX on PC]] · [[Done/Optimization/Projectile_VFX_PC_Optimization_Plan - Research|Research]] — Merged into FPS recovery. Material equivalence, fades, texture copies and the weapon variation matrix remain scoped experiments.
- [[Done/Optimization/Mac_GPU_Frame_Budget_Plan|Mac GPU frame budget]] — Implementation closed. Mac defaults, AA, hazard mesh and covered startup frame. Acceptance pending. Broad shader warmup was removed.
- [[Done/Optimization/Squadron_Pooling_Plan|Squadron pooling]] — Implementation closed. Reuse by player and type. User acceptance pending. Ship pooling has its own scope.

### Related work outside Optimization

- Covered startup and variant warmup → [[Done/Refactoring/Battle_Startup_Sequence_Plan]] · [[Done/Bugs/Shader_Warmup_Keyword_Errors]]. Particle/material ownership on exit → [[Done/Bugs/Scene_Teardown_And_Material_Leaks]].
- Hazards, capture and rendering dependencies → [[Done/Features/Space_Hazards_Shape_Rework_Plan]] · [[Done/Refactoring/Capture_Reinforcement_Tally_Dedup]] · [[Done/Refactoring/Fog_Disabled_Stand_Ins]] · [[Done/Refactoring/ShipLit_Autodesk_Material_Conversion_Plan]].
- Status and user-only checks → [[TODOs/TODOs]] · [[TODOs/User_Acceptance_Queue]].
