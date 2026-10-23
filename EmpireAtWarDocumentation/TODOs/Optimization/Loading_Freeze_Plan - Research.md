---
category: Optimization
status: in-progress
created: 2026-10-23
tags:
  - research
  - loading
  - addressables
---
# Loading Freeze Plan - Research

## Goal

- Record implementation verification and fresh Addressables layout evidence for [[TODOs/Optimization/Loading_Freeze_Plan]].

## Implementation

- Unity `6000.4.7f1`; Addressables `2.9.1`; content build target `StandaloneWindows64`.
- 2026-10-23 content-only build succeeded: 725.64 s report duration, 730.2 s runner duration, no build-period Console errors.
- Unity compilation clean; 77/77 relevant EditMode cases passed: preload 8, cache/root waiting queue 13, mapping 5, wreck dependencies 2, real composition/prewarm 18, ship integrity 10, orchestrator 17, loading isolation 4.
- User-requested Editor Play Mode profiling completed later on 2026-10-23; current-code captures reach Running without runtime errors. Standalone baseline/matched timing, visual, transition/memory and leak acceptance remain deferred.

### Editor Profile (2026-10-23)

- Full report: `Logs/LoadingFreeze/20261023T142740Z-editor-startup/analysis.md`; primary raw/data/JSON under `player-loop-trigger/`; confirming pass under `controlled/`.
- Setup: Republic human vs Separatist medium AI; Coruscant/Small; 1,000 starting credits; primary seed `-1024548213`. Deep Profile off, full domain reload, gameplay capped to 60 FPS after menu readiness.
- Native capture before Play Mode preserves startup. Automated request resumes on Unity `Awaitable.NextFrameAsync` so trigger/loading-scene samples run inside PlayerLoop. No eval calls during capture.
- Primary: frames 0–1662, **23.814 s**; request → Running **20.723 s**. All marker values below are milliseconds.

| Marker | Calls | Total | Max call |
|---|---:|---:|---:|
| ProjectScope.Configure | 1 | 5.984 | 5.984 |
| SkirmishScope.Configure | 1 | 9.800 | 9.800 |
| SceneService.LoadLoadingScene | 1 | 0.634 | 0.634 |
| SquadronPool.Initialize | 2 | 0.372 | 0.371 |
| SquadronPool.CountPrewarm | 2 | 0.701 | 0.657 |
| SquadronPool.PrewarmItem | 30 | 139.645 | 41.978 |
| AddressableHandleLoader.GetMappingData | 154 | 0.058 | 0.002 |

- Mapping resolver timing excludes asynchronous load wall time. Inclusive markers overlap; never sum parents and children.
- Prewarm frames 1569–1598 → exactly one item per frame. First item ~42 ms remains a hitch.
- Primary startup frame 0: **556.749 ms** total, largest PlayerLoop **268.915 ms**; EditorLoop self **241.510 ms**, Mono.JIT self **221.731 ms**.
- Primary battle activation frame 1555: **475.198 ms** total, largest PlayerLoop **445.889 ms**; GC **208.986 ms** under LoadLevelAsync Integrate, Mono.JIT self **127.287 ms**.
- Primary asset integration frame 627: **320.562 ms**, serialized-read self **82.789 ms**, File.Read **41.508 ms**; ResourceManager DelayedActionManager/AssetDatabase loading present.
- Primary frame 1553: **393.420 ms** total but largest PlayerLoop **1.022 ms**, EditorLoop self **391.076 ms** → Editor overhead.
- Confirming capture: **631.154 ms** asset-loading frame, File.Read self **333.391 ms**; battle activation **484.562 ms**; max prewarm **42.346 ms**. Seeds/caches differ; no matched comparison.
- Both controlled passes reach Running with no runtime Console errors. No production changes. Profiler/FPS settings restored; non-playing clean MainMenuScene; temporary imported helper and metadata removed.

### Editor Retest (2026-10-23, concurrent batches)

