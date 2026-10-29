---
category: Optimization
status: in-progress
created: 2026-09-30
updated: 2026-10-11
---
# Battle particle rendering evidence — 2026-09-30

## Important Values
- Capture: `Logs/RenderAudit/20260930T165740445Z-capture`; 19:57 Kyiv / 16:57 UTC.
- Original result: partial; Profiler summary failed, Frame Debugger and Render Graph completed; camera-selector value unreadable.
- Original files retained. Recovery: `output/RenderAuditReview_20260930/profiler-summary-recovered.json`.
- Recovered Profiler: two frames, indices 0/1 after loading; recovery timestamps are export time.
- Profiler draw calls: 451/457; SetPass: 239/247; triangles: 1,759,591/1,788,255; BRG draws: 146/148.
- Separate end-of-render UnityStats: 429/453 draws; 225/245 SetPass; values describe different frame identities.

## Decision
- User observation: stars spawn at battle start with ~200 FPS; mid-battle performance falls to ~12 FPS (~5 → 83.3 ms/frame).
- Inference: prioritize work that grows with combat: ship/navigation ticks, active effects, smoke/fire overlap and effect lifecycle. Stars alone are not established as the cause.
- Camera, quality, resolution and background population must be matched before treating the FPS difference as a controlled comparison.
- Chosen: investigate CPU ship/navigation work and accumulating combat effects first; defer star reduction.
- Why: `Battle.Ship.Tick` inclusive 55.45/46.95 ms, self 35.69/27.00 ms, 46 calls/frame.
- `NavigationFloodJob (Burst)`: 8/7 calls, 16.84/15.06 ms main-thread self time.
- `ShipPathGrid.EnsureFlood` runs block-grid and flood jobs through `Schedule(...).Complete()`; synchronous work verified in live source.
- Render thread: frame 1 `Semaphore.WaitForSignal` 64.63 ms of 70.48 ms; waiting is not GPU shading time.
- Frame durations: 165.66/70.48 ms; frame 0 also contains 76.36 ms unattributed root self time and JIT activity.
- Whole-frame GPU timing is unavailable. Two frames support investigation, not sustained FPS or percentile claims.

## Implementation

### Export repair and verification
- Replayed the original recording before editing → `ArgumentNullException` at `CaptureCpuHotspots`, nullable sample-name dictionary key.
- Group CPU samples by `GetSampleMarkerId`; emit `marker_id` and retain original nullable `marker`.
- Regression tests replay both captured frames and verify frame/thread exports, marker identity, call counts and inclusive timing.
- Filtered EditMode run: 2 passed / 0 failed / 0 skipped; main-menu scene checked clean beforehand.
- Unity compilation completed without errors. Existing unrelated hardpoint/audio MissingReferenceExceptions predate this task.
- Tests preserve and restore the Profiler buffer; original audit files remain unchanged.
- Run: `unity command run_tests --mode editor --filter RenderAuditProfilerToolTests --filter_type testName --include_explicit true --async_tests true --json`; check clean scenes first, poll `test_status`.

### Particle scope
- Read-only Unity inventory: 65 prefab assets under `Assets/Prefabs/Vfx` and `Assets/Prefabs/Models`.
- 111 particle-system configurations, including nested copies; 105 Billboard / 4 Stretch / 2 Mesh.
- 21 shared material asset paths; 10 legacy-shader materials referenced by 75 configurations.
- 19 engine configurations share `Assets/Art/Materials/Vfx/Engines.mat`.
- PPFX default/explosion09/smoke07 materials are shared by 21/20/20 configurations; material cloning is not demonstrated.
- 68 renderers request shadow casting, but this does not establish an active shader ShadowCaster pass or measured shadow cost.
- One configuration enables particle lights; no collision or ParticleSystem trails enabled in this scope.
- `active_self` records prefab-local activation, not runtime visibility; capacity and stored emission rates are not live population.

### Measured render evidence
- Frame Debugger: 287/289 events; 139/141 DynamicGeometry events, 35 HybridBatch events.
- Particle/ion shaders: 129/131 events; first frame has 90 different-material breaks and 25 SRP-incompatible breaks.
- Frame Debugger events may contain multiple draws; clear/compute events are not draw calls.
- Rendering Debugger transparent pass: 1.3341 ms CPU / 1.804288 ms GPU; opaque: 0.1507 / 0.176384 ms.
- SSAO GPU: 0.069888 ms; Bloom: 0.044032 ms; these snapshots do not support prioritizing post-processing over transparencies.
- ParticleSystem.Update main-thread self: 0.4570/0.4576 ms.
- Exported particle geometry self across workers: 2.3609/2.3408 ms; largest worker entry 1.7830/1.7894 ms.
- Worker self totals overlap in wall time; the largest geometry entry is not attributed to a particular system by this export.

