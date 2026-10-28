---
category: Optimization
status: todo
created: 2026-10-28
audit_date: 2026-10-28
---
# Battle Rendering Performance Audit — Research

## Goal

- Preserve both 2026-10-28 runtime audits and their preliminary capture; distinguish confirmed findings from unresolved issues.
- Requirements and open implementation work: [[TODOs/Optimization/Battle_Rendering_Performance_Audit]].
- Hardware: Apple M2; Unity 6000.4.7f1; Editor-hosted Play Mode. Capture directory timestamps are UTC.

## Rules

- Both audits restored decorative asteroids; Play Mode remained active, with temporary pause/step only inside the existing Frame Debugger capture.
- No production code, serialized assets, quality settings or Obsidian configuration were changed. No automated tests ran.
- Audit A quality preset was not recorded; do not label it Balanced/High or assume a preset. Audit B was verified Performant.
- GPU counters, thread timings and frame intervals are different measurements; do not add them or infer FPS from the GPU counter.
- Raw traces and JSON remain under git-ignored `Logs/RenderAudit/`; this note preserves findings and summary measurements, not a binary trace backup.

## Implementation

### Existing tools and collection method

- Official `unity` CLI; `editor_status`, `console`, read-only `eval`, existing `render_audit_profiler` and `render_audit_capture`.
- Combined captures: `unity command render_audit_capture --frames 30 --timeout_seconds 240 --frame_debugger_frames 1 --json`.
- Follow-up captures: `unity command render_audit_profiler --frames 60 --timeout_seconds 30 --json`.
- `BattleInstancingAudit.Run()` produced a static renderer/material TSV; direct prefab text inventory covered all Assets particle-containing prefabs, including vendor examples.
- Read-only runtime inventories inspected particle systems, renderer materials, live mesh/instance counts, cameras, quality and pipeline settings. No test runner used.
- On/off/restored comparison temporarily changed only `DecorAsteroidRenderer.enabled`; callback restored its original value on completion/error. No map GameObject was disabled.

### Preliminary profiler — historical, before causal comparison

- Evidence directory: `Logs/RenderAudit/20261028T185923754Z-profiler/`.
- Play Mode kept running; no pause, code changes, asset changes, or tests.
- 60-frame profiler capture completed. Profiler restored to disabled; deep profiling disabled.
- Initial runtime Screen reading: 2940×1514; VSync 0, target FPS -1, focused, timeScale 1. Later captures establish actual rendering dimensions separately.

- **Measured**
- Rendered intervals: mean 51.071 ms (~19.6 FPS), median 45.798 ms, p95 101.182 ms.
- CPU total counter: mean 53.659 ms, median 45.677 ms.
- CPU main active counter: mean 17.432 ms, median 14.590 ms.
- CPU render active counter: mean 2.368 ms.
- Main-thread Semaphore.WaitForSignal: 25.397 ms/frame average across exported top-40 markers.
- 58.542 million triangles/frame; 58.079 million instanced; 8,380 instanced instances.
- GPU time counter is zero/unavailable; no definitive GPU-duration measurement.
- Counters are Editor-hosted and may include Editor rendering.
- Capture startup/JIT/profiler collection overhead affects spikes; not all spikes represent normal gameplay.

- **Live decorative geometry snapshot (later than capture; counts may differ)**
- Large: 2,187 submitted x 7,116 triangles = 15,562,692; cutoff 200,000 units.
- Medium: 3,554 submitted x 11,080 triangles = 39,378,320; cutoff 5,000 units.
- Debris Sphere001: 664 submitted x 714 = 474,096.
- Debris Sphere002: 696 submitted x 934 = 650,064.
- Total submitted decor: 56,065,172 triangles.
- Source: `Assets/Scripts/Components/Environment/DecorAsteroidRenderer.cs`.
- Render lines 63-86 performs distance-only filtering; no per-instance frustum culling or LOD selection.
- DrawBatch lines 89-103 submits batches of up to 511 instances. Shadows disabled for decor.
- Large decor distance uses camera.farClipPlane. Medium mesh has more triangles than large mesh.

