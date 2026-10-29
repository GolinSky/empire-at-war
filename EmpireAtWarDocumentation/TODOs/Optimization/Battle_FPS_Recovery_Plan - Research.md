---
category: Optimization
status: in-progress
created: 2026-10-29
---
# Battle FPS Recovery Plan — Research

## Goal

- Preserve the 2026-10-29 PC Play Mode capture and its comparison with the Mac audits.
- Plan: [[TODOs/Optimization/Battle_FPS_Recovery_Plan]]. Mac data: [[Done/Optimization/Battle_Rendering_Performance_Audit - Research]].

## Environment

- Windows 11, AMD Ryzen 7 9800X3D (16 threads), NVIDIA GeForce RTX 5070, Direct3D11, Unity 6000.4.7f1, Editor Play Mode.
- Quality **5 Cinematic** (`URP-Cinematic`): render scale 1, MSAA off (FXAA on camera), HDR on, shadow distance 3,000, 4 cascades, 4096 shadow map, GPU Resident Drawer `InstancedDrawing`.
- Camera: 2560×1440, FOV ≈ 29.9°, pitch 55°, far clip 200,000. VSync 0, target FPS -1, focused.
- Entry: MainMenu → `StartDemoButton` → Play `StartDemoButton` → Skirmish `StartGameButton` (default skirmish).
- No code/assets changed. All temporary toggles restored. Play Mode stopped afterwards.

## Ablation run — `Logs/RenderAudit/20261029T110518Z-pc-ablation/ablation.json`

- 12 phases × 8 s (2 s settle + 6 s sample); FPS = frame count / elapsed; GPU = `FrameTimingManager.gpuFrameTime` median.
- Scene counts: field rock GameObjects 5,108; nebula 1; star renderers 2; hazard + dust renderers 11; fog 1.

| Phase | FPS | Mean ms | p95 ms | GPU med ms | Median tris |
|---|---:|---:|---:|---:|---:|
| baseline | 135.8 | 7.36 | 8.35 | 7.01 | 57.18 M |
| decor_off | 174.0 | 5.75 | 7.46 | 2.61 | 0.20 M |
| field_rocks_off | 139.3 | 7.18 | 8.55 | 6.53 | 57.39 M |
| all_rocks_off | 177.7 | 5.63 | 6.69 | 2.05 | 0.25 M |
| nebula_off | 125.6 | 7.96 | 9.22 | 5.21 | 57.81 M |
| stars_off | 127.1 | 7.87 | 9.59 | 7.14 | 57.93 M |
| hazard_dust_off | 128.7 | 7.77 | 8.84 | 7.16 | 57.94 M |
| fog_off | 128.3 | 7.79 | 9.04 | 7.30 | 57.94 M |
| postfx_off | 132.8 | 7.53 | 9.13 | 7.10 | 57.94 M |
| sun_shadows_off | 124.7 | 8.02 | 11.10 | 6.43 | 57.69 M |
| rocks+nebula+postfx+shadows off | 167.6 | 5.97 | 8.14 | 0.60 | 0.31 M |
| restored | 109.8 | 9.11 | 12.86 | 6.47 | 57.56 M |

- Decor: −4.4 ms GPU, +38 FPS. Field rocks: ≈ −0.5 ms GPU (Unity frustum culling already active).
- Nebula: −1.8 ms GPU, but FPS fell (CPU/Editor-bound phase; battle load grew). Use GPU delta, not FPS, for GPU-only items.
- Everything off → GPU 0.6 ms while FPS stays ~168 → CPU-bound in Editor.
- Camera moved during phases (`(-188.9,500,-257.8)` → `(-312.4,868.6,-975.4)`); restored phase was busier (109.8 FPS). Treat sub-0.5 ms differences as noise.
- Live readings: baseline ~136 FPS; decor-off ~199 FPS (instant `smoothDeltaTime`) → matches the reported ~80 FPS loss.
- Tool defect: JSON numbers used the OS locale decimal comma.

