# Battle attribution instruments

## Continuous capture

- Editor and Development Players enumerate `ProfilerRecorderHandle`s before starting built-in recorders. `Logs/BattleAudit/profiler-markers.txt` records names and units. Unsupported counters remain `unavailable`. Release Players exclude instrumentation.
- Detailed and rolling CSVs add GC collection, PlayerLoop/EditorLoop, PlayerLoop phases, physics, script update/late update, loading, present/vsync and canvas rebuild timings. Inclusive scopes overlap: never add parents to children.
- `uncovered_ms` = frame duration minus PlayerLoop minus EditorLoop, only when both are available. This diagnostic residual is not additive CPU work.
- Raw counters: `gc_collections` (generation-0 count delta), `gc_allocation_count`, `gc_used_bytes`, `gc_reserved_bytes`, `total_used_bytes`. History windows add `gc_used_mb`, `gc_reserved_mb` and collections. Heap values are end-of-window samples.
- Lifecycle attribution covers registered Tick/LateTick/PostTick/FixedTick systems on the main thread. Per completed frame: `alloc_topN_name`/`alloc_topN_bytes` and `time_topN_name`/`time_topN_ms` for N=1–5. Summaries retain capture totals and measured attribution overhead. Worker-thread allocations are outside this byte delta.
- `script_update_remainder_ms` subtracts coordinator Update work from script Update. It excludes late/fixed callbacks to avoid subtracting unrelated phases.
- Rolling capture triggers on >50 ms or >=1,000,000 GC bytes; `is_hitch=1` means either. `hitches` prints built-in attribution/GC for every trigger and custom markers for the slowest ten. The >200 ms period groups consecutive slow frames into episodes. Pauses and missing context bias period estimates.

## Native flight recorder

- Menu: **Tools → Empire At War → Diagnostics → Performance → Arm Hitch Profiler**; optional GC allocation stack variant; **Disarm Hitch Profiler**. Tools never change Play Mode. One native profiler owner at a time, including Render Audit.
- Defaults: threshold 200 ms, 300 frames of history (previous setting restored on disarm), target decode `[hitch − 60, hitch + 10]`, bounded save count and cooldown.
- Unity `ProfilerDriver.SaveProfile(string)` saves the entire rolling buffer; `.data` contains a superset of the requested range. Metadata records the exact decoded indices and missing startup context. Unity frame numbers are distinct from native Profiler indices.
- Each hitch saves native `.data`, summary JSON, caller tree and Markdown attribution. Largest main-thread self marker is decoded automatically. Export/decoding perturbs subsequent frames: exclude instrument-generated stalls from gameplay conclusions.
- Native recording stops during export/decoding. The resume boundary is skipped; interrupted allocation windows restart after it, and the hitch cooldown starts after export completes. This prevents the recorder from capturing its own large exports recursively. Report assembly runs off the Editor thread.
- **Allocation Sweep (5 min)** captures 60 frames every 30 s with GCAlloc stacks. Up to 14 resolved methods preserve game callers above framework allocators. GC mode returns to `None` on completion, cancellation, error, Play Mode exit and assembly reload.
- `allocation-ranking.json` merges exhaustive stacks by game caller. Denominator = every valid sampled frame, including zero-allocation frames; unsampled intervals are excluded. Unresolved callers remain explicit. Legacy truncated `top` tables must be regenerated.

## Offline analysis

```sh
python Tools/BattleAudit/battle_audit.py hitches <run>/hitch_<run_id>.csv
python Tools/BattleAudit/battle_audit.py allocations <run>/allocation-ranking.json <run>/allocation-01.json <run>/allocation-02.json
python Tools/BattleAudit/battle_audit.py memory <run>/memory-before.json <run>/memory-after.json <run>/memory-diff.json
python Tools/BattleAudit/battle_audit.py compare <editor-run> <player-run> <run>/editor-player.json
python Tools/BattleAudit/battle_audit.py report <run>
```

- Memory input: `{"types":[{"type":"TypeName","owner":"OwnerName","count":2,"bytes":1024}]}`. Equal type/owner keys aggregate before comparison. Freed types retain negative deltas. Growth alone cannot distinguish leaks, caches and pools; inspect retained owners in `.snap`.
- Editor/Player comparison reports hitch size/period with `classification: unverified`. Only matched save, running duration, camera, resolution and workload evidence supports tagging a cause `editor-only`; absence in a short Player run is insufficient.
- `report` writes `analysis.md`, embeds saved hitch/profiler evidence, top allocation callers and memory growth, and inventories the folder. H1 (blocking GC), H2 (Editor work), H3 (ship-order spike) stay pending until a reviewer correlates frames and stacks.

## Repeatable measurement

1. Load `fps-mid-game` yourself. Record save, camera, quality, resolution, revision and workload counts. Keep Inspector/Editor activity consistent. Tools never load or reset the battle.
2. Use **Repeatable Attribution Run (4 min)**: 3 minutes of armed hitches, then a 1-minute allocation sweep in the same folder. The standalone sweep defaults to 5 minutes. Stop/cancel through the Performance menu if needed. Review run metadata and imported matching CSV/history alongside native outputs.
3. For growth, use the separate memory pair on a long replay: snapshots at running minute 5 and 30. Snapshot/crawling stalls are instrument overhead; exclude them from gameplay performance. Inspect retained references before diagnosing the growth.
4. Repeat from `fps-mid-game` and `fps-late-game-p2` in a Development Player for 3 minutes each. Same runtime recorders apply. Copy Player files into a labelled directory; run `compare` with the matching Editor folder. Keep Editor-only findings outside gameplay targets only after evidence confirms the distinction.
5. Run `report <run>`; preserve native buffers until attribution is reviewed. Write H1/H2/H3 verdicts with frames/stacks and update the owning optimization plan. No baseline or FPS improvement is established by installing these tools.

- Memory Profiler 1.1.12 is pinned for Unity 6000.4: [Unity package compatibility](https://docs.unity3d.com/6000.4/Documentation/Manual/com.unity.memoryprofiler.html).
- `fixtures/hitch_synthetic.csv` is synthetic parser data, never a baseline. Python regressions: `test_battle_attribution.py`. Unity category: `Core`. Tests and live replays deferred during implementation.