- **Conclusion**
- Historical preliminary conclusion: Excessive decorative asteroid geometry is the strongest bottleneck candidate; measured CPU waiting supports rendering-side pressure. GPU bottleneck remains an inference without GPU timing or controlled A/B comparison. Later on/off/restored evidence confirms the dominant cost in the measured views.
- First optimization candidates: low-poly/LOD decor meshes, per-instance frustum culling or spatially localized batches, dedicated large-decor draw distance.
- Secondary CPU costs: FogFadeWriteJob ~1.327 ms/frame on main thread, DecorAsteroidRenderer.LateUpdate ~1.138 ms/frame. Do not add nested marker times.
- Earlier native allocation lifetime warnings observed around 18:55 UTC; not established as cause of sustained FPS loss.
- One runtime query timed out during capture export; succeeded after export finished with longer query timeout.
- Artifacts: profiler.data, profiler-summary.json, capture-status.json, live-decor-geometry.json.

### Audit A — first complete battle investigation

- Evidence directory: `Logs/RenderAudit/20261028T191418575Z-capture/`.

- **CONFIRMED RESULT**
- Decorative asteroids dominate the sustained frame cost in the measured view.
- 24-second on/off/restored experiment; each 8-second phase discarded 2 seconds for settling and sampled 6 seconds.
- Camera position, rotation and 2560x1440 resolution identical across every recorded sample.
- On: 39.228 FPS, mean 25.499 ms, median 25.460 ms, p95 28.336 ms, median 52,060,135 triangles.
- Off: 100.912 FPS, mean 9.908 ms, median 10.097 ms, p95 11.191 ms, median 286,015 triangles.
- Restored: 39.353 FPS, mean 25.422 ms, median 25.325 ms, p95 28.453 ms, median 52,060,033 triangles.
- FPS computed from actual Unity frame progression divided by elapsed time. Per-frame durations sampled on Editor update for distinct frame IDs. This is a live evolving battle, not deterministic replay.
- About 15.6 ms/frame (61%) removed; 2.57x FPS. On/restored agreement supports attribution to the renderer.
- Only DecorAsteroidRenderer.enabled was temporarily changed. The map object and gameplay stayed active. Renderer restored and enabled afterward.
- First attempt restored successfully but output serialization failed for Unity Vector3; second attempt used primitive arrays and exported successfully.

- **EXISTING EDITOR TOOLS**
- render_audit_capture: 30 profiler frames, 30 Render Graph frames, Rendering Debugger, 1 Frame Debugger frame.
- Profiler and Frame Debugger exports completed. Frame Debugger: 133/133 events, not truncated.
- Rendering Debugger partial: a stale Volume camera selector could not be read. Other panels exported.
- Frame Debugger caused UnityEditor.FrameDebuggerWindow.DrawEnabledFrameDebugger NullReferenceExceptions during capture. Disabled afterward; no new errors in subsequent cursor check. No further Frame Debugger captures run.
- An earlier all-tools capture at 191243 captured the menu/transition camera, not the battle; excluded from battle conclusions.

- **DIRECT DRAW EVIDENCE**
- Frame Debugger expanded index counts / 3 (do not multiply again by instance counts):
- Sphere003 medium rocks: 13 events, 3273 instances, 36,264,840 triangles.
- Mball.000_Mesh.001 large rocks: 9 events, 2127 instances, 15,135,732 triangles.
- Sphere001 debris: 4 events, 1007 instances, 718,998 triangles.
- Sphere002 debris: 5 events, 1077 instances, 1,005,918 triangles.
- The asteroid geometry is actually submitted in the captured draw events, not merely present in asset files.

- **GPU/CPU EVIDENCE**
- Initial battle profiler: median GPU counter 45.203 ms; main active 8.365 ms; render active 1.636 ms.
- Later busier battle profiler: Logs/RenderAudit/20261028T191801399Z-profiler; 60 frames completed.
- Later medians: GPU 50.234 ms; main active 9.907 ms; render active 1.877 ms; total CPU 32.144 ms; 57.418 million triangles.
- These are asynchronous counters in an Editor-hosted player, not additive components of one synchronous frame. Profiler/capture overhead affects timings. Use unprofiled on/off/restored comparison for causal FPS result.

