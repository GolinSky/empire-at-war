# P0 fleet stall investigation

## Status

- P0 remains open. Actual long frames reproduced; native evidence identifies a synchronous AssetBundle wait for the 296 ms event. Exact asset/caller attribution remains unresolved. A scoped satellite warmup omission is fixed; no general FPS improvement is claimed. Tests remain disabled.
- Working checkout initially clean at `50730efef840fe432330281014d8f8e2f22eb528`. Used the retained November 7 Mono Development Player, not a new build from this revision. Embedded source metadata still names `3cdc86cba`; the prior retained-fix build provenance is in `../2026-11-07-Optimization-Fixes/README.md`.
- Workload: seed 565646751, Republic / Medium Separatist, Small Coruscant, 120 population requested, attack-move, High Fidelity, camera output 2940×1846, render scale 1, incremental GC off, VSync off, uncapped, time scale 1. Evolving combat and random visual variation are not matched deterministic comparisons.

## Captures

| Run | Harness maxima, 3 × 60 s | Evidence |
|---|---|---|
| Unprofiled | 125.901 / 105.088 / 47.853 ms | Rolling history also recorded 145.299 ms; callback phases differ. |
| Metal System Trace + waiting-thread Time Profiler | 168.607 / 297.662 / 83.474 ms | Rolling history captured 296.146 ms, Unity frame 11598, observed 2026-11-07T09:01:09.441741Z. |

- Instrumented event: observation timestamps jump from 123.965625 to 124.264309 s on row 11597; Unity reports the 296.146 ms delta on row 11598. Align native evidence with the actual observation gap, not just the reported delta row.
- That row has capture-writer wait 0 ms, main-thread counter 13.036 ms, render-thread counter 4.582 ms, GPU counter 11.979 ms. Counter phases differ; these are not a causal breakdown of the 296 ms gap.
- Harness observed no GC-count change across its longest instrumented callback interval. This does not identify the cause or rule out every GC-related interaction.
- The retained diagnostic Player exited with SIGSEGV after saving completed windows. Shutdown failure is separate from in-battle hitch attribution.
- Concurrent Unity compilation, PlayerSettings changes and another recovery Player appeared during investigation; this task did not initiate them. Their settings are left untouched. Native instrumentation and host memory pressure prevent treating the traced run as a clean performance baseline.
- Full trace processing was stopped after prolonged paging. `trace trim` recovered a 15 s raw slice (~1.9 GB); CPU and Metal imports succeeded, plus a 1 s System Trace import. The abandoned ~27.55 GB full container was removed after verifying those retained files.

## Files

- `measurements.json`: complete rolling rows and harness summaries for these two runs.
- `baseline-long-frames.csv`, `traced-long-frames.csv`: >100 ms frames plus two rows before/after.
- Raw evidence: `/Users/golinsky/Library/Caches/EmpireOptimization/20261107-P0/`.
- Owner: `EmpireAtWarDocumentation/TODOs/Optimization/Optimization_Audit_Fix_Plan.md`.

## Native attribution

- [Native evidence](native-evidence.json): actual observer gap 298.684 ms, containing 260.103 ms Blocked, 35.059 ms Running, 1.240 ms Runnable and 2.281 ms Preempted on Player main thread 236766 (PID 24672).
- Raw context-switch stack events during the central 230 ms: 188 occurrences of `AssetBundleLoadAssetOperation::GetLoadedAsset`, 183 of `PreloadManager::WaitForAllAsyncOperationsToComplete` with timed semaphore acquire. These are stack occurrences, not independent duration samples.
- Player GPU work stops for 281.889 ms (slice time 4.793328–5.075216 s). The GPU timeline and CPU blocked state support a synchronous asset-load wait starving rendering. They do not support a long GPU pass or OS runnable starvation as the primary cause of this captured event.
- Native symbols recovered with the retained `UnityPlayer.dylib`; inferred image base `0x10a400000` independently agrees with PlayerMain and semaphore stack frames. Original image/JIT symbol chunks were unavailable; the managed caller and requested key of this exact event remain unresolved.
- An isolated copy of the retained Player instruments `CachedAssetService.Load` with timestamp/key/caller logging only for loads ≥5 ms. It changes no shared Editor or production source; repeat results are diagnostic because concurrent user work continues.

## Scoped fix and verification

- Two isolated packed probes found AI site construction synchronously loading `TurbolaserSatelliteView`: 43.034 ms and 50.416 ms in `CachedAssetService.Load`; the second included 47.202 ms directly in `AssetBundleRequest.asset`. Caller: `EnemySiteConstructionController.Tick` → `CaptureSitesSystem.TryStartConstruction` → `SiteFacilityBuilder.Build` → `DefendPlatformFactory.Create`.
- `BattleAssetPreload` used an old three-prefix list plus the asteroid variant, omitting all five defense satellites. It now enumerates `DefendPlatformType` through `DefendPlatformAssets.GetDataName/GetViewName`, matching the factory. Satellite data loads before Running; views and wrecks join paced warmup, or upfront preload for restore/simulation.
- Existing Core fixture now accesses every platform factory key and wreck after restore preload and after normal warmup, then checks that the loader handle count did not grow. These tests were authored, not run.
- Changed preload source compiled successfully with Roslyn against the retained Player assemblies outside Unity. No fresh Unity Player build or test execution is claimed.
- Packed verification uses an isolated copy with an equivalent five-prefix expansion of the old binary list; the production implementation uses the canonical type mapping. Instrumentation wraps `CachedAssetService.Load` and `AssetBundleRequest.asset`, logging completed waits ≥5 ms; factory requests for Turbolaser are also logged below that threshold.
- A separate 198.608 ms probe interval had no ≥5 ms `CachedAssetService.Load` call. It is not attributed to the satellite omission or claimed fixed. The exact managed asset/caller for the original native 296 ms event remains unproven.

- Final isolated packed verification completed all three 60 s windows: maxima 34.015 / 29.608 / 44.940 ms. Actual AI construction requested `TurbolaserSatelliteView` in 0.0022 ms at frame 14360, realtime 133.468 s. [Managed probe records and timings](managed-probe-evidence.json) preserve the before/after call stacks. This verifies the narrow cache-coverage change, not the absence of rare stalls or a general FPS gain.
