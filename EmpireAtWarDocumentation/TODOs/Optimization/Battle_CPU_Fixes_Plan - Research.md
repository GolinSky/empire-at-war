---
category: Optimization
status: in-progress
created: 2026-10-11
updated: 2026-10-11
---
# Battle CPU fixes — acceptance audit

## Goal
- Audit the existing battle without stopping, pausing, stepping, reloading scenes or recompiling project scripts.
- Supply Phase 8 evidence; keep acceptance open until matched performance, behavior and Development Player gates pass.

## Important Values
- Date: 2026-10-11. Display times use Kyiv (UTC+3); artifact names use UTC.
- Editor: `6000.4.7f1`, PID `30728`, project `F:/Private/empire-at-war`; connected through official `unity` CLI.
- Capture: `battle_20261011_141152_737`, 17:11:52–17:12:02, 91 frames / 10.047 s; completed normally.
- Settings: **1×**, High Fidelity, 2560×1440, VSync 0, unlimited target FPS; RTX 5070 / Ryzen 7 9800X3D.
- Source: `5e883b707cead56b208c32dc1e6bfca4dd3864aa`, dirty. Saved `Assets/Scripts`, `Packages`, `ProjectSettings` diff SHA256: `A06B2B00BF79FCCBA03251942D98DF33F30D1EF3FA024566FE918A92B52A94EB`.
- Existing Phase 0 comparison is **4×** (`battle_20261011_122536_354`); different source, battle population and speed → no causal speedup claim.
- Earlier same-revision 1× capture `battle_20261011_140628_387`: 124 frames / 10.052 s, CSV median 79.217 ms. Later battle grew from ~109.5 to 120 squadron ticks/frame; these are different workloads.

| 10-second capture metric | Median | p95 | p99 / maximum | Mean |
| --- | ---: | ---: | ---: | ---: |
| Frame duration | 104.981 ms | 194.972 ms | 227.891 ms | 110.071 ms |
| CPU main thread, includes waits | 86.004 ms | 121.545 ms | 173.604 ms | 89.324 ms |
| Managed allocation recorder | 246,147 B/frame | 5,529,998 | 8,548,964 | 1,365,793 |

- Median frame duration ≈9.5 FPS. Frame duration and CPU-counter duration are distinct; do not attribute their difference to one unmeasured subsystem.
- GPU recorder: 74/91 frames available, mean 0.617 ms, maximum 29.172 ms; GPU timing can arrive late. Editor measurements are not a Player GPU benchmark.
- Quantiles in `analysis.json`: arithmetic median and nearest-rank p95/p99. Older tool summaries select a lower middle sample for even frame counts; do not silently mix those median definitions.

## Implementation

### Captures and preservation
- Saved the existing Profiler buffer before collecting more data: `Logs/BattleAudit/20261011T1412Z/existing-profiler.data` (489,484,992 bytes).
- Invoked `Tools/Empire At War/Diagnostics/Performance/Capture Battle (10 Seconds)`; copied CSV and summary into the audit directory.
- Fresh Profiler: `Logs/RenderAudit/20261011T141224307Z-profiler/`, 60/60 requested frames, indices 270–329, `.data` 730,065,700 bytes. `profileEditor=false`, Deep Profiling off.
- Read retained main-thread samples incrementally through an ephemeral script outside `Assets`; no profile reload or project compilation. Full marker and allocation-parent results: `raw-main-thread.json`.
- Fresh Render Graph: `Logs/RenderAudit/20261011T141310347Z-render-graph/`, two completed frames, Main Camera, 20 passes, 36 textures and one buffer in inspected frame; zero culled passes.
- Fresh Rendering Debugger: `Logs/RenderAudit/20261011T141312153Z-rendering-debugger/`, six panels, zero unreadable values.
- Frame Debugger: reused the completed pre-existing `Logs/RenderAudit/20261011T140658863Z-capture/frame-debugger-frame-01.json`, Unity frame 13501, 888/888 events, no truncation.
- Fresh Frame Debugger capture was deliberately not started: `RenderAuditFrameDebuggerTool.CaptureOperation.Start` sets `EditorApplication.isPaused=true` and steps frames. It conflicts with the active no-interruption constraint.
- Final readback: Play Mode active, unpaused, frame advanced to 21002, time scale 1; Profiler recording and Frame Debugger disabled, matching initial settings. No gameplay code/assets modified; no Unity tests/build run.