- **VFX FINDINGS**
- Not all effects can be certified optimized from these snapshots. They are not the main sustained bottleneck in the tested view, but have actionable review targets.
- 1. Visible Nebula Filament Volume: Frame Debugger event 50, _Samples=128, two 3D texture fetches per loop iteration in source, early termination present. One draw can have high pixel cost. No per-effect GPU duration available, so no precise nebula cost claimed.
- 2. Stars: 6500 + 60 live particles, 26240 vertices, 2 draws in one dynamic geometry event. No collision/noise/lights/trails, shadows off. Much smaller geometry burden than asteroids.
- 3. Space dust: one captured draw, 16760 vertices / 8380 triangles. Transparent/depth-sampling shader; overdraw not quantitatively measured.
- 4. Bloom: 17 captured draw events; motion blur, SSAO (3 events) and post-processing also present. Draw counts do not establish their GPU costs.
- 5. Early live snapshot: 138 particle systems, 6 playing, 6560 particles. HardPointExplosionPool contained legacy particle shaders and shadow-enabled renderers, all with zero particles in that snapshot.
- 6. Later renderer snapshot: 683 particle renderers; 440 reference Legacy Shaders/Particles variants; 424 have shadow casting enabled. All reported shaders isSupported=true. This does NOT prove correct URP pass support or that a shadow draw is emitted. Original prefab links unavailable on runtime objects.
- 7. Later combat snapshot: 763 systems, 54 playing, 11721 particles; substantial trail particles. Most systems are idle/pooled. Counts changing during combat are not proof of a leak.
- 8. Later profiler top-40 marker export shows individual particle geometry/update worker entries around 0.02-0.08 ms/frame. No dominant particle CPU hotspot observed. Export is truncated per thread; do not interpret missing markers as zero cost or sum parallel worker times as frame duration.

- **ASSET AUDIT SCOPE**
- Read all Assets prefab text: 104 particle-containing prefabs, 297 ParticleSystem blocks including vendor examples.
- Assets/Prefabs subset: 42 prefabs, 112 blocks; 5 partial/stripped blocks lack complete settings. Nested variants/references mean these are not resolved instance totals.
- 107 full project-owned blocks: collision/trigger/trail modules disabled throughout; lights enabled in 5, noise in 4.
- Existing BattleInstancingAudit: 70 particle renderer/material rows, 65 URP Particles/Unlit and 5 Ion Glow, 66 billboard and 4 stretched. It limits VFX folder to names containing Projectile and misses legacy explosion effects found live. Its clean result is not an all-VFX certification.
- Particle maximums are capacity, not active load. Billboard/stretch particle rendering does not gain ordinary mesh instancing just from checking instancing flags.

- **PRIORITIES**
- First: asteroid geometry reduction/LOD, per-instance frustum culling or spatial batches, sensible large-rock distance limit.
- Then: evaluate visible nebula sample count and screen coverage; review legacy explosion materials, shadow flags, and offscreen simulation behavior during representative explosions.

### Audit B — Performant repeat

