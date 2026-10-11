---
category: Optimization
status: in-progress
created: 2026-10-11
updated: 2026-10-11
---
# Battle CPU performance follow-up

## Goal
- Attribute and reduce remaining main-thread costs after the radar rewrite; preserve combat, movement, visibility and audio behavior.
- Keep separate evidence for sustained cost, occasional stalls, rendering submission and GPU execution.
- Execution order: allocation attribution + target preparation → combat application and squadron ticks → navigation and ship/audio → matched acceptance.

## Decision
- Current baseline: 2026-10-11 15:25 Kyiv, 78 frames / 10.143 s, 4× speed, High Fidelity, 2560×1440; median 123.010 ms / 8.1 FPS.
- Fresh 15:31 sample: 12 frames; median CPU 96.584 ms, GPU 2.965 ms. Short diagnostic sample, not a benchmark.
- Radar: live source now has centralized `RadarScanService`; latest battle marker average 8.581 ms versus historical 656.729 ms. Different workloads/marker coverage → observational improvement, not controlled speedup.
- Fix execution plan: [[TODOs/Optimization/Battle_CPU_Fixes_Plan]].
- Attack implementation remains in [[TODOs/Optimization/Battle_Attack_Optimization_Plan]]; rendering in [[TODOs/Optimization/Battle_Particle_Rendering_Optimization_Plan]].
- Already analyzed: capture comparison, main-thread hotspot ranking, attack/radar source paths; rendering agent verified 949 events, shader/texture groups, squadron indicators and adjacent GPU scopes.
- Not established: exact allocation callers, current audio share, causes of every spike, matched implementation gains or final radar parity.
- Current request: analyze existing captures and related files; skip another battle run. Source review completed at `74368d81`; implementation, raw Profiler replay and runtime acceptance remain pending.

## Rules
- Keep the user's running session in Play Mode. Do not stop it for diagnostics; coordinate any pause/capture with the active request.
- Re-read live source before editing: other agents changed radar during this investigation. Do not reimplement its obsolete per-component scan path.
- One writer per shared combat file and one active Unity capture at a time; record ownership before implementation.
- Record source revision plus dirty diff identity, fleet/squadron/effect counts, camera, resolution, time scale, quality and profiler settings.
- Compare ≥3 representative 10-second runs per variant, with matched workload and separate 1×/4× results; report median/p95/p99 and allocations.
- Nested inclusive/self timings and worker totals are not additive wall time. Frame Debugger events are not draws.
- Follow AGENTS.md test scene safety and asset persistence rules. This planning update runs no tests and changes no game assets.

## Implementation