- Tested revision `a583652df` with fixed seed `-1024548213`, matching the earlier primary scenario. Deep Profile off; 60 FPS after menu readiness; 243 captured frames / 7.682 s.
- Request → `Running` **5.664 s** (earlier **20.723 s**); no new runtime errors. Cache/disk/system state is not controlled, so this is a reference comparison.
- Worst frame **983.782 ms** during Editor asset integration; File.Read self **487.804 ms**, file-header self **140.314 ms**, serialized-read self **120.527 ms**. Loading freezes remain.
- Max Configure: root **8.296 ms**, battle **8.819 ms**. Loading-scene API **0.905 ms**. 30 prewarm items in 30 frames; **144.742 ms** total, **48.837 ms** maximum item.
- Battle-loading full frame median/p95 **16.641 / 68.160 ms**. A separate **684.943 ms** frame is mostly EditorLoop (**682.507 ms**), with only **1.058 ms** PlayerLoop.
- Evidence: `Logs/LoadingFreeze/20261023T150935Z-editor-repeat/analysis.md`, native raw/data, timeline, scenario and metrics JSON. Editor/profiler restored, temporary helper removed, compilation clean; no production changes.

## Important Values

| Group | Current bundle bytes | Explicit assets | Implicit assets |
|---|---:|---:|---:|
| Data | 15,883,473 | 77 | 147 |
| View | 1,726,503,360 | 138 | 3,448 |
| Model | 447,433,819 | 30 | 1,211 |
| Ui | 4,376,840 | 26 | 50 |
| Audio | 174,611 | 4 | 1 |

- Prior recorded artifacts: Data 1,425,665,562 B; View 1,670,826,272 B; Model 470,031,007 B. These are artifact observations, **not matched Player performance captures**.
- 58 migrated `UnitWreckData` assets → `AssetReferenceGameObject`; canonical `PrefabKey` is the prefab GUID.
- 58 referenced wreck prefabs explicit in View; 59 wreck prefab files exist, one unreferenced.
- Zero implicit wreck prefabs across groups; zero implicit wreck visual assets in Data; zero wreck prefab/path duplicates. Shared living/wreck mesh and texture duplication can still exist.
- 962 duplicated implicit assets remain: 852 Model/View, 64 Model/Data, 18 Model/Data/View; remaining combinations are in the audit JSON.
- All Addressables group `.asset` bytes match pre-build snapshots. Original modified `addressables_content_state.bin` restored; SHA-256 `DDE7D433F570D5472AB33B0D1DD42A691932DBA2F20060E1D2D6C432312BD9C4`.
- Build preferences restored: `GenerateBuildLayout=false`, JSON report format, `AutoOpenAddressablesReport=true`.

## Decision

- Keep existing Pack Together/LZ4 group schemas. Why: Player attribution/packing measurements are deferred; remaining duplicate count alone does not prove a runtime win.
- Keep synchronous `IAssetService.Load<T>` for consumers; async preload fills the same `(Type, key)` cache before scope construction.
- Load battle dependencies before starting the target scene's suspended async load. Why: avoid loading dependencies behind a blocked Unity async operation queue.
- `SkirmishScope` takes cache ownership once; loading cancellation/failure disposes unclaimed handles. Root owns/releases the mapping handle.
- Prewarm one squadron per frame, stow before yielding, await all players before restore/station spawn and `Running`.

## Edge Cases

- Native VContainer child scope build can force an unbuilt root to build. Typed `ProjectScope` parent references instead enqueue implicit scene scopes until root preload/build completes.
- Root preloads menu/music dependencies even on direct battle startup so returning to menu uses cache hits.
- Loading-scene synchronous load remains behind `SceneService.LoadLoadingScene`; switch only if Player profiling identifies that hitch.
- Root `BattleSaveValidator` synchronous loads occur before scene transitions; inspect separately if save loading still hitches.
- Profiler markers: `ProjectScope.Configure`, `SkirmishScope.Configure`, `AddressableHandleLoader.GetMappingData`, `SquadronPool.Initialize`, `SquadronPool.CountPrewarm`, `SquadronPool.PrewarmItem`, `SceneService.LoadLoadingScene`. Marker scopes never span awaits.

## Files

- Full layout: `Library/com.unity.addressables/BuildReports/buildlayout_2026.10.23.16.59.48.json`.
- Compact layout/duplicate audit: `Library/LoadingFreezeVerification/build-layout-review.json`.
- Build status/restoration evidence: `Library/LoadingFreezeVerification/build-status.json`.
- Generated Library artifacts are local and may be cleared; key results are retained in this note.