- Evidence directory: `Logs/RenderAudit/20261028T193330002Z-capture/`.
- Runtime: Battle.unity, Play Mode active/unpaused, Performant index 0, URP-Performant, Apple M2, targetFPS -1, VSync 0, timeScale 1.
- Screen initially reported 2940x1514; actual Frame Debugger targets and later camera are 2560x1440. Do not attribute slowdown to the initial Screen reading. Runtime pipeline serialized values: renderScale 1, MSAA 2, HDR false, shadowDistance 50, SRP Batcher true, dynamic batching false, GPU Resident Drawer mode 0.
- Combined capture: 30 profiler + 30 Render Graph frames; Frame Debugger 163 events. Profiler/Render Graph/Frame Debugger completed. Rendering Debugger partial: stale destroyed Volume camera selector. Unity FrameDebuggerWindow.DrawEnabledFrameDebugger NullReferenceExceptions recurred during frame capture. Tool disabled Frame Debugger and restored unpaused state afterward.
- Observed shader events: Bloom 17, motion blur 1, no SSAO or shadow draws. Nebula Filament Volume still _Samples=128. Performant is applied but does not eliminate those custom/background/post-processing workloads.
- Initial profiler medians: CPU total 51.811 ms; main active 9.258 ms; render active 2.426 ms; GPU counter 108.278 ms; 59,098,570 triangles.
- Final 60-frame profiler: Logs/RenderAudit/20261028T193612669Z-profiler.
- Final medians: CPU total 52.264 ms; main active 10.189 ms; render active 2.660 ms; GPU counter 107.740 ms; 54,946,897 triangles.
- GPU counter exceeds observed frame interval. Preserve as an Editor profiler counter, not an independently validated GPU latency or FPS estimate. Counters overlap/asynchronously report; do not add them. The unprofiled comparison below is primary causal evidence.
- Frame Debugger mesh totals (index counts already expanded; do not multiply again):
- Medium Sphere003: 15 events, 3641 instances, 40,342,280 triangles.
- Large Mball.000_Mesh.001: 9 events, 2280 instances, 16,224,480 triangles.
- Debris Sphere002: 5 events, 1458 instances, 1,361,772 triangles.
- Debris Sphere001: 5 events, 1433 instances, 1,023,162 triangles.
- Space dust: 8380 triangles; planet: 36480 triangles.
- Unprofiled 24-second asteroid comparison; 8 seconds per phase, 2-second settling + 6-second observation:
- On: 116 samples; 19.318 FPS; mean 51.749 ms, median 51.838 ms, p95 54.420 ms; median 59,019,361 triangles.
- Off: 292 samples; 48.634 FPS; mean 20.562 ms, median 20.440 ms, p95 22.315 ms; median 105,083 triangles.
- Restored: 116 samples; 19.350 FPS; mean 51.725 ms, median 51.854 ms, p95 54.568 ms; median 59,021,715 triangles.
- All phases quality 0; same camera position (1198.28711,500,724.2209), quaternion (0.46174863,0,0,0.8870109), 2560x1440. Camera moved between earlier capture/snapshot and comparison; do not treat phase captures as identical frames. FPS uses actual frame progression / elapsed time.
- Decor rendering adds about 31.19 ms/frame: ~60.3% of current baseline; disabling gives ~2.52x FPS. Restored baseline agrees closely.
- Earlier comparison was 39.2 -> 100.9 -> 39.4 FPS, with a different view/load. Current residual off cost 20.56 ms versus earlier 9.91 ms. This does not isolate a quality-preset regression; no preset A/B was authorized/performed.
- Live VFX:
- First snapshot 137 systems, 6 playing, 6560 particles. Later 138 systems, 6 playing, 6560 particles. Only 6500 stars + 60 bright stars had particles in these snapshots; impact emitters playing with zero particles. Explosion pools idle.
- 128 particle renderers use legacy materials; 112 have non-Off shadow flags. Flags do not prove actual shadow draws; captured frame had zero shadow casters. Existing material concerns persist but cannot explain this frame through active explosions.
- 128-sample nebula remains a pixel-cost candidate; no isolated per-effect GPU timing or overdraw measurement. Final top-40 export's largest particle geometry worker entry ~0.13 ms/frame; no dominant particle CPU hotspot observed. Do not sum parallel worker times as frame duration.
- Conclusion: decorative asteroid geometry remains the main sustained bottleneck on Performant. Preset removes some features but does not reduce the dominant asteroid geometry or the nebula sample count. Residual workload is significant; exact reason for difference from earlier view is not isolated.
- Final audit verification: decor enabled, Performant unchanged, Play Mode active/unpaused, profiler off, Frame Debugger off.

## Important Values

### Complete on/off/restored sample statistics

- FPS = Unity frame-count advancement / elapsed observation time; duration samples use unscaledDeltaTime for distinct frame IDs.
- p95 below uses the sorted sample at floor(0.95 × (N−1)); 2-second settling excluded.

| Audit / phase | Samples | FPS | Mean ms | Median ms | p95 ms | Median triangles |
|---|---:|---:|---:|---:|---:|---:|
| A / Enabled | 236 | 39.228 | 25.499 | 25.460 | 28.336 | 52,060,135 |
| A / Disabled | 605 | 100.912 | 9.908 | 10.097 | 11.191 | 286,015 |
| A / Restored | 237 | 39.353 | 25.422 | 25.325 | 28.453 | 52,060,033 |