### Priority assets
| Asset | Verified configuration / capture | Interpretation |
| --- | --- | --- |
| `SkirmishVfx.prefab/StarVfx` | 5,400/s, 15 s lifetime, cap 54,000, Billboard; captured 216,000 vertices / 324,000 indices / 4 draws | Capture contains 54,000 quads; present during reported fast start; defer until cost isolation identifies relevance |
| `ExplosionVfx.prefab/ppfxPipelineFire` | 100/s, lifetime 1–6 s, cap 1,000, AlwaysSimulate; named captured draws contain 348–359 quads | Persistent smoke accumulates; reduce measured overlap/emission |
| `LaserProjectile.prefab`, `ProtonBeamView.prefab` | Each embeds five hit-effect systems, including PPFX smoke/fire | Beam impact uses same costly smoke family |
| `Projectile.prefab`, `DualProjectile.prefab` | Stretch; capture: one draw, four vertices per named bolt; URP Unlit / legacy Additive (Soft) | Small geometry, repeated submissions; ordinary particle instancing toggle is insufficient |
| `ImpactEffects.prefab` | Four shared systems; stored rates 10/s but emission disabled | Runtime `Emit` drives impacts; no passive 10/s emission |
| `Engines.mat` users | Billboard, rateOverDistance 15/m, lifetime 1 s; shared material | Motion-driven demand; measure before reducing |
| `SkirmishVolumetricNebulaClouds.prefab` | 48 mesh particles, one instanced draw, 80 ray samples; particle sizes 2,600–3,800 | Submission is already instanced; pixel/raymarch cost remains a candidate |

### Runtime overrides and lifecycle
- `BoltShot.Play` disables emission, sets loop=false, emits one particle; lifetime = travel time + 1 s; profile size/color applied through particle data.
- `ImpactEffectView`: shield flash + ring; armor flash + 8 sparks; lifetimes 0.16/0.35/0.18/0.2–0.45 s.
- `BeamShot`: armor hit calls `hitEffect.Play(true)`; `StopBeam` stops emission and lets existing particles drain.
- Projectile/beam code uses particle startColor or LineRenderer vertex color; targeted search found no material-instantiation calls in weapon views.
- Transparent sorting can interleave different shared materials; 90 breaks do not mean 90 unique materials.
- Keep texture-sheet animation, additive/premultiplied/alpha blends and soft-particle behavior explicit during migration.
- Unity mesh-particle instancing guidance: https://docs.unity3d.com/Manual/PartSysInstancing.html.
- The generic Built-in instancing shader examples must not be copied unchanged into URP.

### Jobs investigation — 2026-09-30

- Scope: live navigation, ship combat states, target/due attack batching; full raw sample ancestry from the original two-frame recording.
- Unity 6000.4.7f1; Burst 1.8.29, Collections 6.4.0, Mathematics 1.3.3. Profiler names confirm Burst execution.
- Diagnostic script/data: `output/RenderAuditReview_20260930/{InspectJobTimeline.cs,job-timeline.json}`; Profiler buffer restored after inspection.
- No gameplay code, scenes or VFX changed; no new automated test run.

| Raw timeline measurement | Frame 0 | Frame 1 |
| --- | ---: | ---: |
| All ship tick calls | 46 | 46 |
| All ship ticks inclusive | 55.45 ms | 46.95 ms |
| Eight ticks containing navigation job samples | 53.38 ms | 44.90 ms |
| Main-thread Complete calls inside ship ticks | 34 | 31 |
| Those Complete scopes inclusive | 19.71 ms | 19.90 ms |
| Flood jobs: all threads | 8 | 8 |
| Flood jobs executing on main thread | 8 | 7 |
| Main-thread flood execution | 16.84 ms | 15.06 ms |
| Worker flood execution | none | 1 job / 2.09 ms |
| Route extraction jobs: all threads | 18 | 15 |
| Slowest ship tick inclusive / self | 17.37 / 14.66 ms | 17.82 / 15.01 ms |
| GC.Alloc calls inside all ship ticks | 1,352 | 750 |

