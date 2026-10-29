---
category: Optimization
status: todo
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
