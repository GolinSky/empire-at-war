# Live battle CPU audit — 2026-10-29

## Outcome

- Severe CPU-side frame pressure reproduced in the user's existing match. No gameplay changes or tests. No Play Mode stop, pause, scene reload, camera pinning or rendering ablation commands were issued.
- Main 10-second CSV: **264.521 ms conventional median / 259.927 ms mean / 411.922 ms p95 / 437.158 ms maximum**, approximately **3.8 FPS** at median pacing. The recorder's nearest-rank median is **256.985 ms / 3.9 FPS**; its summary and Python's conventional median differ for the 38-frame sample. Both original results are preserved.
- Priorities supported by evidence: expensive individual shot prewarm creations; squadron order work; target snapshot copying; capture-site spikes; telemetry/hazard/AI allocations. These are investigation targets, not implemented fixes or proven isolated speedups.

## Setup

- Source: `21d64a927629da94d8bc8ca310b48e978a983233`; `main`. Initial dirty path: `EmpireAtWarDocumentation/.obsidian/workspace.json` (untouched and excluded from commit).
- Unity `6000.4.7f1`, Apple M2, Metal, Editor, Performant, time scale 1, VSync 0, target FPS -1. Deep Profile disabled. Regular Profiler disabled before capture and restored afterward.
- Main CSV start: `2026-10-29T21:37:12.952Z` (23:37:12 Europe/Bucharest), battle time 2250.288 s; completed gameplay frames 105416–105453.
- Post-Profiler census, `21:38:52Z`: 83 ships, 321 squadrons, 454 weapon components. CSV mean invocation counts: 84.74 ship ticks and 318.21 squadron order updates per frame; invocation counts are not independent population censuses.
- Camera census: Main Camera 2560×1440, FOV 29.8755°, position (-684.78,1175,2552.26), rotation (55,0,0). Recorder Screen resolution 642×1156 differs from camera resolution; do not treat it as actual rendering resolution.
- Later scenario snapshot: seed `1875278133`, map type `Full`, nebula variant `2`. Metadata snapshots were taken sequentially; camera/workload were not pinned throughout the evolving battle.
- Unity CLI updated from `1.0.0-beta.14` to `1.0.0-beta.15` during collection; no Editor/package update or recompile requested.

## Gameplay timing — Profiler disabled

Primary evidence: `battle_20261029_213712_952.csv`, 38 frames / 10.024 s. Values below are means except where labeled; inclusive parent/child scopes overlap.

| Metric | Mean | p95 | Interpretation |
| --- | ---: | ---: | --- |
| CPU main | 202.655 ms | 332.308 ms | Includes waits; does not alone measure useful execution |
| Render thread | 7.472 ms | 11.888 ms | Includes waits |
| GPU counter | 18.482 ms | 20.662 ms | Delayed Editor counter; no isolated GPU benchmark |
| Target batch | 27.033 ms | 66.934 ms | Includes snapshot, application and nested work |
| Target snapshot | 15.862 ms | 26.972 ms | Main-thread preparation |
| Target dispatch + completion scope | 0.232 ms | 0.506 ms | Not worker execution duration |
| Target apply | 9.732 ms | 43.996 ms | Nested attack initiation work |
| Due apply | 13.406 ms | 42.199 ms | Includes emission/impact work |
| Projectile GetOrCreate | 13.607 ms | 42.433 ms | Includes instantiation |
| Projectile Instantiate | 12.158 ms | 40.997 ms | 104 calls across 38 frames |
| Squadron orders | 16.696 ms | 32.959 ms | Separate from pilot/flight |
| Squadron pilot / flight | 3.442 / 1.932 ms | 5.621 / 2.805 ms | Separate child costs |
| Radar scan | 10.749 ms | 29.826 ms | Snapshot mean 6.681 ms |
| Navigation plan | 1.008 ms | 3.673 ms | Maximum 3.677 ms in this sample |
| Allocations | 506,009 B/frame | 5,654,737 B/frame | Median 153,544.5 B; collection time not established |

- Mean workload: 2,831 target requests, 14,656 candidate records, 557 pending impacts per frame. Zero target/due buffer growth. Candidate records are not unique enemies.
- Mean SetPass calls 1,155; triangles 1.356 million. CSV draw/batch counters unavailable; separate live snapshot and Profiler rendering identities preserve rendering statistics.
- An earlier eval timed out but executed later: `battle_20261029_213652_704.csv` records 18 frames and a 4.138 s outlier. Retained as contaminated diagnostic evidence, excluded from headline pacing. Do not infer a gameplay root cause from that outlier.
- Historical captures use different hardware, source, speed, population and camera conditions. No matched regression or speedup comparison is claimed.

## CPU attribution — regular Profiler