### Saved-data and source review — 2026-10-11
- [x] Read CPU/attack/rendering plans; verify saved CSV, Profiler JSON and complete historical Frame Debugger against live source.
- [x] Confirm current priorities: target capture 16.459–21.118 ms/frame; due apply 12.010–16.864 ms/frame; squadron Tick 8.469 ms in fresh sample. Values come from different workloads.
- [x] Separate stalls: frame `20777` has 287.191 ms due apply; frame `20800` has 81.268 ms navigation across 35 plans. Navigation attribution must include command callers outside Ship.Tick.
- [x] Preserve existing improvements: centralized radar, retained ordered candidates, counted hardpoint collection, cheaper ring checks, pooled/inactive idle shot effects and 0.5 s squadron aim refresh.
- Remaining candidates: repeated eligibility/position snapshots, fallback reason counters, due-ID lookup, squadron order/steering/pose split, stationary replans and repeated whole-pool audio mixing.
- Capture limitation: summaries omit allocation sizes/ancestry/stacks; Frame Debugger saves only finished frames. Detailed method/line findings and uncertainties: [[TODOs/Optimization/Battle_CPU_Performance_Plan - Research#Source analysis — 2026-10-11]].

### Allocation attribution and GC
- [x] Confirm allocation pressure: latest 3.727 MB/frame average, maximum 9.640 MB; historical 44.275 MB/frame and 266.846 ms GC collection.
- [ ] Inspect existing profiler.data CPU Hierarchy GC Alloc and Timeline before another capture; separate bytes, allocation count, collection time and thread.
- [ ] If caller stacks are absent, enable GC.Alloc call stacks before a short new capture; Deep Profiling off initially. Restore capture settings afterward.
- [ ] Export top allocating stacks with method/file/line, bytes/frame, calls/frame, bytes/call and containing gameplay phase. Separate Editor/tool overhead.
- [ ] Optimize only proven leading sources: repeated temporary data, buffer growth, strings/logging or effect creation according to stacks; do not assume pooling solves all allocations.
- [ ] Verify reduced bytes and collection stalls in matched runs; do not attribute all collected garbage to the scope where GC happens.
- Done gate: ranked allocation report, before/after evidence for selected changes, no gameplay/lifecycle regression.
- Reference: https://docs.unity3d.com/Manual/profiler-cpu-navigating.html ; allocation call stacks affect future frames only.

### Squadron simulation
- [x] Fresh profiler: Squadron.Tick averages 8.469 ms inclusive across 12 frames; earlier audit ~11.35/11.88 ms.
- [x] Trace Squadron.Tick: UpdateOrder → pilot steering → flight Step/death synchronization/pose application. WeaponComponent ticks independently.
- [ ] Measure order acquisition, pilot steering, model integration and pose application separately; do not charge the whole weapon batch to Squadron.Tick.
- [ ] Record live squadrons/craft and active states; identify duplicate/unchanged work before lowering update rates.
- [ ] Validate formation, targeting, member loss, visibility and order transitions; measure per-squadron and total cost.
- Scope excludes visual squadron indicator rendering, owned by the rendering plan.

### Navigation and ship/audio attribution
- [x] Latest navigation average 4.812 ms, maximum 81.268 ms; nested candidate/route/flood costs are 4.678/3.901/2.720 ms average.
- [ ] Record Plan requester/state/reason, retry count, contacts, grid dimensions, candidates and route/flood counts.
- [x] Verify source: stationary pursuit retries and per-Plan grid reconstruction remain; ring candidates already skip edge snapping and reject known unreachable cells.
- [ ] Attribute redundant requests before fixing; preserve nearest-reachable fallback, arrival/deferred commands and reservation ordering.
- [ ] Split Ship.Tick into order/state execution and AudioShipComponent.UpdateAudio. Latest average 7.069 ms; current code calls both under one marker.
- [x] Trace audio: each accepted shot/matching loop renewal can remix all 20 voice slots, plus service Tick; consider one frame-boundary mix only after measuring share and preserving timing.
- [ ] Record SFX requests accepted/refused, loop updates, active sources, voice stealing, source creation and audio allocations. No separate audio capture exists in the reviewed outputs.
- [ ] Measure pooled projectile flight/tracking versus impact completion, muzzle-flash ticking and effect retirement; latest BoltShot marker 8.217 ms/frame.
- [ ] Measure cold versus warm projectile pools: latest 462 creations / 78 frames, 1.325 ms/frame instantiate scope; preserve lease/drain and owner-death behavior.
- Done gate: identify measured child costs, select bounded fixes, verify behavior and matched timings; do not call a broad marker a diagnosed bug.

### Radar acceptance and capture reliability
- [ ] Obtain radar owner's implementation/test evidence; validate 1×/4× scheduling, detection delay, cloak/hostility, destruction, range modifiers and obstacle contacts.
- [ ] Retain historical radar diagnosis in research; current stages average Snapshot 2.189 ms, Job 0.751 ms, Apply 5.392 ms.
- [ ] Repair/reconfigure bounded Frame Debugger export so a complete current battle frame saves; 15:25 capture timed out at event 799 with zero saved frames.
- [ ] Investigate unreadable Rendering Debugger camera selector separately; partial panel export is not gameplay failure.
- [ ] Final acceptance: matched sustained and spike metrics, no new errors, full relevant behavior checks; confirm major gains in a Development Player.

## Files
- [[TODOs/Optimization/Battle_CPU_Performance_Plan - Research]]
- `Assets/Scripts/Services/Timing/BattlePerformanceCapture.cs`
- `Assets/Scripts/Editor/RenderAuditProfilerTool.cs`
- `Assets/Scripts/Editor/RenderAuditFrameDebuggerTool.cs`
- `Assets/Scripts/Services/Radar/RadarScanService.cs`
- `Assets/Scripts/Services/ShipNavigation/ShipNavigationService.cs`
- `Assets/Scripts/Entities/Ship/Ship.cs`
- `Assets/Scripts/Components/Ship/Audio/AudioShipComponent.cs`
- `Assets/Scripts/Services/Audio/ShipSfxService.cs`
- `Assets/Scripts/Components/Weapon/WeaponComponent.cs`
- `Assets/Scripts/Components/Weapon/TargetSelectionBatch.cs`
- `Assets/Scripts/Components/Weapon/CombatAttackCoordinator.cs`
- `Assets/Scripts/Entities/Squadron/Squadron.cs`
- `Assets/Scripts/Services/ShipNavigation/ShipPathGrid.cs`
- `Assets/Scripts/Components/ViewComponents/Weapon/ShotEffectPool.cs`
- `Assets/Scripts/Components/ViewComponents/Weapon/BoltShot.cs`