- Complete, WaitForJobGroupID and job timings are nested; do not add them to ship-tick inclusive time.
- Eight navigation-containing tick invocations account for ~96% of ship-tick inclusive time in both frames; ship identity/state is not recorded.
- Flood ancestry: `Ship.Tick → Battle.Ship.Tick → JobHandle.Complete → WaitForJobGroupID → NavigationFloodJob (Burst)`.
- Block-grid jobs do run on workers in both frames. Main-thread work stealing is specifically confirmed for the flood jobs.
- Longest tick in each frame contains one grid-block sample, one flood sample and five main-thread route-extraction samples.
- Ship self time remains 35.69/27.00 ms outside instrumented child markers; candidate resolution, route construction/validation and synchronization are not individually timed.
- GC.Alloc call counts are allocation frequency, not allocated bytes or evidence that GC collection explains the slowdown.

### Source findings
1. **Immediate completion serializes requests.** `ShipPathGrid.cs:83,185,211` schedules and immediately completes route, block-grid and flood jobs.
   - No retained JobHandle or cross-request dependency graph; callers explicitly require same-frame results.
   - `NavigationFloodJob` and `NavigationRouteJob` are `IJob`; only `NavigationGridBlockJob` is `IJobParallelFor`.
   - Job scheduling alone does not remove main-thread stalls. Unity documents delayed completion and work-stealing markers: https://docs.unity3d.com/Manual/job-system-creating-jobs.html and https://docs.unity3d.com/Manual/profiler-markers.html.
2. **Each Plan owns a short-lived grid.** `ShipNavigationService.cs:128` creates/disposes `ShipPathGrid`; `_isFlooded` only reuses results within that Plan's destination candidates.
   - Flood execution allocates nine NativeArray/NativeList containers including obstacle data and open/closed sets; later Plan calls rebuild them.
   - Direct clear routes bypass the flood, so not every Plan necessarily executes navigation jobs.
3. **Flood traverses all reachable cells.** `NavigationFloodJob.cs:50` processes its heap until empty, without a goal-specific stop.
   - The capped square-map grid is roughly 161×161 ≈26,000 nodes; actual capture grid dimensions were not recorded.
   - This supports multiple destination candidates and nearest-reachable fallback; replacing it with A* requires preserving those behaviors.
4. **Managed candidate work amplifies cost.** `TryPlanNear` considers center + 16 rings × 8 candidates = 129/pass; a failed pass may trigger another pass around the nearest reachable position.
   - Success exits at the next ring boundary, so it still evaluates the rest of the first successful ring.
   - `TryResolveDestination` tries 24 edge positions per blocking contact; each position checks contacts again.
   - `ShipRoutePlanner.Build` builds/validates curved, turned and polyline alternatives; `IsRouteClear` checks route samples against contacts on the main thread.
   - `ShipBezierRoute` allocates segment/arc/sample arrays (24 arc intervals, 12 debug intervals per segment). Full contacts include idle registered ships.
   - These are verified cost multipliers, not individually attributed milliseconds in the capture.
5. **Stopped pursuit can retry every tick.** `ShipEngagement.cs:29` suppresses replanning only when IsMoving AND target drift is below threshold.
   - Hunt/Guard call Pursue each tick outside firing range; if planning leaves the ship stationary, the same target can trigger planning again next tick.
   - `AttackTargetState.UpdateFormationMove` has a similar stationary/out-of-range closing path; non-formation attack already has destination guards.
   - Source establishes the retry paths; the recording does not establish which state triggered its eight requests.
6. **Attack Jobs are not the largest measured scopes here.** Target batch total 1.48/1.33 ms; target Complete 0.03/0.02 ms.
   - Due batch total 0.54/2.34 ms; due Apply 0.51/2.31 ms; due Complete ~0.03/~0.02 ms.
   - These paths already batch records and reuse capacity; their job synchronization is much cheaper in this recording than navigation completion.

### Recommended investigation order
1. Instrument Plan reason/count, requesting order/state, destination-candidate count, grid size, contact count and managed route construction/validation.
2. Reproduce stopped/out-of-range Hunt/Guard and congested formation pursuit; suppress redundant failed retries until target/obstacles/order change or an explicit retry interval elapses.
3. Reduce managed duplicate candidate/route work and reuse native buffers; cache only with correct origin, clearance, obstacle and reservation validity.
4. If needed, stage independent navigation requests: schedule block→flood work with dependencies, finish later and apply in stable order.
   - Retain same-frame responsiveness if achievable; otherwise document latency explicitly.
   - Shared service scratch lists and sequential destination reservations cannot be accessed concurrently without a request snapshot and deterministic commit design.
5. Evaluate goal-bounded search only after measuring candidate/fallback demand; compare against the existing full-flood behavior.
6. Capture matched start/mid-battle and Development Player runs before claiming a complete explanation of ~200→12 FPS.