- Capture `Logs/RenderAudit/20261029T213731043Z-profiler`, indices **525–614**, 90 frames, profileEditor=false. CPU main median 154.83 ms, p95 321.38 ms; GPU counter median 18.06 ms. This later instrumented window is not the CSV benchmark.
- Main-thread self-time means over exported top-40 lists: `Instantiate.Awake` 31.286 ms, `Battle.Squadron.UpdateOrder` 19.529 ms, capture-site Tick 12.629 ms, target snapshot Copy 10.086 ms, enemy commander Tick 5.851 ms, radar Snapshot 4.903 ms. Omitted markers make aggregates lower bounds.
- Raw ancestry decoder: **91 Instantiate.Awake calls / 2,452.0 ms inside ShotPoolPrewarmer.Tick** → 27.244 ms/frame across this window. Another 103 calls / 318.1 ms occur inside `Battle.Projectile.Instantiate`. These costs are nested in other scopes; do not add them again.
- Live source `Assets/Scripts/Components/ViewComponents/Weapon/ShotPoolPrewarmer.cs`: `BUDGET_MILLISECONDS = 0.5`, checked between `pool.PrewarmOne(target)` calls. One creation can exceed the entire budget; the loop cannot interrupt an expensive Awake. This is a concrete follow-up, not proof of a leak or that prewarming should be removed.
- Capture-site Tick maximum inclusive 283.327 ms. Its source performs strength sampling, per-site capture, visibility, render and hover work. Child/caller attribution for this spike remains open; do not assume GC caused it.
- Significant diagnostic/Editor time: EditorLoop mean self 59.624 ms; `Profiler.FlushMemoryCounters` 12.817 ms. Worker idle/wait durations are not extra CPU frame cost.
- Native `sample` evidence was collected **during Profiler export**, not steady gameplay. It catches `FrameDataView.GetMarkers`/managed string allocation on the main thread. Export walks each captured frame/thread synchronously; its stall is tool overhead. No stop/pause command was used, but diagnostics did introduce overhead and an export stall.

## Allocation attribution

- Separate capture `Logs/RenderAudit/20261029T213912116Z-profiler`, indices **617–640**, 24 frames with GCAlloc stacks; Deep Profile disabled. Instrumented CPU main median 241.88 ms is not an unprofiled baseline.
- Main-thread decoder: **4,239,756 bytes / 80,730 allocation calls** → 176,656.5 B/frame (172.52 KiB/frame). Frame 629 carries about 1,498 KiB. Includes Editor/capture work; excludes other threads.
- `allocation-stacks.json`: top first-resolved methods. `allocation-full-stacks.json`: top complete ancestry groups (up to ten resolved frames). Groups are alternative views of the same allocations; never add totals across these reports.
- Leading complete-stack groups: `BattleTelemetry.Flush` direct 479,488 B, its String.Format/ToString 289,904 B, builder expansion 265,312 B, final ToString 261,708 B, enum formatting 83,540 B. These disjoint groups alone attribute **at least 1,379,952 B** to telemetry flush ancestry.
- `HazardService.RefreshMemberships`: 464,136 B / 11,581 calls. Source has per-entity facade queries and membership iteration; exact allocation expressions need follow-up before changing them.
- Enemy evaluation: `GetIonCost` 193,640 B in first-method grouping; build queues 179,600 B; context Build 122,840 B. Shield getters, hardpoint collection and navigation sample arrays also appear in saved rankings.
- Diagnostic allocation visible: RenderAudit rendering stats 30,544 B; GUIStyle mesh allocation 99,040 B has `EnemyEconomyDebugView.OnGUI` ancestry. Preserve this distinction from release gameplay cost.
- GC stack recording restored from `GCAlloc` to original **None**, confirmed `2026-10-29T21:39:49.135Z`, while playing=true and paused=false. Absence of GC.Collect in top-40 regular summaries does not establish no collections.

## Other tools and coverage limits

- **Project Auditor:** requested ProjectSetting only, scan `8bb38602`. Status says completed / zero issues, but Console explicitly reports **“Could not find any registered modules.”** Treat as **no usable static coverage**, never a clean audit. No code compilation scan or package install attempted during the match. Generated defaults are preserved as `project-auditor-generated-settings.txt`; their newly created ProjectSettings file was removed after capture.
- **Rendering Debugger:** six panels saved; **partial**, stale destroyed camera in `Volume -> Camera`. Do not interpret missing camera values as valid settings.
- **Render Graph:** two valid executions captured at gameplay frames 105905–105906; complete pass/resource JSON preserved compressed.
- **Console:** retained errors are Pipeline diagnostic request timeouts; warning above explains Auditor coverage. Retained history is not a measured error rate or proof that no gameplay errors exist.
- Frame Debugger and rendering ablation were not run: freezing a frame or pinning/altering the camera would interfere with the ongoing match. No Player launch, build, tests, scene save/reload, gameplay/settings tuning or optimization was performed.

## Evidence and follow-up

- This folder commits both CSVs and recorder summaries, computed metrics, raw per-thread Profiler summaries (gzip), decoder inputs/results, allocation ancestry, Render Graph and Rendering Debugger exports, console/state snapshots and a raw-artifact SHA-256 manifest.
- Large native `profiler.data` recordings remain saved locally under the two `Logs/RenderAudit` capture folders; the manifest records exact paths, sizes and hashes. Git contains portable decoded evidence, not the large binary recordings.
- Next investigations, by measured priority: individual prewarm Awake cost and growing warm demand; squadron order cadence; snapshot Copy; capture-site spike descendants; telemetry flush and hazard/AI allocations. Confirm improvements with matched workload and target Player captures before claiming savings.
- Broader CPU optimization remains active in vault `TODOs/Optimization/Battle_CPU_Performance_Plan`; this task completes evidence collection only. No reusable production rule changed: results are workload-specific; existing performance measurement rules remain applicable.
