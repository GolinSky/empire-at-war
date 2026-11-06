# Battle hitch, Unity frame 41354

**Managed garbage collection is the leading explanation for the original 721.25 ms hitch.** A comparable Editor run captured an allocation on a Unity Pipeline worker triggering collection while the gameplay thread waited. The two native recordings from the original run cover other frames, so the allocating call for frame 41354 remains unknown. [Compact trace evidence](evidence.json)

## Original frame

- M2 Editor, Coruscant seed 565646751, fleet engagement. Completed frame 41354 was observed at 17:30:33.347591 UTC. Its unscaled interval was 721.253 ms and the gap between consecutive recorder observations was 725.884 ms. WindowServer’s log for the matching render size independently showed a 715 ms gap. [Original 61 rows](original-61-frames.csv)
- The trigger row recorded 5.507 ms CPU main, 1.532 ms render thread, an unavailable GPU sample, 703,962 allocated bytes and 0.000 ms waiting on the rolling writer. The largest custom scope was weapon tick at 0.216 ms. These counters cover different parts of a frame and cannot be subtracted from wall time to infer a GPU stall. [Original counters](original-61-frames.csv)
- The next frame took 40.277 ms, allocated 4,910,661 bytes and processed 570 target requests versus 11 on the trigger row. The native traces cover Unity frames 18990–19079 and 48358–48447. Neither has a stack for 41354. [Original audit](../../../Logs/BattleAudit/20261106T172531Z-live-hitch/analysis.md)

## Reproduction

A new Coruscant fleet battle used seed -1896963907, 120 population and 20 ships ordered to engage. The captures below are separate observations in that battle. Incremental GC was disabled in the running Editor.

| Capture | Observation |
|---|---|
| macOS Time Profiler, Unity frame 14587 | A 2.040 s hitch. The Unity Pipeline worker had 392 samples inside Mono collection spanning 1.955 s. The main thread was in `GC_lock` in 15 of its 20 available samples. |
| Unity native Profiler, Unity frame 30733 | The rolling CSV measured 1,271.836 ms. Native Profiler frame 1660 took 1,270.36 ms, including 1,228.08 ms of self time inside the Game view’s `GUIUtility.ProcessEvent`. The managed collection counter increased once during the overlapping render gap. |
| Collection counter only, native Profiler off | During 125 s, 951 ms and 885 ms hitches each aligned with a managed collection. A separate 645 ms hitch had no collection-count change across its measured gap. |

[Resolved thread stacks, frame alignment and collection counters](evidence.json)

For frame 14587, the resolved worker path was Unity Pipeline’s `BasePipelineServer.HandleCommandsRequest` → `System.Text.Encoding.GetBytes(string)` → managed array allocation → `GC_collect_or_expand` → `GC_stopped_mark` → `GC_mark_from`. On the main thread, `ShotEffect.Update` → `ShotEffectPool.OnEffectCompleted` → `ShotEffectPool.Detach` reached an allocation and waited for the collector lock. The command-list encoding triggered this particular collection. The trace cannot measure how much of the live heap came from earlier gameplay, Editor work or diagnostics. [Resolved stacks](evidence.json)

For the profiler-off check, the observer recorded both the elapsed time between consecutive end-of-render callbacks and Unity’s unscaled delta. The render gap that contains a collection can appear one row before Unity reports the long delta. Matching both intervals avoids blaming work that ran after the stall. [Observer records](evidence.json)

## Local raw evidence

- [Time Profiler recording](../../../Logs/BattleAudit/20261106T174405Z-721ms-trace/unity-wall-time.trace)
- [Triggered Unity Profiler recording](../../../Logs/BattleAudit/20261106T174405Z-721ms-trace/trigger.data)
- [Original compositor timestamps](../../../Logs/BattleAudit/20261106T174405Z-721ms-trace/original-compositor.log)
- [Native Unity scopes around the trigger](../../../Logs/BattleAudit/20261106T174405Z-721ms-trace/native-scopes.json)
- [Profiler-off collection observations](../../../Logs/BattleAudit/20261106T174405Z-721ms-trace/gc-watch.json)

Both Unity Profiler settings were restored. The reproduction battle reached its ended state. [Final state](../../../Logs/BattleAudit/20261106T174405Z-721ms-trace/final-state.json) · [Profiler settings](../../../Logs/BattleAudit/20261106T174405Z-721ms-trace/restored-state.json)
