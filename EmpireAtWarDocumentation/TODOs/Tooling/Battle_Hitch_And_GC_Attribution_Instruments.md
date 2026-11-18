---
category: Tooling
status: todo
created: 2026-11-18
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

## Plan
### Phase 1 — Built-in markers and GC counters in every hitch/battle CSV
- [ ] Dump available marker/counter names once (`ProfilerRecorderHandle.GetAvailable` + `ProfilerRecorderDescription`) to `Logs/BattleAudit/profiler-markers.txt`. Use only names from that list; do not guess.
- [ ] Add `ProfilerRecorder`s to `BattlePerformanceCapture` (feeds the rolling hitch capture and the 10/60 s CSV):
  - Time: `GC.Collect`, `PlayerLoop`, `EditorLoop`, the PlayerLoop phases (Initialization, EarlyUpdate, FixedUpdate, PreUpdate, Update, PreLateUpdate, PostLateUpdate), `FixedUpdate.PhysicsFixedUpdate`, script update/late update, `Loading.*` (read/awake), present/vsync wait, UI canvas rebuild.
  - Counters: GC collection count (delta of `GC.CollectionCount(0)`), `GC Allocation In Frame Count`, `GC Used Memory`, `GC Reserved Memory`, `Total Used Memory`.
- [ ] Derived column `uncovered_ms = frame_duration_ms − PlayerLoop − EditorLoop` (where both exist) to show work outside any loop.
- [ ] `battle_audit.py hitches`: print the dominant built-in phase, GC collections and uncovered time for each slow frame; summarise the period between hitch frames > 200 ms.
- Acceptance: on a replay of `fps-mid-game`, ≥ 80% of each > 200 ms frame's duration lies in named columns, and H1/H2 is decided.

### Phase 2 — Per-system time and allocation in every frame
- [ ] Instrument `OrderedLifecycleCoordinator` (Tick, LateTick, PostTick, FixedTick) once: per registered system, `Stopwatch` time + `GC.GetAllocatedBytesForCurrentThread()` delta. This covers AI, ships, squadrons, combat, UI, networking and telemetry with one hook.
- [ ] Record per frame the top 5 systems by allocated bytes and by time into the hitch CSV (`alloc_top1_name`, `alloc_top1_bytes`, …) and a per-capture total table into `_summary.txt`.
- [ ] Report MonoBehaviour `Update`s outside the coordinator as one remainder (`script_update_ms` minus coordinator time).
- Editor and Development builds only (`#if UNITY_EDITOR || DEVELOPMENT_BUILD`); measure the instrument's own overhead and print it in the summary.
- Acceptance: the systems behind ≥ 80% of mean bytes/frame and of the 1–17 MB frames are named.

### Phase 3 — Hitch-triggered Profiler snapshot ("flight recorder")
- [ ] Editor tool **Diagnostics → Performance → Arm Hitch Profiler**: keeps the Profiler recording with a ≥ 300-frame history; when a frame exceeds a threshold (default 200 ms) it saves frames [hitch − 60, hitch + 10] with `ProfilerDriver.SaveProfile` to `Logs/BattleAudit/<run>/hitch_<frame>.data`.
- [ ] After each save, run the existing summary (`battle_audit.py profiler`) and the `decoder callers` for the top marker automatically; write `hitch_<frame>.md`.
- [ ] Option: record GC allocation call stacks while armed (higher overhead; off by default). Always restore `memoryRecordMode = None` on disarm, Play Mode exit and domain reload.
- [ ] Limits: at most N saves per session, cooldown between saves; never start/stop Play Mode.
- Acceptance: one playtest of ≥ 5 min produces at least one saved snapshot of a > 200 ms frame with its marker tree.

### Phase 4 — Allocation sweep
- [ ] Editor tool **Allocation Sweep (5 min)**: GCAlloc call stacks on, a 60-frame capture every 30 s, decode each with `decoder allocations`, merge into `allocation-ranking.json` (game method → bytes, calls, frames), then restore `None`.
- [ ] Keep 6–14 resolved frames per stack so framework allocators (`String.Concat`, `CombineImpl`, `Internal_CloneSingle`) resolve to the game caller (README §4 step 5, automated).
- Acceptance: top 10 allocating game methods with bytes/frame for a mid-game and a late-game save.

### Phase 5 — Memory growth
- [ ] Add `com.unity.memoryprofiler` (package change: needs user approval).
- [ ] Add `gc_used_mb`, `gc_reserved_mb`, `gc_collections` to each `fps_history` window.
- [ ] Snapshot at minute ~5 and ~30 of a replayed save; diff by type and owner; record the top growing types.
- Acceptance: the cause of the 3.3 → 4.3 GB growth is named (leak vs cache vs pool growth).

### Phase 6 — Editor vs Player separation
- [ ] Same recorders in a Development Player; replay `fps-mid-game` and `fps-late-game-p2` for 3 min each.
- [ ] Compare hitch period and size with the Editor run; anything only in the Editor is tagged `editor-only` and excluded from gameplay targets.

### Phase 7 — Repeatable measurement run
- [ ] One documented procedure: load save → arm hitch profiler → 3 min → allocation sweep 1 min → stop; outputs one folder with CSV, hitch snapshots, allocation ranking, memory diff and an `analysis.md` generated by `battle_audit.py report`.
- [ ] Update `Tools/BattleAudit/README.md` (new columns, tools, procedure) and record a new baseline in [[TODOs/Optimization/Live_Battle_CPU_Sweep_Plan]].

## Comparison saves (2026-11-18, same battle)
- `Logs/Playtests/20261118T1013Z/saves/fps-mid-game.json`, `fps-late-game.json`, `fps-late-game-p2.json` (also in `%USERPROFILE%/AppData/LocalLow/GolinSkyCompany/EmpireAtWar/Saves/Skirmish/`).

## Agent handoff
- Read `Tools/BattleAudit/README.md` first; reuse `BattlePerformanceCapture`, the rolling hitch capture and `battle_audit.py` instead of new parallel tools.
- No gameplay/performance fixes in this plan; record findings in the owning optimization plan.
- Never start, stop or pause Play Mode; the user owns it. Restore GC call-stack recording after every use.
- No tests run (local override); add EditMode tests for CSV column/header changes and the decoder parser.
- Hand back: new columns/tools, a sample output folder, H1/H2/H3 verdicts, and open questions.

## Tests (not run — local override)
- `Core`: `BattlePerformanceCapture` header/column tests, hitch window tests; Python: `battle_audit.py` parsing of new columns (add a fixture CSV).

## Files
- `Assets/Scripts/Services/Timing/BattlePerformanceCapture.cs`, `BattleProfilerMarkers.cs`
- `Assets/Scripts/Services/Analytics/BattleFpsHistory.cs`
- `Assets/Scripts/Services/VContainer/Lifecycle/OrderedLifecycleCoordinator.cs`
- `Assets/Scripts/Editor/` (new hitch profiler / allocation sweep tools), `Assets/Scripts/Editor/RenderAuditAblation.cs` (reference)
- `Tools/BattleAudit/battle_audit.py`, `Tools/BattleAudit/README.md`
- `ProjectSettings/ProjectSettings.asset` (`gcIncremental`, read only here), `Packages/manifest.json` (Phase 5)