- Audit A camera: `[{"name":"Main Camera","pos":[-1141.30347,664.9882,-1838.10327],"rotation":[0.46174863,0.0,0.0,0.8870109],"pixelWidth":2560,"pixelHeight":1440}]`.

| Audit / phase | Samples | FPS | Mean ms | Median ms | p95 ms | Median triangles |
|---|---:|---:|---:|---:|---:|---:|
| B — Performant / Enabled | 116 | 19.318 | 51.749 | 51.838 | 54.420 | 59,019,361 |
| B — Performant / Disabled | 292 | 48.634 | 20.562 | 20.440 | 22.315 | 105,083 |
| B — Performant / Restored | 116 | 19.350 | 51.725 | 51.854 | 54.568 | 59,021,715 |

- Audit B — Performant camera: `[{"name":"Main Camera","pos":[1198.28711,500.0,724.2209],"rotation":[0.46174863,0.0,0.0,0.8870109],"pixelWidth":2560,"pixelHeight":1440}]`.

### Profiler timing distributions

- Units: ms; values computed from exported nanosecond counters. Zero GPU data in the preliminary capture means unavailable.
- First 30-frame capture in Audit A and later 60-frame captures are separate live windows, not controlled before/after pairs.

| Capture | Counter | Mean | Median | p95 | Min | Max |
|---|---|---:|---:|---:|---:|---:|
| Preliminary (60 frames) | CPU Total Frame Time | 53.659 | 45.677 | 102.065 | 16.853 | 464.339 |
| Preliminary (60 frames) | CPU Main Thread Frame Time | 17.432 | 14.590 | 27.656 | 8.447 | 99.734 |
| Preliminary (60 frames) | CPU Render Thread Frame Time | 2.368 | 2.315 | 2.866 | 1.947 | 3.534 |
| Preliminary (60 frames) | GPU Frame Time | 5.311 | 0.000 | 51.060 | 0.000 | 75.377 |
| A / combined (30 frames) | CPU Total Frame Time | 43.901 | 25.488 | 27.281 | 13.807 | 594.290 |
| A / combined (30 frames) | CPU Main Thread Frame Time | 9.734 | 8.365 | 13.505 | 6.125 | 36.856 |
| A / combined (30 frames) | CPU Render Thread Frame Time | 1.776 | 1.636 | 2.098 | 1.456 | 3.348 |
| A / combined (30 frames) | GPU Frame Time | 47.461 | 45.203 | 55.450 | 35.844 | 93.754 |
| A / combat (60 frames) | CPU Total Frame Time | 37.994 | 32.144 | 35.886 | 12.896 | 439.932 |
| A / combat (60 frames) | CPU Main Thread Frame Time | 12.947 | 9.907 | 24.137 | 7.722 | 64.654 |
| A / combat (60 frames) | CPU Render Thread Frame Time | 1.958 | 1.877 | 2.354 | 1.598 | 2.900 |
| A / combat (60 frames) | GPU Frame Time | 51.007 | 50.234 | 61.416 | 38.468 | 87.939 |
| B / combined (30 frames) | CPU Total Frame Time | 57.199 | 51.811 | 53.022 | 12.124 | 290.754 |
| B / combined (30 frames) | CPU Main Thread Frame Time | 10.028 | 9.258 | 13.004 | 5.964 | 28.263 |
| B / combined (30 frames) | CPU Render Thread Frame Time | 2.498 | 2.426 | 3.345 | 1.563 | 4.319 |
| B / combined (30 frames) | GPU Frame Time | 110.595 | 108.278 | 116.015 | 87.780 | 172.476 |
| B / follow-up (60 frames) | CPU Total Frame Time | 55.593 | 52.264 | 55.444 | 13.666 | 300.118 |
| B / follow-up (60 frames) | CPU Main Thread Frame Time | 10.375 | 10.189 | 12.870 | 7.029 | 19.522 |
| B / follow-up (60 frames) | CPU Render Thread Frame Time | 2.688 | 2.660 | 3.072 | 1.825 | 4.564 |
| B / follow-up (60 frames) | GPU Frame Time | 107.605 | 107.740 | 123.528 | 83.638 | 180.577 |

### Additional static VFX findings

