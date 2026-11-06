# Unity Pipeline catalog GC · fleet rerun

**The experimental patch removed the large allocation in command-list encoding, but long GC stalls still occurred in fleet combat. The patch was reverted; the project uses Unity's registry package `0.6.0-exp.1`.** The prior 2.040 s hitch had a Pipeline worker marking the heap while the gameplay thread waited. The experimental native trace caught a different 1.350 s hitch, with collection running on the main thread during Editor GUI layout. [Prior trace](../2026-11-06-Battle-Hitch/analysis.md) · [Rerun evidence](evidence.json)

## Change

- During this run, Unity Editor loaded the experimental `com.unity.pipeline@0.6.0-exp.1.eaw.1` package. It cached four encoded catalog responses per server, invalidated them when command discovery or server identity changed, and encoded cold responses in bounded blocks.
- The full catalog was 309,524 bytes across 76 blocks. The largest block was 4,104 bytes. Repeated requests reused the same body. Clearing the command registry produced a different body with the same bytes and block layout. [Cache observations](evidence.json)
- Ten successful catalog variants matched the original normalized JSON and HTTP body lengths. Coverage included full and compact listings, filters, groups and pagination. Two invalid variants returned HTTP 400. Eighteen concurrent reads matched the reference catalog. [Before/after and request logs](../../../Logs/BattleAudit/20261106T181128Z-catalog-fix)

## Fleet results

Coruscant seed `-1896963907`, Republic against Medium Separatist AI, 120 population spawned and 20 ships ordered to engage. This repeats the scenario that produced the confirmed Pipeline worker stack. The battle used a dirty local checkout and incremental GC was disabled. [Fleet setup](../../../Logs/BattleAudit/20261106T181128Z-catalog-fix/prepare.json) · [Attack order](../../../Logs/BattleAudit/20261106T181128Z-catalog-fix/engage.json)

| Window | Requests | What happened |
|---|---:|---|
| 140 s with native profiling off | 128 | Frame 29006 took 1,705.37 ms. A collection counter incremented during the corresponding render gap. One full catalog request overlapped the gap. This window has no native stack for that collection. |
| 70 s with macOS Time Profiler attached | 80 | Frame 41618 took 1,349.59 ms. Of 271 main-thread samples in the interval, 261 included Mono collection. The resolved initiating allocation was in `UnityEngine.GUILayoutUtility.BeginContainer` during Editor repaint. |

All 208 driven requests completed with HTTP 200 and the declared byte lengths. The load alternated full, compact and filtered catalog queries and included unique full URLs to exercise cold encoding. The trace identifies the initiating allocation for frame 41618. Earlier gameplay and tool allocations can still contribute to heap pressure. A single rerun does not establish a population-wide change in hitch rate or what triggered the original 721 ms frame. No errors appeared in the Editor Console. [Resolved stack and observations](evidence.json) · [Local Time Profiler recording](../../../Logs/BattleAudit/20261106T181128Z-catalog-fix/patched-fleet.trace)
