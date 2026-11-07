---
type: engineering-reference
system:
  - optimization
  - addressables
  - rendering
  - vfx
verified: 2026-11-06
verification: Synthesized from the 18 optimization plans and 11 research
  companions indexed in the project cookbook. Procedures checked against
  content-build history, Unity asset inspection and battle captures.
tags:
  - optimization
---
# Unity optimization cookbook

## Rules

- Write down the symptom, the unit, the scenario and what would count as success. Give frame latency, download bytes, installed bytes, resident memory and first-use latency separate acceptance criteria.
- Preserve the player-visible contract before changing cost. Record reference views, gameplay timing, input latency and effect lifetime. Keep an importer rollback record and restore candidates that fail a gate.
- Record revision, content catalog, platform/API, build type, hardware, preset, display and camera dimensions, internal render scale, AA, frame cap and VSync. Record seed, camera pose, simulation speed, actors, active effects and cold/warm state.

## Implementation

### Reduce a download or install

1. Build the same target and content revision before and after. Retain the catalog, build layout, bundle file sizes and Player build report. Separate built-in scenes and Resources from Addressables.
2. Follow a real entry point such as opening the menu or spawning a unit. List its explicit entries, transitive dependencies and bundle closure. Check duplicate objects and redundant payload separately from the number of shared paths.
3. Group by when assets must load and release together. Move shared dependencies only when the layout demonstrates a benefit. For optional catalog entries, use a reference that can load on demand and verify the caller releases its handle.
4. For identical source files, compare importer settings, alpha, color space, wrap, GUID consumers and aliases before consolidating. Rebuild. Compare the cold first-use path, hot reuse and memory after exit.
5. If bytes are concentrated in textures, inspect the resolved platform format, dimensions, mip chain and CPU readability. Confirm the asset actually appears in the packed layout. Measure CPU-copy savings in the Player; Editor memory can retain copies. Check the final packed atlas when auditing sprites.
6. Pilot hull color, packed masks, normals, thin UI, additive glow and world sprites separately. Compare alpha edges, decoded normals, team masks and closest zoom. For meshes, screen positional/UV/normal error and check consumers of CPU geometry. For audio, audition first play, loops and crossfades under load and measure DSP cost.

### Diagnose low FPS or a hitch

1. Split steady-state frame time from outliers. Report frame interval median, p95, p99, maximum, hitch count and sample length. Preserve the frames around a hitch and identify capture/export stalls.
2. Compare the same workload with one variable changed, then restore it. Repeat each variant. Use an unprofiled timing run for the result and a Profiler timeline, Frame Debugger or GPU trace for attribution.
3. On CPU, follow snapshot preparation → scheduling → wait → application. Count work items, searches, allocations and creations. Check which thread actually ran the work. Publish job results after completion and validate owner/generation before use.
4. For spawn spikes, distinguish asset I/O from object creation, initialization and registration. Measure the consumer’s exact typed key: a GUID and a friendly alias can occupy different cache slots for the same prefab. Warm only the measured working set. Cap work per frame and time individual creations that can exceed a whole-frame budget.
5. On GPU, isolate geometry, transparent draws, pixel coverage, shadows and post-processing at a fixed camera. Cull conservative transformed bounds and preserve edge visibility. On UI, measure rebuilds by canvas/update rate and check input, stacking and resolution changes.
6. Repeat material improvements in a Development Player on each target. Preserve raw wall-clock intervals. Worker, parent/child and parallel GPU timings can overlap.

### Reduce VFX cost without losing combat information

1. Count live instances and particles by family. Separate enabled, emitting, visible, off-camera and draining effects. Record render mode, shared material, effective blend/queue, depth/fade and runtime overrides.
2. Isolate one family in a fixed firing view. Measure simulation and geometry, render-thread submission, drawn pixels and GPU pass time. Record configured capacity, actual particle population and bounds intersecting the camera as separate counts. Time the GPU pass directly.
3. Choose the experiment that matches the cost. For simulation, vary rate or lifetime. For submission, group compatible draws. For fill, reduce overlapping screen coverage. Check tint, alpha, UV wrapping, depth intersections and ordering on both bright and dark backgrounds.
4. Preserve shot count, timing, hits/misses, faction colors and screen readability. Author which looping emitters may be suppressed; keep the owner’s lease alive when suppression leaves zero particles. Let stopped effects simulate until they drain. Verify fog reveal, camera return, reuse, owner death and scene exit.
5. Keep the change when the timing or byte gain exceeds run variation and the visual/lifecycle checks pass. Record rejected experiments and exceptions beside the accepted result.

## Files

- Project recipes, numeric gates and original plans → [[Rules/EMPIRE_AT_WAR_OPTIMIZATION_COOKBOOK]].
- Worked audit of assets, live particle populations and frame evidence → [[Artifacts/Audits/Optimization_2026-11-06/Optimization_Audit]].
- Unity references → [Addressables build layout](https://docs.unity3d.com/Packages/com.unity.addressables@2.9/manual/BuildLayoutReport.html) · [texture platform settings](https://docs.unity3d.com/Manual/class-TextureImporter-type-specific.html).