## Frustum test — second battle (new random map)

- Camera `(-1184,500,-1562)`, pitch 55°. Decor instance counts this map: large 1,687 / medium 2,876 / debris 3,301 + 3,384.
- Submitted after distance filter: large 1,687, medium 2,641, debris 1,033 + 1,054 → **42.99 M triangles**.
- Inside frustum (transformed AABB vs. planes): **0** for every mesh → all 43 M triangles were wasted.
- Field rocks (2,182) inside frustum: 0 → correctly culled by Unity.
- Large decor scales 1.5–2.8; mesh extents medium ≈ 13 units, large ≈ 10 units.

## Profiler — `Logs/RenderAudit/20261029T110742693Z-profiler/`

- 120 frames, idle early battle (no combat), decor enabled.
- Medians: CPU total 5.95 ms, main 4.70 ms, render thread 0.76 ms, GPU counter 4.27 ms, 43.1 M tris, 81 SetPass, 145 draws, 6,415 instanced instances in 28 instanced draws.
- Main-thread self time (avg/frame, frames 10–119): `EditorLoop` 1.10; `DecorAsteroidRenderer.LateUpdate` **0.80**; `FogFadeWriteJob (Burst)` **0.69**; `WaitForJobGroupID` 0.23; Render Graph record 0.22; BRG culling 0.15; `CaptureSitesSystem.Tick` 0.09; `UnitHoverService.Tick` 0.06; `EnemyEconomyDebugView.OnGUI` 0.05 (Editor/dev only).

## Live VFX snapshot (idle battle)

- 134 particle systems, 6 playing, 6,560 particles (StarVfx 6,500 + BrightStars 60). 48 engine trails emitting.
- 128 particle renderers on `Legacy Shaders/Particles/*`; 112 with shadow casting on.
- Culling modes: PauseAndCatchup 112, AlwaysSimulate 20, Automatic 2.
- Combat VFX load not measured in this session.

## Code findings

- `b19830c53` (2026-10-28 "- hazard system"): new `DecorAsteroidBuilder` (`MAX_INSTANCES = 15000`, `MAX_CANDIDATES = 120000`, candidates = area/1000 × `RockDensity`), new `DecorAsteroidRenderer`, `MapLayoutView` calls `decorRenderer.Configure(layout.DecorRocks, camera)`.
- `DecorAsteroidRenderer.Render`: linear scan of all instances each `LateUpdate`, distance test only (debris 2,200, medium 5,000, large = far clip 200,000), copies matrices into a 511 batch, `Graphics.RenderMeshInstanced`.
- Decor meshes: `Mball.000_Mesh.001` (large, 7,116 tris, AutodeskInteractive), `Sphere003` (medium, 11,080, URP/Lit), `Sphere001` (714), `Sphere002` (934).
- `AsteroidRockMedium.prefab` uses material `AsteroidMiningFacility_Glass` (URP/Lit opaque, env reflections + specular on).
- `NebulaFilamentVolume.shader`: `_Samples` range 48–160, default 112; `AzureRift`, `EmberWings`, `VioletPillars` set 128; loop with two `SAMPLE_TEXTURE3D_LOD` per step; early-out at transmission < 0.005; `ZTest Always`, `Blend One OneMinusSrcAlpha`.
- `SpaceHazard.shader`: per-pixel FBM loop (`octaves`), transparent, ZWrite Off; measured negligible on PC.
- Other 2026-10-28 rendering commits checked, no large measured cost: `642ff7590`/`4dd716b2a` map dust, `220aceaab` construction reveal, `981c52dd4` heavy laser beam, `7883644c3` tractor beam.

## Console findings during the run

- `MissingReferenceException` in `ExplosionVfx.StopEmitting` ← `HardPointExplosionPool.FadeAt` ← `HardPoint.OnDisable` (scene teardown).
- `MissingReferenceException` in `MusicService.SetVoiceDucking` ← `ShipSfxService.Dispose` (scope dispose).
- Play Mode was stopped at 11:06:56 UTC by an external `AssetDatabase.Refresh()` eval (not part of this capture), right after the ablation finished.