## Files
- Capture events: `Logs/RenderAudit/20260930T165740445Z-capture/frame-debugger-frame-{01,02}.json`.
- Snapshot settings/timings: `Logs/RenderAudit/20260930T165740445Z-capture/rendering-debugger.json`.
- Full prefab/material inventory and replay summary: `output/RenderAuditReview_20260930/`.
- Plan: [[Done/Optimization/Battle_Particle_Rendering_Optimization_Plan]].

### Rendering investigation — 2026-10-11
- Source: `Logs/RenderAudit/20261011T115023720Z-capture/`; Frame Debugger frame 59290, 949/949 events, no truncation; event/data indices match throughout.
- Frame Debugger events ≠ draws. Ignore Clear* event draw/shader fields; clear records retain previous shader information.
- Transparent: 843 events / 1,234 reported draws = 1,078 non-Canvas + 156 world UI; opaque 279 draws; main-light shadows 354 draws.
- Transparent shaders: URP Particles/Unlit 424 events / 661 draws; Ion Glow 147/219; URP Unlit 81/149. Shader names do not establish renderer type.
- Particles/Unlit texture groups: smokeSheet03 247 draws; BlasterBolt 183; BlasterBolt_Tiled 111; TIEInterceptor engine texture 51; TIEBrute engine texture 20. Includes imported glow meshes.
- Ion Glow: 143 events / 215 draws use brightness 6 + white material color, matching shared `MuzzleFlash.mat`; 4 events / 4 draws use brightness 4 + blue color, matching `IonArcGlow.mat`. Material-identity attribution is inferred from signatures; export lacks material IDs.
- `MuzzleFlash.mat` is referenced by 12 projectile/beam prefabs; `MuzzleFlashView` uses LineRenderer vertex color and 0.1 s stored duration, without material cloning.
- Different-material breaks: 363 events; Particles/Unlit 217, Ion Glow 95, MPUI 28, legacy alpha 10, legacy premultiply 11, legacy additive 1, Ion Field 1.
- SRP-incompatible breaks: 158; Particles/Unlit 95, Ion Glow 47, MPUI 10, other shaders 6. Particle/Line/Canvas renderer restrictions matter; `IonGlow.shader` already uses UnityPerMaterial.
- World UI: 39 repeated sequences of 2 MPUI + IconShadow + UI/Default = 156 draws / 624 vertices. Icon textures: TIEBomber 18, TIEFighter 11, TIEInterceptor 8, Belbullab22 2.
- `SquadronIconComponent` controls world Canvas per squadron, checks friendly/vision visibility, and updates billboard transform every LateTick; source/prefab structure matches captured sequences.
- Screen UI: 41 Canvas draws; world UI is already included in the transparent total.
- Shadows: 347 Ship Lit + 7 Ship Wreck draws, 3,208,522 reported vertices / 5,878,500 indices; 2048×1024 atlas. No combat-particle ShadowCaster draw is present.
- Current serialized HighFidelity: shadow distance 1,000, 2 cascades, main atlas setting 2,048; active-at-capture quality not explicitly recorded.
- Rendering Debugger frame 59289: CPU / CPUInline / GPU column order verified in installed Core package. Transparency 2.0818 / 0.0013 / 1.754624 ms; opaque 0.3264 / 0.0041 / 0.450304; shadows 0.3538 / 0.0149 / 0.218112; bloom 0.0515 / 0.0336 / 0.046336; SSAO 0.0177 / 0.0096 / 0.054784.
- GPU whole-frame and MotionBlur scope values are zero/unusable; do not interpret as zero cost. Timings are adjacent snapshots, not exact Frame Debugger event timings.
- Post passes: bloom 16 DrawProcedural events (1 prefilter + 10 downsample + 5 upsample), SSAO 3, high-quality camera motion blur 1. Filter Clear* records to avoid false extra shader passes.
- Render target 2560×1440; bloom starts 1280×720 then reaches 40×22; SSAO occlusion/horizontal blur 1280×720, final composite 2560×1440.
- Profiler frames 122/123: 1,802/1,806 draws, 726/739 SetPass, 3,335,865/3,374,289 triangles; main-thread frame 858.1139/1,075.06763 ms; whole-frame GPU time unavailable.
- Radar scans: 1,286/1,313 calls; inclusive 619.7048/629.7695 ms, self 589.5341/601.1728 ms. FixedBehaviourUpdate 66/67 calls indicates fixed-step catch-up; capture overhead/time scale must be isolated before claiming production FPS.
- ParticleSystem.Update inclusive 0.9509/0.9202 ms; UGUI.Rendering.UpdateBatches inclusive 2.6495/4.4471 ms. Do not add nested self/inclusive timing.
- Priority: isolate radar/fixed-step CPU amplification; rendering experiments on muzzle/bolt/trail submission and squadron indicator batching; measure smoke overlap before tuning; shadow LOD/distance second; bloom/SSAO lower priority at measured cost.
- Verification: read-only capture/source/serialized-asset analysis; no Unity tests, Play Mode changes, code or asset mutations.

