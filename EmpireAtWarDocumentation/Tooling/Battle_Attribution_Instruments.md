---
type: engineering-reference
system:
  - profiling
  - tooling
verified: 2026-11-18
verification: Live source review, Unity compilation with zero errors, Python AST
  syntax inspection; tests and replays not run.
---
# Battle Attribution Instruments

## Rules
- Entry points/procedure: `Tools/BattleAudit/README.md` and `Tools/BattleAudit/Attribution.md`.
- User owns Play Mode. Instruments never load saves, start, stop or pause it.
- Editor/Development continuous capture: discovered built-in markers, GC counters and lifecycle callback time/bytes. Unsupported values remain `unavailable`.
- Rolling trigger: >50 ms OR ≥1,000,000 GC bytes. `is_hitch` includes both; selected windows cannot establish average FPS.
- Inclusive markers overlap; never sum parents and children. Attribution bytes cover the main thread only.

## Implementation
- Performance menu: Arm/Disarm Hitch Profiler; Allocation Sweep (5 min); Repeatable Attribution Run (4 min); Schedule Memory Snapshots (5 and 30 min).
- Native history ≥300 frames; derived hitch range −60/+10. Unity `SaveProfile(string)` retains the full buffer in `.data`.
- Allocation sweep: 60 frames/30 s; 14 resolved methods; exhaustive stack ranking divided by valid sampled frames including zero-allocation frames.
- Restore GC stack mode `None` on completion, cancellation, error, Play Mode exit and reload. Native capture owners are mutually exclusive.
- Memory Profiler pinned at 1.1.12. Full `.snap` saved live; expensive managed crawling waits until user exits Play Mode.
- Memory totals = shallow native/managed object sizes grouped by direct-referrer types/native counterparts. Reserved/untracked/graphics bytes and retained roots require `.snap` inspection.
- Repeatable short run = 3 min hitch +1 min allocation; long memory pair is a separate replay in the same folder.
- Offline commands: `hitches`, `allocations`, `memory`, `compare`, `archive`, `report`. `report <run>` writes `analysis.md`.

## Edge Cases
- Live archive can miss unfinished rolling writer batches and history windows. Re-import after teardown for full context.
- Snapshot/export/GC stack recording adds overhead; exclude instrument-generated stalls from gameplay targets.
- Editor/Player comparison stays `unverified` until save/workload/camera/settings/duration match. Absence in one short Player sample does not prove `editor-only`.
- Growth does not prove a leak. H1/H2/H3 and ≥80% attribution acceptance require measured replays.
- Pinned package snapshot adapter uses internal APIs; `BattleMemorySnapshotExporterTests` checks contracts. Revalidate on upgrade.

## Files
- [[Done/Tooling/Battle_Hitch_And_GC_Attribution_Instruments]]; [[TODOs/User_Acceptance_Queue#Battle Hitch and GC Attribution Instruments]].
- `Assets/Scripts/Editor/BattleAttributionRun.cs`, `BattleProfilerFlightRecorder.cs`, `BattleProfilerDecoder.cs`, `BattleMemoryCapture.cs`, `BattleMemorySnapshotExporter.cs`.
- `Assets/Scripts/Services/Timing/BattleBuiltinRecorders.cs`, `BattleSystemAttribution.cs`, `BattlePerformanceCapture.cs`; `Assets/Scripts/Services/Analytics/BattleFpsHistory.cs`.