### CPU attribution

| Scope | 10 s mean ms/frame | Fresh 60-frame mean ms/frame | Interpretation |
| --- | ---: | ---: | --- |
| Target snapshot capture | 22.789 | 20.933 | Leading sustained measured gameplay cost |
| Target prepare | 1.960 | 1.805 | Native-input preparation remains measurable |
| Squadron UpdateOrder | 10.725 | 11.462 | Leading squadron child |
| Squadron pilot | 0.205 | 0.205 | Small relative to order work |
| Squadron flight | 1.027 | 1.064 | Secondary squadron cost |
| Projectile update | 7.865 | 10.255 | `BoltShot`/effect update; marker name says TurretUpdate |
| Ship order tick | 4.640 | 5.512 | Separate from ship audio |
| Ship audio | 0.318 | 0.290 | Small in this workload |
| Due apply | 6.109 | 5.542 | Includes sort/lookup/emission/impact |
| Due lookup | 0.009 | 0.009 | No longer a leading measured cost |
| Due emission | 1.283 | 1.160 | Smaller than impact work |
| Due impact | 4.781 | 4.336 | Dominates due apply; fresh maximum 32.418 ms |
| SFX ApplyMix | 0.007 | 0.007 | Exactly 91 calls / 91 battle frames |
| Navigation Plan | 1.836 | 0.990 | Bursty; 10 s maximum 78.390 ms |
| Radar Scan | 1.919 | 2.441 | Includes snapshot/job/apply |

- Nested scopes overlap; do not add parent and child rows.
- Profiler CPU total: median 96.303 ms, p95 122.380, maximum 256.544; main counter median 89.655 ms. EditorLoop mean self 6.601 ms.
- Reported Profiler GPU median 0.081 ms; low Editor GPU counters and expensive gameplay scopes support CPU-first investigation, not a precise shipping GPU claim.
- Target workload: 17,314 requests, 4,760,237 candidate records (~52,310/frame), no target-buffer growth. Candidate records are not unique targets.
- Fallbacks: 87 events / 17,314 requests (0.502 events/100 requests): 86 range-boundary, one yaw-boundary, zero parent-tilt/version/invalid/out-of-span events. This does not prove randomized targeting parity.
- Due workload: 56,170 inspected records, 4,610 flagged, 4,608 committed. Stale/cancelled validation can explain noncommits; counts alone do not prove correctness.
- Navigation: 67 Plan calls, 44 grid builds, zero stationary-reuse marker calls. PreserveCourse maximum 78.510 ms → measure caller/contact/reservation invalidations; this run does not exercise the stationary-cache success gate.
- Projectile pools: 2,861 acquisitions, 25 Instantiate calls; zero target/due native-buffer growth. This does not prove Phase 7 overlap capacity.
- Live source: `TargetSelectionBatch.CaptureTargetSelections` already shares spans per weapon owner; investigate remaining candidate acquisition/validation/Transform cost before another redesign.
- Live source: `Squadron.UpdateOrder` routes to Hunt/Guard/AttackMove/idle logic; `SquadronTargetSelector.SelectNear/SelectSeen` scan candidates/entities. Attribute order type and candidate count before changing cadence.

### Allocations and stalls
- All 60 retained main-thread frames: **60,721,656 bytes**, **1,357,731 allocation samples**; mean 1,012,028 B/frame, median 281,189, p95 4,429,358, maximum 5,991,459.
- Different capture/time/thread scope from the battle allocation recorder → totals need not match.

| Immediate Profiler parent | Bytes | Share | Allocation calls |
| --- | ---: | ---: | ---: |
| DueBatch.Impact | 41,050,152 | 67.6% | 967,508 |
| Radar.Apply | 12,934,064 | 21.3% | 250,613 |
| EnemyUnitCommander.Tick | 2,067,852 | 3.4% | 48,938 |
| ShipAbilityService.Tick | 1,784,900 | 2.9% | 42,181 |

- These are enclosing Profiler scopes, not exact allocating functions or source lines; uninstrumented descendants remain inside each parent.
- Checked first 100 GC.Alloc samples per frame: **0/6,000** had call stacks. Enable GC.Alloc stacks for a separate short attribution capture; keep Deep Profiling off and restore settings.
- No managed `GC.Collect` marker found in the 60 retained main-thread frames. This short observation does not satisfy a GC-stall acceptance gate.
- Battle frame 17142: 227.891 ms frame duration, 109.394 ms main counter, 8,548,964 allocated bytes, impact 21.438 ms. Do not claim GC caused the whole frame.
- Navigation maximum remains 78.390 ms; distinguish route/flood work from cheap grid construction (mean 0.002 ms/frame).