### Later session supersedes CPU priority — 2026-10-11
- Rendering investigation above uses the 14:50 audit. User pasted the rendering agent's matching analysis; preserve its source-verified findings and caveats.
- 15:25 battle capture reports radar 8.581 ms/frame after source changed to centralized RadarScanService. The old 620 ms radar finding is not a current diagnosis.
- 15:25–15:28 Capture All: profiler/Render Graph completed; rendering camera selector partial; Frame Debugger timed out at event 799 with zero saved frames.
- 15:31 fresh profiler: median CPU 96.584 ms, GPU 2.965 ms; target preparation is largest steady exported main-thread hotspot.
- Current capture manifest, CPU comparison and uncertainty: [[TODOs/Optimization/Battle_CPU_Performance_Plan - Research]].
- Retain rendering priority: muzzle/bolt/smoke submission and squadron indicators → measured overdraw → shadows → motion blur; bloom/SSAO lower priority in available old GPU snapshot.

### Targeted rendering changes — 2026-10-11
- User direction: use the latest saved full battle capture; stop broad idle-session profiling. New startup and partial isolation captures are diagnostic only.
- Runtime inventory: `output/ParticleRendering_20261011/runtime-inventory.json`; ordinary muzzle renderers share material ID 63632, `Assets/Art/Materials/Vfx/MuzzleFlash.mat`, Ion Glow. No clone fix justified by this snapshot.
- Live `DualProjectile.mat`, `ShipExplosionSmoke.mat`, `Engines.mat`, `Vulture_MuzzleFlashHelper.mat`: already URP Particles/Unlit. Historical migration recommendations are obsolete for these assets.
- All 18 squadron prefabs: merge black background + colored rounded frame into one standard Image with shared SquadronFrame material; rebind `SquadronIconComponent.frameImage` through SerializedObject.
- 10 prefabs have identical unpacked icon/silhouette sprites → shared SquadronIcon shader composites both samples in one Image. Other 8 retain separate silhouette artwork and foreground Image.
- Frame constants: 72×72 canvas, 3 px corner radius, 1.5 px border, black background alpha 0.5. Icon: 58.5/54 scale, normalized vertical offset 1.5/54; normalized offset preserves transparent sprite padding and zoom scaling.
- Existing icon/color/component references remain; disabled Background/Icon graphics retain their objects. Component visibility, ownership/selection updates, sorting, screen scaling and hit-test logic were not edited.
- 18-prefab fixture: 72 → 44 enabled graphics. Historical 39-indicator composition: 18 TIEBomber + 11 TIEFighter + 8 TIEInterceptor ×3; 2 Belbullab22 ×2 → projected 115 draws, previously 156. No new combat-frame measurement.
- Before/after ownership colors and alpha 0.2/0.35 rendered at 720×720. Mean absolute RGB difference 0.015/255; max 16/255; 350 pixels >2/255. No missing/displaced icons observed; remaining differences are tiny sampling/edge changes.
- Beam prefabs only: `LaserProjectile` and `ProtonBeamView` override nested `HitEffect/ppfxPipelineFire` emission 100/s → 50/s. `ExplosionVfx` source remains 100/s; lifetime 1–6 s, size and other systems unchanged.
- Seed 12345, 6 s simulation: rate 100 → 362 live particles; rate 50 → 185. Both prefabs drain to zero after StopEmitting + 6.1 s simulation; stopped state verified. Manual Simulate changes playback state, so stop again after advancement to inspect IsAlive.
- Targeted validation: all 18 saved indicator references/counts/shared materials passed; both new shaders have no compiler messages. `SerializedReferenceTests`: 2 passed, 0 failed, clean MainMenuScene before async run.
- Assets saved through PrefabUtility/AssetDatabase; generated screenshot folder removed from Assets through AssetDatabase.DeleteAsset. Preview/validation scripts and JSON live outside Assets in `output/ParticleRendering_20261011`.
- Earlier broad isolation was interrupted; diagnostic toggles/time scale restored. Its drifting draw counts and startup Profiler are excluded from acceptance. Unrelated RadarScanService teardown errors and transient concurrent ShipMoveComponent compile errors were observed; latest console interval had no new errors.
- Remaining: combat density/readability and pooled reuse/death/scene exit, muzzle/bolt/smoke submission isolation, matched Player frame timing. No claimed sustained FPS gain.