## Ablation script outline (for the Phase 0 tool)

```csharp
// EditorApplication.update driven; per phase: apply() → 2 s settle → 6 s sample → restore()
samples.Add(Time.unscaledDeltaTime * 1000f);
tris.Add(UnityEditor.UnityStats.triangles);
FrameTimingManager.CaptureFrameTimings();
if (FrameTimingManager.GetLatestTimings(1, timings) > 0) gpu.Add(timings[0].gpuFrameTime);
// toggles: DecorAsteroidRenderer.enabled, field MeshRenderer.enabled, nebula/stars/hazard/fog renderers,
// UniversalAdditionalCameraData.renderPostProcessing, Light.shadows
```

- Required fixes: pin camera pose, `CultureInfo.InvariantCulture`, record map seed and quality.

## Execution — 2026-10-29 (in progress)

- Original captures and numerical tables above are preserved; they are historical evidence, not matched acceptance for the new changes.
- Mac measurement explicitly skipped by the user for this execution.
- Blender decimation: medium 11,080 → 1,496 tris; large 7,116 → 1,196; debris 714/934 → 196/196. Bounds and UV cardinality checked; medium silhouette extrema shrink up to ~11.5%, so LOD appearance requires acceptance.
- Unity persisted/read back four low meshes, two field prefabs, SceneContext bindings and Performant volume profile. Compilation clean after integration; no automated tests run (local override).
- Particle source-prefab audit: 131 renderer occurrences; shadow casters 32 → 0, legacy-material occurrences 37 → 0 through nine shared material conversions. These counts differ from the historical live pool-expanded 112/128 counts. Exact legacy blend parity is under review before acceptance.
- Reusable Editor capture: `unity command render_audit_ablation --json`; 12 × (2 s settle + 6 s sample), restore on finish/cancel/reload/Play Mode exit. `Logs/RenderAudit/<timestamp>-ablation/ablation.json`; invariant JSON numbers; actual seed, quality, camera, resolution, source identity and nebula variant.
- Historical baseline Development Player build is isolated from the primary checkout; fresh asset import is ongoing. No new FPS result claimed yet.

- First reusable-tool run `Logs/RenderAudit/20261029T115047857Z-ablation/` was intentionally cancelled after six phases: field-renderer count was 0 because runtime clones lose PrefabUtility source links. Fixed discovery to use the actual serialized `MapLayoutView.fieldRoot`; this partial run is not performance acceptance. JSON parsed successfully with decimal-point numeric values despite the OS comma locale; cancellation persisted `status: cancelled` and restored toggles.

- Particle parity correction: exact Unity 6000.4.7f1 built-in shader source was checked against local URP 17.4 source. Standard URP premultiply differs (legacy squares vertex alpha), and legacy alpha/additive explicitly clamp output alpha. Eight materials use `EmpireAtWar/Vfx/Particle Legacy Unlit`; `matppfx_explosion03` uses stable standard URP Multiply with RGB tint 4, alpha modulation and `DstColor/Zero`. Original texture/tint/ST/queue retained. All custom passes compiled; ShaderUtil reported no shader messages. `ParticleRecoveryBuilder` is repeatable.
- Fog scheduling: `ScheduleUpdateAndWriteBytes` runs from `FogOfWarSystem.Update`; `LateUpdate` completes before texture upload. Grid reads/mutations/disposal complete pending work. No production `GetVisibility` consumers currently intervene; profiler worker placement remains to verify.
- Teardown: `HardPointExplosionPool` removes expired effects before stopping/reading native particles; `ExplosionVfx.Play` still dereferences mandatory setup. `MusicService.SetVoiceDucking` changes the gain only; normal Tick applies it.