- `SkirmishVfx.prefab`: StarVfx maximum 6,500; `ImpactEffects.prefab`: one emitter maximum 2,048. Capacity is not measured emission or GPU cost.
- Original project particle block culling values: Automatic 82, AlwaysSimulate 16, PauseAndCatchup 9, missing/stripped 5; runtime clones include vendor/nested settings beyond this text inventory.
- Limited `BattleInstancingAudit` returned no missing/unsupported-material blockers in its scope; live legacy explosion materials show why this does not certify all VFX.
- `NebulaCloudVolume.shader`: default 64 samples, clamp 24–96; `NebulaBillowVolume.shader`: default 80, clamp 32–128.
- `NebulaFilamentVolume.shader`: default 112, clamp 48–160; captured material uses 128. `VolumetricNebulaRaymarch.shader`: at most 30 steps.
- Raymarch shaders have early termination; declared defaults do not prove use in a captured frame. Only the filament shader was specifically confirmed active by the reported draw evidence.
- Transparent/additive blending with ZWrite Off is common among VFX. Screen coverage/overlap remain unmeasured; draw count and low triangle count cannot certify low pixel cost.

## Edge Cases

### Audit-tool issues and rejected evidence

- Preliminary query timed out after 5,000 ms during capture export; succeeded after export with 20,000 ms timeout. Do not classify this as a sustained gameplay stall without independent evidence.
- Earlier NativeArray disposal warnings: allocation lifetime exceeded 4 frames (ages 5–9); not proven to cause sustained FPS loss.
- `20261028T191243330Z-capture`: combined tool completed but captured menu/transition Camera, only 8 Frame Debugger events and no decor renderer. Rejected as battle evidence.
- Both accepted combined captures triggered `UnityEditor.FrameDebuggerWindow.DrawEnabledFrameDebugger` NullReferenceException / IMGUI repaint errors. Frame Debugger disabled afterward; subsequent console cursor checks returned no new errors. Tool defect remains open.
- Both Rendering Debugger exports were partial because `Volume -> Camera` referenced a destroyed camera. Other panel values and Frame Debugger draw exports were produced; no all-tools-clean claim.
- First Audit A comparison restored renderer state but JSON export failed on `UnityEngine.Vector3.normalized` self-reference. Successful retry serialized primitive camera arrays; use only successful retry measurements.
- One read-only eval used an incorrect FrameDebugger namespace and failed compilation in memory; corrected reflection lookup read `UnityEngine.FrameDebugger`. No project scripts were changed/recompiled.
- Temporary Editor connection failure occurred around scene transitions; state was checked again. Initial menu/stop observations were not battle results.
- Some Rendering Debugger frame/GPU values were zero; do not interpret unset counters as zero workload.
- Raw frame summaries retain only top 40 markers by self time per thread. Inclusive times overlap; missing markers are not free, and parallel worker durations are not additive frame cost.

## TODO

