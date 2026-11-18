---
category: Tooling
status: done
created: 2026-11-18
completed: 2026-11-18
acceptance: pending-user
---
# Battle Hitch and GC Attribution Instruments

## Goal
- Every frame > 50 ms and every GC-heavy frame in an ordinary playtest gets a named cause automatically: no live capture, no luck.
- Per-system managed allocation (bytes/frame) and memory growth are recorded during normal play.
- Scope: measurement tooling only. No gameplay or performance fixes in this plan.

## Why the two biggest problems are still unattributed (playtest 2026-11-18)
- Evidence: `Logs/Playtests/20261118T1013Z/telemetry/hitch_87d9cd81ee954b8995acc5e89400c902.csv` (614 hitch frames), `fps_history_editor.csv`, `Logs/BattleAudit/20261118T1050Z/analysis.md`.
- **Large hitch:** 57 frames > 200 ms (≈ 300–380 ms), one every ~35 s (gaps 30 → 41 s, growing as FPS drops). Not on the 60 s window boundary. In these frames the recorded main-thread time is **5–29 ms** and GPU 1–2 ms → ≈ 300 ms is spent **outside every recorded scope**.
  - Hitch CSV columns cover only custom `Battle.*` scopes plus `FrameTimingManager` totals; no Unity built-in markers (`GC.Collect`, `PlayerLoop` phases, `EditorLoop`, loading, physics, present/vsync waits) and no GC collection count.
  - Native Profiler captures are on demand, 90–120 frames (~2 s): a 35 s periodic event is caught only by chance; nothing saves the Profiler buffer when a hitch happens.
  - One different hitch exists: frame 80611, `ship_order_tick` 326 ms (real game code, single event).
- **GC:** 189 KB/frame mean, 117 frames > 1 MB, peak 17 MB/frame. Hitch CSV has only `gc_allocated_bytes` per frame (total). Allocation call stacks exist only in the manual procedure (`Tools/BattleAudit/README.md` §4: GCAlloc mode, 40–80 frames) and were never on during a playtest.
- **Memory:** 3.3 → 4.3 GB in 31 min; no snapshots; `com.unity.memoryprofiler` is not installed; FPS history stores process memory only.
- **Setting:** `ProjectSettings.asset` `gcIncremental: 0` → every collection is a full, blocking GC.
- All evidence is Editor-only: Editor-loop work is inside `frame_duration_ms` and cannot be separated today.

## Hypotheses to confirm or reject (do not fix here)
- H1: full blocking GC every ~35 s (non-incremental GC; the period grows as the allocation rate falls with FPS).
- H2: periodic Editor-side work (Editor loop, asset/refresh, inspector, MCP/heartbeat), absent in a Player.
- H3: a single synchronous route plan in `ship_order_tick` (frame 80611, 326 ms).

## Implementation
- Scope confirmed 2026-11-18: all seven tooling phases implemented. User deferred tests, replay captures, baselines and empirical verdicts.
- [x] Phase 1: available-marker inventory; built-in timing/counters; GC collections; uncovered time; periodic-hitch analysis.
- [x] Phase 2: completed-frame lifecycle time/bytes, top-five columns, capture/system totals, numeric attribution overhead, Update remainder. Editor/Development only.
- [x] Phase 3: bounded native flight recorder, 200 ms default, ≥300-frame history, hitch−60/+10 derived reports, automatic top-marker callers and Markdown.
- [x] Phase 4: allocation sweep (5 min default), 60 frames every 30 s, 14 resolved stack frames, exhaustive game-caller ranking, GC mode cleanup.
- [x] Phase 5: Memory Profiler 1.1.12 pinned/installed; history heap/collections; scheduled 5/30-minute snapshots; automatic shallow type/direct-owner export and diff after user exits Play Mode.
- [x] Phase 6: same runtime counters in Development builds; offline Editor/Player hitch size/period comparison with explicit unverified classification.
- [x] Phase 7: repeatable 4-minute shared-folder run (3 min hitches + 1 min allocation), matching CSV/history archive, automatic analysis report, operating guide.
- Native `SaveProfile(string)` saves the entire rolling buffer; derived JSON/caller trees select the target range. Missing early context is bounded by available history.
- Memory output covers native/managed shallow object bytes and direct-referrer type sets; reserved/untracked/graphics memory and exclusive retained ownership require full `.snap` inspection.
- Short runs do not include a 30-minute memory pair. Schedule that separate replay into the same directory, then regenerate the report after export.
- Live CSV archive may omit pending writer batches; history windows can straddle boundaries. Re-import after battle teardown for complete context.