- Final tool review tightened the live protocol: 21 × 8 s = 168 s, with a restored sample after every off phase (the previous restored sample is the next enabled baseline). This replaces the initial 12-phase tool protocol, while preserving the historical 12-phase research table above. Battle evolution still limits small FPS/CPU causal claims; historical Player comparison uses a frozen matched snapshot.
- Tool metadata now includes untracked paths; renderer toggles are applied once at phase boundaries to avoid per-frame native setter overhead contaminating off-phase CPU measurements.

## Current Editor verification — 2026-10-29

- Completed `Logs/RenderAudit/20261029T121629109Z-ablation/ablation.json`: all 21 phases; seed `20261029`, Medium map, Cinematic, 2560×1440, camera `(-1184,500,-1562)` / pitch 55° / FOV 29.9°, nebula variant 0. Actual field renderer count 22,124 includes LOD renderers; nebula 1, stars 2, hazard/dust 25, fog 1.
- Opening baseline **194.113 FPS**, GPU **1.284608 ms**, **153,340 triangles**. Final restored **155.463 FPS**, GPU **1.286400 ms**, **153,344 triangles**. Live battle evolved; these are not a matched historical FPS speedup or sustained ≥190 FPS claim.
- After completion: input lock false, camera restored exactly, decor enabled, post-FX enabled. No current console errors after returning to menu; neither reported teardown exception recurred.
- Profiler: `Logs/RenderAudit/20261029T122050363Z-profiler/`, 120 frames completed. Frames 10–119: decor `LateUpdate` main self **0.03336 ms/frame** (below 0.2 ms target); FogFadeWriteJob main marker **0.07546 ms/frame**, workers combined **0.87314 ms/frame** / 109 worker calls. Worker execution confirmed; main scheduling/remaining marker cost is not zero. Attribution: `fog-decor-attribution.json`.
- Historical Development build temporarily paused for resource scheduling: physical RAM free 0.79 GiB with both Editors, 3.47 GiB after stopping only the isolated build tree. Import cache retained; no historical performance measurement yet. Resume sequentially after primary acceptance.

## Combat capture — 2026-10-29

- Battle Simulator: 2 Venator + 4 Arquitens vs 2 Providence + 4 Munificent, hangars on, separation 500 units, normal speed. Initial paused/end-state snapshot excluded; restarted before the accepted recording.
- `Logs/RenderAudit/combat-running-snapshot.json`: 9 surviving ships, 10 squadrons, 1,375 particle systems, 46 playing, 11,319 particles; live particle shadow casters **0**, legacy particle-material renderers **0**, time scale **1**.
- Accepted CSV: `Logs/RenderAudit/20261029-combat/battle_20261029_122808_577.csv` (+ summary), 1,895 frames / 10.001 s, Cinematic 2560×1440, source `d92834f30` dirty. Average frame **5.276 ms** (~189.5 FPS), p95 **7.395 ms**, p99 **9.589 ms**; GPU average **2.211 ms**, p95 **2.487 ms**; triangles average **1,307,436**, p95 **1,435,618**. No pre-migration combat baseline → no isolated VFX speedup claim.
- Combat Profiler: `Logs/RenderAudit/20261029T122831775Z-profiler/`, 120 frames completed; final rendered frame 12:28:32 UTC, before quality switching.
- Runtime Performant verification found a remaining integration defect: nebula MPB samples **48**, but effective volume stack still bloom **6/Half**, motion blur **0.1**. Saved sparse pipeline profile alone is insufficient in this scene. Fix is in progress; Phase 4 not yet accepted.

- Correction to the first preset diagnostic: quality index **1 is Low**, **0 is Performant**. Actual Performant proof (`performant-zero-before-fix.json`) shows **Bloom 3/Quarter/HQ false** already works. The remaining defect is **MotionBlur 0.1**, explicitly overridden by the scene Global Volume Profile. A Performant-only priority volume is being bound to enforce intensity 0; the earlier broad bloom failure diagnosis is superseded.