- [[TODOs/Optimization/Battle_Rendering_Performance_Audit#TODO|Implementation and acceptance checklist]].
- Fix capture-window exceptions, stale-camera panel reads and explicit rejection of menu/nonrepresentative frames.
- Isolate remaining GPU work with matched view/load/preset, then validate in a Development Player; current runs do not isolate a preset regression.
- Review legacy effects under actual emission, including offscreen recovery and shadow-pass behavior. No all-effects optimization certification was established.

## Files

- Repository-relative artifact paths below; files are local and git-ignored. Notes are versioned; raw traces are not included in the documentation commit.
- Each accepted profiler directory contains `profiler.data`, `profiler-summary.json`, and `capture-status.json`.
- Each combined capture also has `render-graph-frame-*.json`, `render-graph-summary.json`, `rendering-debugger.json`, `frame-debugger-frame-01.json`, and `frame-debugger-summary.json`.

### `20261028T185923754Z-profiler`

- `Logs/RenderAudit/20261028T185923754Z-profiler/capture-status.json` — 6,914,979 bytes.
- `Logs/RenderAudit/20261028T185923754Z-profiler/findings.txt` — 2,582 bytes.
- `Logs/RenderAudit/20261028T185923754Z-profiler/live-decor-geometry.json` — 2,781 bytes.
- `Logs/RenderAudit/20261028T185923754Z-profiler/profiler-summary.json` — 6,914,979 bytes.
- `Logs/RenderAudit/20261028T185923754Z-profiler/profiler.data` — 32,344,944 bytes.

### `20261028T191243330Z-capture`

- `Logs/RenderAudit/20261028T191243330Z-capture/battle-instancing-audit.tsv` — 3,607,274 bytes.
- `Logs/RenderAudit/20261028T191243330Z-capture/capture-status.json` — 845,296 bytes.
- `Logs/RenderAudit/20261028T191243330Z-capture/capture-validity-and-vfx-notes.txt` — 2,374 bytes.
- `Logs/RenderAudit/20261028T191243330Z-capture/frame-debugger-frame-01.json` — 56,100 bytes.
- `Logs/RenderAudit/20261028T191243330Z-capture/frame-debugger-summary.json` — 412 bytes.
- `Logs/RenderAudit/20261028T191243330Z-capture/particle-prefab-inventory.json` — 95,897 bytes.
- `Logs/RenderAudit/20261028T191243330Z-capture/profiler-summary.json` — 744,549 bytes.
- `Logs/RenderAudit/20261028T191243330Z-capture/profiler.data` — 40,949,444 bytes.
- `Logs/RenderAudit/20261028T191243330Z-capture/render-graph-frame-01.json` — 81,858 bytes.
- `Logs/RenderAudit/20261028T191243330Z-capture/render-graph-frame-02.json` — 81,858 bytes.
- `Logs/RenderAudit/20261028T191243330Z-capture/render-graph-frame-03.json` — 81,858 bytes.
- `Logs/RenderAudit/20261028T191243330Z-capture/render-graph-frame-04.json` — 81,858 bytes.
- `Logs/RenderAudit/20261028T191243330Z-capture/render-graph-frame-05.json` — 81,858 bytes.
- `Logs/RenderAudit/20261028T191243330Z-capture/render-graph-frame-06.json` — 81,858 bytes.
- `Logs/RenderAudit/20261028T191243330Z-capture/render-graph-frame-07.json` — 81,858 bytes.
- `Logs/RenderAudit/20261028T191243330Z-capture/render-graph-frame-08.json` — 81,858 bytes.
- `Logs/RenderAudit/20261028T191243330Z-capture/render-graph-frame-09.json` — 81,858 bytes.
- `Logs/RenderAudit/20261028T191243330Z-capture/render-graph-frame-10.json` — 81,858 bytes.
- `Logs/RenderAudit/20261028T191243330Z-capture/render-graph-summary.json` — 2,297 bytes.
- `Logs/RenderAudit/20261028T191243330Z-capture/rendering-debugger.json` — 238,221 bytes.

### `20261028T191418575Z-capture`

- `Logs/RenderAudit/20261028T191418575Z-capture/asteroid-comparison-error.txt` — 9,502 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/asteroid-comparison-summary.json` — 1,667 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/asteroid-comparison.json` — 571,285 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/battle-instancing-audit.tsv` — 3,607,274 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/capture-status.json` — 3,801,013 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/findings.txt` — 6,251 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/frame-debugger-frame-01.json` — 22,553,576 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/frame-debugger-summary.json` — 416 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/live-combat-particles.json` — 199,061 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/live-particles.json` — 132,266 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/live-vfx-sources.json` — 270,330 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/particle-prefab-inventory.json` — 95,897 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/profiler-summary.json` — 3,371,461 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/profiler.data` — 64,499,828 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/render-graph-frame-01.json` — 131,339 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/render-graph-frame-02.json` — 131,339 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/render-graph-frame-03.json` — 131,339 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/render-graph-frame-04.json` — 131,339 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/render-graph-frame-05.json` — 131,339 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/render-graph-frame-06.json` — 131,339 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/render-graph-frame-07.json` — 131,339 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/render-graph-frame-08.json` — 131,339 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/render-graph-frame-09.json` — 131,339 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/render-graph-frame-10.json` — 131,339 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/render-graph-frame-11.json` — 131,339 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/render-graph-frame-12.json` — 131,339 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/render-graph-frame-13.json` — 131,339 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/render-graph-frame-14.json` — 131,339 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/render-graph-frame-15.json` — 131,339 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/render-graph-frame-16.json` — 131,339 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/render-graph-frame-17.json` — 131,339 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/render-graph-frame-18.json` — 131,339 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/render-graph-frame-19.json` — 131,339 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/render-graph-frame-20.json` — 131,339 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/render-graph-frame-21.json` — 131,339 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/render-graph-frame-22.json` — 131,339 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/render-graph-frame-23.json` — 131,339 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/render-graph-frame-24.json` — 131,339 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/render-graph-frame-25.json` — 131,339 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/render-graph-frame-26.json` — 131,339 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/render-graph-frame-27.json` — 131,339 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/render-graph-frame-28.json` — 131,339 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/render-graph-frame-29.json` — 131,339 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/render-graph-frame-30.json` — 131,339 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/render-graph-summary.json` — 6,357 bytes.
- `Logs/RenderAudit/20261028T191418575Z-capture/rendering-debugger.json` — 238,775 bytes.

### `20261028T191801399Z-profiler`

- `Logs/RenderAudit/20261028T191801399Z-profiler/capture-status.json` — 6,667,860 bytes.
- `Logs/RenderAudit/20261028T191801399Z-profiler/profiler-summary.json` — 6,667,860 bytes.
- `Logs/RenderAudit/20261028T191801399Z-profiler/profiler.data` — 95,035,696 bytes.

### `20261028T193330002Z-capture`

- `Logs/RenderAudit/20261028T193330002Z-capture/asteroid-comparison-summary.json` — 1,699 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/asteroid-comparison.json` — 284,329 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/capture-status.json` — 3,296,828 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/final-profiler-metrics.json` — 431 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/findings.txt` — 4,621 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/frame-debugger-frame-01.json` — 10,751,928 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/frame-debugger-summary.json` — 418 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/live-particles-later.json` — 132,267 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/live-particles.json` — 131,334 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/profiler-summary.json` — 2,915,237 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/profiler.data` — 107,255,556 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/render-graph-frame-01.json` — 108,149 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/render-graph-frame-02.json` — 108,149 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/render-graph-frame-03.json` — 108,149 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/render-graph-frame-04.json` — 108,149 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/render-graph-frame-05.json` — 108,149 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/render-graph-frame-06.json` — 108,149 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/render-graph-frame-07.json` — 108,149 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/render-graph-frame-08.json` — 108,149 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/render-graph-frame-09.json` — 108,149 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/render-graph-frame-10.json` — 108,149 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/render-graph-frame-11.json` — 108,149 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/render-graph-frame-12.json` — 108,149 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/render-graph-frame-13.json` — 108,149 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/render-graph-frame-14.json` — 108,149 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/render-graph-frame-15.json` — 108,149 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/render-graph-frame-16.json` — 108,149 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/render-graph-frame-17.json` — 108,149 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/render-graph-frame-18.json` — 108,149 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/render-graph-frame-19.json` — 108,149 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/render-graph-frame-20.json` — 108,149 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/render-graph-frame-21.json` — 108,149 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/render-graph-frame-22.json` — 108,149 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/render-graph-frame-23.json` — 108,149 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/render-graph-frame-24.json` — 108,149 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/render-graph-frame-25.json` — 108,149 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/render-graph-frame-26.json` — 108,149 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/render-graph-frame-27.json` — 108,149 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/render-graph-frame-28.json` — 108,149 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/render-graph-frame-29.json` — 108,149 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/render-graph-frame-30.json` — 108,149 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/render-graph-summary.json` — 6,388 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/rendering-debugger.json` — 238,753 bytes.
- `Logs/RenderAudit/20261028T193330002Z-capture/runtime-quality.json` — 6,780 bytes.

### `20261028T193612669Z-profiler`

- `Logs/RenderAudit/20261028T193612669Z-profiler/capture-status.json` — 5,765,364 bytes.
- `Logs/RenderAudit/20261028T193612669Z-profiler/profiler-summary.json` — 5,765,364 bytes.
- `Logs/RenderAudit/20261028T193612669Z-profiler/profiler.data` — 126,643,636 bytes.

- Source files and related optimization plans: [[TODOs/Optimization/Battle_Rendering_Performance_Audit#Files]].
