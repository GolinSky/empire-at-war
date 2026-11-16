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
  - Active
  - optimization
---
# Empire at War optimization cookbook

## Rules

- Start with the observed symptom. Download/install size → content layout. First reinforcement/death hitch → preload and pool timing. Slow battle → frame intervals plus CPU/GPU attribution. Excess smoke/bolts → live effect census and fixed-view capture.
- Use [[Rules/UNITY_OPTIMIZATION_COOKBOOK]] for the comparison procedure. Use [[Artifacts/Audits/Optimization_2026-11-06/Optimization_Audit]] for findings at the November 6 revision. Live code and saved assets establish current behavior. Use the backlog for status and the dated records for evidence.

## Implementation

### Addressables, textures and audio

1. Place faction main/wreck/placement entries together in `EmpireUnitViews`, `RepublicUnitViews`, `SeparatistUnitViews` or `RebellionUnitViews`. Shared facilities/platforms belong in `View`. Keep shared icons, ship art, shaders, fonts and VFX in their lifetime owners. Preserve GUID, address, aliases and labels. Verify all four faction schemas remain `PackTogether`, CRC off under the loading plan. [[AssetPipeline/Guides/ADDRESSABLES_BUNDLE_GUIDE]].
2. Trace `ReinforcementData` preview references and `BattleAssetPreload` human/hosted selection. Start from a real address and inspect its transitive art. An entry in the default group after Add Selected needs an ownership review. Rebuild mappings only after verifying aliases.
3. Compare matching content builds. October 24 Windows bundles fell from 1,534,762,737 B after the ownership split → 1,025,898,126 B after texture/mesh screening → 989,838,944 B after the approved mesh pass. The final historical layout recorded 18 bundles and zero duplicated payload. Establish a fresh same-target baseline for new changes.
4. Pilot model textures by use. Linear RGB RMSE ≤2/255, maximum alpha error ≤8/255. Decoded normal mean ≤1.5°, p99 ≤6°. Compare the active team palette and close/distant views. Restore sharp-alpha glow, exact-copy station hulls and other exceptions that fail the screen. Mesh gates are position ≤0.05% of bounds diagonal, UV ≤1/4096 and approved maximum normal error 3°. Evidence and per-asset exceptions live in `Tools/DesktopAssetCompression/Evidence/`.
5. Read back **effective** default/Standalone format, imported texture dimensions and GPU format. Audit CPU readers before changing Read/Write. For music, check menu/battle load lifetime, first play and crossfade. For short repeated effects, check DSP CPU, concurrency and latency. Measure RAM/VRAM separately from bundle bytes.

### Loading, battle CPU and UI

- `BattleAssetPreload` runs dependent batches before Running. `BattleAssetWarmup` starts deferred views/wrecks at Running with four in flight. Restore/simulation preloads those keys earlier. Preload exactly the consumer’s `(type, key)`. Trace a cold `ShipSpawnClearance` lookup separately from shot creation.
- `ShotEffectCache` shares storage by prefab. `ShotPoolPrewarmer` drains one loading queue with a 4 ms budget and caps runtime growth at one creation/frame. Mark individual prefab/Awake spikes. Keep in-flight leases valid on mount release. Cancel them on battle teardown.
- Profile slow fleets with matched seed, camera, render settings, speed, population and orders. Separate sustained frame cost from isolated stalls. Capture Editor and Development Player independently, keep profiler exports and command traffic outside timed runs, and use native stacks to identify CPU, asset-loading, GC and GPU waits. Use the active [[TODOs/Optimization/Battle_CPU_Performance_Plan|Battle CPU plan]] to track findings.
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

- Active optimization work → [[TODOs/TODOs#Active Tasks|Backlog]] and [[TODOs/User_Acceptance_Queue]].
- Battle CPU history → [[Done/Optimization/Battle_CPU_Fixes_Plan]] (initial implementation), [[Done/Optimization/Late_Battle_CPU_Fix_Plan]] (late-fleet fixes and M2/Windows evidence), and [[TODOs/Optimization/Battle_CPU_Performance_Plan - Research]] (detailed attribution).
- Dated evidence → [[Artifacts/Audits/Optimization_2026-11-06/Optimization_Audit]] and [November 7 packed results and captures](../../Docs/Audits/2026-11-07-Optimization-Fixes/README.md). Follow-up → [[TODOs/Optimization/Optimization_Audit_Fix_Plan]].

### Related work outside Optimization

- Covered startup and variant warmup → [[Done/Refactoring/Battle_Startup_Sequence_Plan]] · [[Done/Bugs/Shader_Warmup_Keyword_Errors]]. Particle/material ownership on exit → [[Done/Bugs/Scene_Teardown_And_Material_Leaks]].
- Hazards, capture and rendering dependencies → [[Done/Features/Space_Hazards_Shape_Rework_Plan]] · [[Done/Refactoring/Capture_Reinforcement_Tally_Dedup]] · [[Done/Refactoring/Fog_Disabled_Stand_Ins]] · [[Done/Refactoring/ShipLit_Autodesk_Material_Conversion_Plan]].
- Status and user-only checks → [[TODOs/TODOs]] · [[TODOs/User_Acceptance_Queue]].


### Safe desktop compression audit · 2026-11-16
- Unity's compression checkbox can coexist with an explicit uncompressed default format. Verify effective Standalone output and target format validity; Windows/macOS share Standalone overrides. Chosen DXT1/DXT5/BC7 formats validated in Unity 6000.4.7f1; actual Mac rendering remains a separate gate.
- Preserve failed quality candidates even when a global compression inventory flags them. Sharp UI can fail BC7; incompatible block dimensions require a separate resizing/padding decision. Compression does not authorize changing Read/Write or reducing already-compressed assets again.
- Unused discovery must include ProjectSettings and include-in-build Sprite Atlases, plus source/tool consumers. No scene/Addressables path does not prove safe deletion; mining-facility DAE and HeavyDreadnought Blend remain authoring inputs.
- Evidence → `Tools/DesktopAssetCompression/Evidence/SafeDesktop20261116.json`; full reports `output/DesktopAssetCompression/Audit20261116/`. Owner → [[TODOs/Optimization/Desktop_Asset_Compression_Plan]].