## Editor ablation — 21 phase table

- Source: `Logs/RenderAudit/20261029T121629109Z-ablation/ablation.json`. Live evolving battle; use bracketed GPU readings, not small FPS/CPU deltas, for attribution.

| Phase | FPS | Mean ms | p95 ms | GPU median ms | Median triangles |
|---|---:|---:|---:|---:|---:|
| baseline | 194.113 | 5.152 | 6.697 | 1.284608 | 153340 |
| decor_off | 182.123 | 5.491 | 8.938 | 1.285632 | 153338 |
| decor_restored | 189.814 | 5.268 | 6.866 | 1.285120 | 153340 |
| field_rocks_off | 210.522 | 4.750 | 6.596 | 1.283840 | 132934 |
| field_rocks_restored | 187.324 | 5.338 | 7.057 | 1.283328 | 153340 |
| all_rocks_off | 164.223 | 6.089 | 9.949 | 1.287936 | 132934 |
| all_rocks_restored | 198.981 | 5.026 | 5.977 | 1.283328 | 153340 |
| nebula_off | 189.320 | 5.282 | 6.763 | 0.748544 | 153328 |
| nebula_restored | 194.449 | 5.143 | 6.460 | 1.281792 | 153340 |
| stars_off | 200.392 | 4.990 | 5.942 | 1.264640 | 140222 |
| stars_restored | 198.648 | 5.034 | 5.825 | 1.282560 | 153340 |
| hazard_dust_off | 199.797 | 5.005 | 5.551 | 1.148672 | 144950 |
| hazard_dust_restored | 196.663 | 5.085 | 5.716 | 1.280768 | 153340 |
| fog_off | 196.062 | 5.100 | 5.378 | 1.243392 | 153140 |
| fog_restored | 183.216 | 5.458 | 6.663 | 1.281536 | 153340 |
| postfx_off | 160.676 | 6.224 | 8.093 | 1.099264 | 153323 |
| postfx_restored | 167.481 | 5.971 | 8.993 | 1.287936 | 153344 |
| sun_shadows_off | 114.824 | 8.709 | 9.006 | 1.278464 | 90614 |
| sun_shadows_restored | 149.583 | 6.685 | 8.835 | 1.287168 | 153344 |
| rocks_nebula_postfx_shadows_off | 204.398 | 4.892 | 6.599 | 0.547072 | 70177 |
| restored | 155.463 | 6.432 | 8.624 | 1.286400 | 153344 |


## Final Performant binding

- `PerformantPostProcessing` uses an explicit serialized Volume; same sparse `PerformantVolumeProfile`, global priority **100**, layer **0**. Active only for quality index **0 (Performant)**; other presets disable the override.
- Actual main-camera proof: `performant-zero-after-fix.json` → nebula **48**, bloom **3 / Quarter / HQ false**, motion blur **0**. `performant-restored-cinematic.json` → quality **5**, bloom **6 / Half**, motion blur **0.1**, priority volume disabled.
- Returned to MainMenu and stopped the agent-owned Play session. Only saved/clean `MainMenuScene` remained. Primary Editor closed through Unity API to free RAM for sequential historical builds; reopen the original project after comparisons.
- Temporary screenshot imported by Pipeline into `Assets/Logs` was copied to `Logs/RenderAudit/combat-screen.png`; verified the folder contained only this capture and deleted it through AssetDatabase. No temporary capture assets remain under Assets.

## Source Review — 2026-10-29

- Current user instruction defers all builds and performance runs; historical-build import/resume notes above describe earlier work, not the current execution.
- Live source/assets confirm the Phase 0 tool and Phase 1–6 implementations. No code/assets changed in this execution.
- Existing research tables and capture numbers preserved. No fresh benchmark, build, Play Mode session, test or shader compilation run.
- Remaining absorbed investigations need attribution/visual evidence before selecting additional optimizations; plan remains active.