### Rendering and logs
- Fresh Profiler means: 2,195.6 draws, 1,329.1 SetPass, 5.107 M triangles, 7.116 M vertices; BRG 490.4 draws / 2,662.4 instances. Editor-wide counters can include Editor rendering; unavailable batch counters are not zero.
- Existing Frame Debugger: transparent stage **772 events / 1,150 draws**; opaque 9 / 124; main shadows 6 / 120. Events and draws are different quantities.
- Shaders: URP Particles/Unlit 547 events / **879 draws**; Ion Glow 52 / 67; URP Unlit 69 / 88. Squadron Frame/Icon 28/27 draws.
- Batch-break reasons: 527 different-material events, 93 SRP-incompatible-node events. These identify submission candidates; no shader change or controlled rendering improvement is established.
- Earlier Frame Debugger and fresh Profiler/Render Graph are different frames. Do not compare draw totals as a before/after result; the graph also reflects a different moment/settings state.
- Initial retained log window: 1,000 entries, including **892** `UnityEditor.FrameDebuggerWindow.DrawEnabledFrameDebugger` NullReferenceExceptions from 17:08:36–17:09:04. These predate this audit and belong to the Editor debugger window.
- After capture cursor 35640: 209 subsequent entries through cursor 35849, **zero warnings/errors**. Preserved console JSON and Editor.log tail; console was never cleared.
- Earlier Rendering Debugger had a destroyed `Volume -> Camera` selector; current six-panel export has no value errors.

## Decision
- Phase 8 remains **pending**. This audit establishes current attribution, not matched acceptance or behavioral parity.
- Priority for follow-up measurement: target snapshot CPU → squadron order CPU → impact/radar allocating stacks → projectile update → navigation spikes.
- Keep lookup/audio implementations under behavior checks; measurements do not justify another performance rewrite there.
- Keep rendering submission findings in the rendering/VFX workstream; CPU fixes plan excludes rendering changes.
- Phase 7 remains unimplemented in live source: `AttackSequenceState.WaitingForEffects`, effect registration/completion and `ShotsPerSalvo` idle cap remain. Complete Phase 7 before whole-plan sign-off, or explicitly accept only phases 0–6.

## TODO
1. Freeze before/after revisions and dirty-diff identities; define a repeatable battle, camera, fleets, squadrons, effects, quality/resolution and profiler settings. Current `scenario=unspecified` is insufficient.
2. Run ≥3 matched 10-second samples per variant at 1× and separately at 4×. Report CPU/frame median/p95/p99, B/frame, GC collections/stalls and workload-normalized costs; keep Frame Debugger disabled during timing.
3. Capture allocation stacks for impact and radar; split squadron order type/selection and target snapshot work. Preserve the current no-pause session; schedule tests/scenario resets separately.
4. Verify target priority and yaw/range/tilt edges; shot counts/impact order and timing; owner/hardpoint/target death and cancellation; effect drain; pause behavior; first-frame audio/ducking; navigation/reservations/formation/member loss.
5. For Phase 7, verify salvo/reload fire rate, no stuck busy hardpoints and warmed overlapping-salvo pool high-water/Instantiate/Destroy behavior.
6. Confirm largest gains in a Development Player with matched settings; then record evidence and close Phase 8. Do not mark completion from compilation or this observation alone.

## Files
- [[TODOs/Optimization/Battle_CPU_Fixes_Plan]]
- [[TODOs/Optimization/Battle_CPU_Performance_Plan - Research]] — earlier baseline and source analysis.
- `F:/Private/empire-at-war/Logs/BattleAudit/20261011T1412Z/analysis.json` — all CSV statistics, Profiler counters/hotspots, raw allocation ranking, Frame Debugger groups and capture state.
- Same directory: `raw-main-thread.json`, `existing-profiler.data`, battle CSV/summary, console snapshots, Editor.log tail, `source-diff.patch`, `session-before.json`, `session-after.json`.
- Fresh Profiler/Render Graph/Rendering Debugger and pre-existing complete Frame Debugger paths are recorded under Captures and preservation.