## Important Values
- Historical playtest: 57 frames >200 ms, typical 300–380 ms, period ~35 s; recorded main-thread 5–29 ms and GPU 1–2 ms.
- Historical GC: mean 189 KB/frame; 117 frames >1 MB; peak 17 MB/frame. Process memory 3.3→4.3 GB in 31 min.
- `gcIncremental: 0` remains unchanged. H1/H2/H3 remain unconfirmed; instrumentation is not a performance fix.
- Rolling trigger: duration >50 ms OR GC allocation ≥1,000,000 bytes; `is_hitch=1` identifies either condition.

## Verification
- Unity 6000.4.7f1 shared compilation completed: 0 errors, 0 compilation warnings. New tool types confirmed loaded after AssetDatabase refresh; no C# compilation/import errors. Unity CLI compatibility probes logged two unsupported console-status command entries.
- Python AST syntax inspection and scoped Git whitespace checks completed. No Python, EditMode, Player or replay tests run.
- Updated `BattlePerformanceCaptureReportTests`; added `BattleSystemAttributionTests`, `BattleMemorySnapshotExporterTests` (`Core`) and `Tools/BattleAudit/test_battle_attribution.py` with synthetic CSV.
- Reviewed `VContainerCompositionTests`, `PostTickLifecycleTests`, `LifecycleContractTests`, `EntityFactoryLifecycleTests`: ordering, constructors, gates and disposal unchanged.
- Reusable guidance → [[Tooling/Battle_Attribution_Instruments]]; repository procedure → `Tools/BattleAudit/Attribution.md`, `Tools/BattleAudit/README.md`.

## TODO
- User-only acceptance is queued in [[TODOs/User_Acceptance_Queue#Battle Hitch and GC Attribution Instruments]]: tests; ≥80% named hitch/alloc coverage; native snapshot; allocating top10; memory-growth ownership; matched Editor/Player runs.
- New baseline deferred in [[TODOs/Optimization/Live_Battle_CPU_Sweep_Plan]]. Do not replace historical evidence with unrun results.

## Comparison saves (2026-11-18, same battle)
- `Logs/Playtests/20261118T1013Z/saves/fps-mid-game.json`, `fps-late-game.json`, `fps-late-game-p2.json` (also in `%USERPROFILE%/AppData/LocalLow/GolinSkyCompany/EmpireAtWar/Saves/Skirmish/`).

## Verdicts from the first attribution run (2026-11-18 13:09, `Logs/BattleAudit/20261118T130915435Z-attribution/`)
- **H1 confirmed:** frames 9333 and 10117 spend 295.7 ms and 288.0 ms in `GC.Collect` inside PlayerLoop 315 / 327 ms → the ~300 ms hitch is a blocking full GC (`gcIncremental: 0`). Fix owner: [[TODOs/Optimization/Live_Battle_CPU_Sweep_Plan]] next-step 2.
- Frames with 4.1–4.5 s `GC.Collect` and 17 s `EditorLoop` are Profiler snapshot saves (instrument overhead); exclude them.
- Tool follow-ups (small, tooling owner):
  - `uncovered_ms` is negative in most Editor hitch frames (−17 s … −20 ms) → in the Editor `EditorLoop` already contains `PlayerLoop`; use `frame − EditorLoop` in the Editor and `frame − PlayerLoop` in a Player.
  - `battle_20261118_131216_070_summary.txt` has 12 frames in 103 s — check why the second CSV recorded almost nothing.
- H2/H3 still open; the allocation ranking (who allocates the bytes that trigger the GC) is the next read.

## Agent handoff
- Read `Tools/BattleAudit/README.md` first; reuse `BattlePerformanceCapture`, the rolling hitch capture and `battle_audit.py` instead of new parallel tools.
- No gameplay/performance fixes in this plan; record findings in the owning optimization plan.
- Never start, stop or pause Play Mode; the user owns it. Restore GC call-stack recording after every use.
- No tests run (local override); add EditMode tests for CSV column/header changes and the decoder parser.
- Implemented tools/columns are reviewable in source. Sample outputs and H1/H2/H3 verdicts require the deferred replay; no synthetic output is presented as evidence.

## Tests (not run — local override)
- `Core`: `BattlePerformanceCapture` header/column tests, hitch window tests; Python: `battle_audit.py` parsing of new columns (add a fixture CSV).

## Files
- `Assets/Scripts/Services/Timing/BattlePerformanceCapture.cs`, `BattleProfilerMarkers.cs`
- `Assets/Scripts/Services/Analytics/BattleFpsHistory.cs`
- `Assets/Scripts/Services/VContainer/Lifecycle/OrderedLifecycleCoordinator.cs`
- `Assets/Scripts/Editor/` (new hitch profiler / allocation sweep tools), `Assets/Scripts/Editor/RenderAuditAblation.cs` (reference)
- `Tools/BattleAudit/battle_audit.py`, `Tools/BattleAudit/README.md`
- `ProjectSettings/ProjectSettings.asset` (`gcIncremental`, read only here), `Packages/manifest.json` (Phase 5)
