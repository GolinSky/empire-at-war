---
category: Optimization
status: in-progress
created: 2026-10-23
tags:
  - plan
  - optimization
  - loading
  - addressables
  - startup
---
# Loading Freeze Plan

## Goal

- Remove startup and loading-screen freezes caused by synchronous asset loading and battle setup.
- Prove the cost split with a Player profiler capture before and after each fix.

## Before Implementation (2026-10-23)

- `CachedAssetService.Load` → `_loader.Load<T>(key)` then `handle.WaitForCompletion()` on every cache miss (`CachedAssetService.cs:31-32`).
- `AddressableHandleLoader.GetMappingData` → `LoadAssetAsync<AssetMappingData>().WaitForCompletion()` on first key resolve (`AddressableHandleLoader.cs:36-38`).
- Two owners, both inside VContainer `Configure` (synchronous, runs in scope `Awake`):
  - `ProjectScope.Configure:35-36` → mapping data + `GameData` (+ `SceneData` via `AssetRegistrations.RegisterShared`).
  - `SkirmishScope.Configure:95` → own `CachedAssetService`; loads `FactionCatalog`, modules, tooltips, `HardPointOverlayData`, battle modes during scope build.
- `SquadronPool.Initialize:51-67`: synchronous loop, up to `MAX_PREWARM_PER_TYPE = 12` full squadrons per type per player; registered in both `LocalPlayerScope:42` and `AiPlayerScope:35`.
- `SquadronPool.CountPrewarm:128-141` loads `ShipData` for every ship of the owner's faction + `SpaceStationData`.
- **New:** `ShipData.Wreck` → `UnitWreckData` → hard ref to wreck view prefab (e.g. `TartanWreckData` → `TartanWreckView.prefab`). 59 wreck prefabs; not Addressable entries → implicit deps of the Data group. `CountPrewarm` therefore pulls every faction wreck prefab + meshes/textures synchronously. Likely contributor to Data bundle size; confirm with build layout.
- `SceneService.LoadScene:65` → `SceneManager.LoadScene` (sync) for the loading scene; target scene via `LoadSceneAsync` + `allowSceneActivation = false` (`:89-90`), loaded 1 s after loading scene (`TimerPoolService.Invoke(..., 1f)`). Scope build on activation still hits the blocking loads above.
- Bundles (`Library/com.unity.addressables/aa/Windows/StandaloneWindows64`, 2026-10-23): View 1,670,826,272 B · Data 1,425,665,562 B · Model 470,031,007 B · Ui 4.4 MB · Audio 175 KB.
- Group schemas View/Data/Model: `m_BundleMode: 0` (Pack Together), `m_Compression: 1` (LZ4 → chunked reads; size alone ≠ full load), CRC on.

## Log Evidence

- `Player.log` (2026-10-23 16:13): `UnloadTime: 133.17 ms` (line 2496) on battle transition; 1 hangar exception.
- `Player-prev.log`: `UnloadTime: 222.01 ms` (4451); `Total: 166.46 ms` (4453, DeleteObjects 94 ms); `Total: 159.63 ms` (4458, DeleteObjects 151 ms); 51 hangar exceptions.
- Hangar error `Hangar: launch points must match the bay count.` from `HangarComponent.Initialize` during ship spawn → separate bug, not startup cause.
- Logs have no startup/frame timings → cannot rank causes. Logs come from a Desktop build (`C:/Users/golin/Desktop/EmpireAtWar/EmpireAtWar-Windows`), not the Library build above.

## Decision

- Original order: measure → async preload before scope build → time-slice squadron prewarm → break `ShipData` → wreck hard refs → bundle layout. User deferred Player measurement on 2026-10-23; implementation and automated/content verification proceeded first.
  - Why: preload removes the main-thread block regardless of bundle layout; prewarm is the largest known sync loop.
  - Avoid: re-packing bundles first (LZ4 already reads only needed chunks; change unproven).
- Keep `IAssetService.Load<T>` synchronous for consumers; make the *cache miss* the exception by preloading.
  - Why: VContainer `Configure` and constructors are synchronous; converting all call sites to async is out of scope.
- Async via Unity 6 `Awaitable` (matches [[TODOs/Refactoring/Battle_Startup_Sequence_Plan]]); no UniTask.

## Progress (2026-10-23)

- User requested implementation before Player testing; baseline, matched captures and visual/runtime acceptance are deferred. No measured performance gain claimed.
- Mapping, root data, music and menu assets preload asynchronously before `ProjectScope.Build()`. Implicit scene scopes use VContainer's typed-parent waiting queue until root assets/container are ready.
- Loading scene awaits typed battle assets, then starts the suspended target scene load. `SkirmishScope` takes the same cache; its destruction releases battle handles after instances/pools. Cancelled/failed preload releases pending handles.
- Asset preload completes **before** target scene loading starts; avoids holding Unity's async operation queue at `allowSceneActivation = false` while loading dependencies.
- `SquadronPool.Initialize` only registers the prewarmer. Startup awaits one squadron per frame per player before restore/station spawn and `Running`; each instance is stowed in its creation frame.
- Migrated 58 `UnitWreckData` assets to `AssetReferenceGameObject`; added 58 referenced prefabs to existing View group. No mapping, schema or packing changes. Editor dependency audit: zero wreck prefab dependencies across 54 ship/station data assets.
- Verification: Unity compilation clean; 77/77 relevant EditMode cases pass (preload 8, cache/root queue 13, mapping 5, wreck dependencies 2, real composition/prewarm 18, ship integrity 10, orchestrator 17, loading isolation 4).
- Content-only Addressables build succeeded (725.64 s report / 730.2 s runner); no build-period Console errors. Data **15,883,473 B**, View **1,726,503,360 B**, Model **447,433,819 B**. All 58 wreck prefabs are explicit View assets; no implicit wreck prefabs or wreck visual assets in Data. See [[TODOs/Optimization/Loading_Freeze_Plan - Research]].
- Layout review: zero wreck prefab duplicates; 962 shared implicit asset duplicates remain, mostly Model/View. Original content-state bytes and build preferences restored; group assets match pre-build snapshots. No packing/schema changes.
- Deferred: Player baseline/matched captures, spinner continuity, battle → menu → battle handle/memory acceptance, and the conditional loading-scene async switch. Plan remains active. Root save validation still uses synchronous consumer loads before transitions; profile that path separately if loading a save hitches.

## Review (2026-10-23)

- Live diff matches steps 1–4 and 6; changes uncommitted, mixed with unrelated working-tree edits.
- Rerun: Editor idle, compile clean, 0 Console errors; 73/73 EditMode pass (preloader 8, wreck deps 2, cache 13, loader 5, composition/prewarm 18, orchestrator 17, ship integrity 10). Loading-isolation 4 not rerun.
- `View.asset` +58 wreck entries; 58 `UnitWreckData` → `AssetReferenceGameObject`; GUID keys fall through `AssetMappingData.GetAssetKey` to Addressables unchanged.
- ~~Risk: `BattleAssetPreload.LoadAsync` awaits keys **serially**~~ → fixed 2026-10-23: `CachedAssetService.BeginPreload<T>` + `CompletePreloadsAsync` (failed/cancelled batch releases all its handles). Battle preload = 3 dependent batches (shared/UI/mode → structures + faction station/ship data+views → wrecks + squadron data/views); root `ProjectScope` preload = 1 batch. Marker names fixed (`MAPPING_MARKER`, `CONFIGURE_MARKER`). Compile clean; 48/48 EditMode (cache 15 incl. 2 new batch tests, preloader 8, loader 5, composition 18, wreck deps 2). Not re-profiled.
- Editor profile `Logs/LoadingFreeze/20261023T142740Z-editor-startup/analysis.md` (serial build): battle request → Running **20.723 s**; request → scope built 17.8 s at median 16.7 ms frames → frame-bound waiting, consistent with serial preload. Hitches: activation 475 ms (`GC.Collect` 209 ms + JIT 127 ms), Editor asset integration 320 ms, first squadron prewarm item 42 ms. Rerun same capture to measure the batched preload.
- Risk: preload now eagerly loads **every faction ship/station/squadron view prefab + wreck** (beyond the plan's data-only scope) → more load time/memory, fewer first-spawn hitches. Measure in step 0/7 before keeping.
- Style: `_mappingMarker` / `_configureMarker` static readonly use `_camelCase`; other new markers use `UPPER_SNAKE`.

## Implementation

0. [ ] **Baseline capture** (Player deferred; markers implemented; Editor profiling authorized and run on 2026-10-23): Development Player, Deep Profile off, record from launch → main menu → battle `Running`. Add `ProfilerMarker`s around `ProjectScope.Configure`, `SkirmishScope.Configure`, `SquadronPool.Initialize`, `CountPrewarm`, `AddressableHandleLoader.GetMappingData`. Record ms per marker + longest frame.
1. [x] **Mapping + root preload**: load `AssetMappingData`, `GameData`, `SceneData` asynchronously in a bootstrap step before `ProjectScope` builds (`autoRun = false` → await → `Build()`), or prove ProjectScope cost negligible in step 0 and skip.
2. [x] **Battle preload in loading scene**: while target scene loads (`allowSceneActivation = false`), await Addressables loads for the battle key set (faction catalog, faction ship/squadron/station data, UI, battle modes). Hand handles to the `SkirmishScope` cache so `Load<T>` hits are cache hits; `ActivateScene` only after preload completes.
   - Ownership: preloaded handles must be released with `SkirmishScope` (`AssetLifetimeScope.OnDestroy`), not leaked by the loading scene.
3. [x] **Time-slice `SquadronPool` prewarm**: replace the synchronous `Initialize` loop with a frame-budgeted build during `BattleState.Loading` (N per frame or ms budget), finished before `Running`. Spawn falls back to `_factory.Create` if prewarm is incomplete (existing path).
4. [x] **`CountPrewarm` without wreck loads**: chose option (a), wreck prefab AssetReferences. Ship/station/structure wreck prefabs load into the battle cache asynchronously; `UnitWreckService` resolves the cached GameObject GUID.
   - Editor dependency audit, 2 tests and fresh build layout pass: zero wreck prefab dependencies in ship/station data; zero implicit wreck prefabs in Data.
5. [ ] **Loading scene**: switch `SceneService.LoadScene:65` to `LoadSceneAsync` only if step 0 shows the loading scene load itself hitches.
6. [x] **Bundle layout review**: fresh successful JSON report; all 58 wreck prefabs explicit in View, no implicit wrecks in Data. 962 shared implicit assets remain duplicated; no packing changes without Player measurement. Details: [[TODOs/Optimization/Loading_Freeze_Plan - Research]].
7. [ ] **Acceptance**: matched capture vs step 0; loading spinner keeps animating through battle load; no new Addressables leaks on battle → menu → battle.
   - [x] EditMode preload cache handoff/cancellation/ownership and prewarm slicing tests; 77/77 relevant cases pass.
   - [x] User-requested Editor launch → menu → battle profiling: 1,663-frame primary capture reaches Running; no runtime Console errors. Marker timings and frame attribution recorded below.
   - [ ] Standalone Player checks deferred; no Player run. Visual, transition and leak acceptance remain unverified.

### Editor Profile (2026-10-23)

- User authorized Editor testing after the initial implementation. Default Republic vs Separatist skirmish, Coruscant/Small, Deep Profile off; 60 FPS cap after menu readiness. No pre-change timing baseline.
- Primary capture: 1,663 frames / 23.814 s; battle request → `Running` **20.723 s**. No runtime errors. Editor/profiler settings restored, MainMenuScene clean, temporary imported helper removed.
- Max marker call: `ProjectScope.Configure` **5.984 ms**, `SkirmishScope.Configure` **9.800 ms**, `SceneService.LoadLoadingScene` **0.634 ms**, pool `Initialize` **0.371 ms**, `CountPrewarm` **0.657 ms**, `PrewarmItem` **41.978 ms**. Mapping resolver max **0.002 ms** (not async load wall time).
- 30 prewarm items across 30 frames, **139.645 ms** total. Slicing verified; the first squadron still exceeds a 16.7 ms frame budget.
- Primary longest frames: startup **556.749 ms** (EditorLoop/JIT); battle activation **475.198 ms** (GC **208.986 ms**, Mono.JIT self **127.287 ms**). Earlier controlled pass: **631.154 ms** asset-loading frame, File.Read self **333.391 ms**.
- Loading hitches remain in Editor. AssetDatabase/serialized integration, scene-activation GC/JIT and first squadron construction need attribution before further changes; the **0.634 ms** loading-scene API sample alone does not justify an async API switch.
- Report: `Logs/LoadingFreeze/20261023T142740Z-editor-startup/analysis.md`. Raw, saved `.data`, marker totals, frame hotspots, battle setup and timeline JSON are alongside it. No matched before/after or standalone improvement claim.

### Editor Retest (2026-10-23, concurrent batches)

- Tested revision `a583652df` with fixed seed `-1024548213`, matching the earlier primary scenario. Deep Profile off; 60 FPS after menu readiness; 243 captured frames / 7.682 s.
- Request → `Running` **5.664 s** (earlier **20.723 s**); no new runtime errors. Cache/disk/system state is not controlled, so this is a reference comparison.
- Worst frame **983.782 ms** during Editor asset integration; File.Read self **487.804 ms**, file-header self **140.314 ms**, serialized-read self **120.527 ms**. Loading freezes remain.
- Max Configure: root **8.296 ms**, battle **8.819 ms**. Loading-scene API **0.905 ms**. 30 prewarm items in 30 frames; **144.742 ms** total, **48.837 ms** maximum item.
- Battle-loading full frame median/p95 **16.641 / 68.160 ms**. A separate **684.943 ms** frame is mostly EditorLoop (**682.507 ms**), with only **1.058 ms** PlayerLoop.
- Evidence: `Logs/LoadingFreeze/20261023T150935Z-editor-repeat/analysis.md`, native raw/data, timeline, scenario and metrics JSON. Editor/profiler restored, temporary helper removed, compilation clean; no production changes.

### Deferred Warm-up + CRC (2026-10-23)

- Preload split: required before `Running` = shared data, UI, mode HUD, ship/structure data, station views, base `DefendPlatformView`/`MiningFacilityView` (read by `SkirmishScope.Configure`), squadron data/views (prewarm). Ship views, asteroid/Aotr structure views and all wrecks → `BattleAssetPreload.DeferredPrefabKeys`.
  - Exception: pending save or `BattleSimulation` → deferred prefabs join the required batch (restore/simulation spawn units during startup).
- `BattleAssetWarmup` (battle scope; `IObserver<BattleState>`): on first `Running` loads deferred prefabs into the battle cache, 4 in flight per batch; cancelled in `LateDispose`. A spawn before warm-up blocks only on its own handle (`CachedAssetService.Load` → `WaitForCompletion`).
- `SceneService`: removed the 2024 fixed 1 s delay before the target load (now 1 frame); `backgroundLoadingPriority` High from `LoadScene` until the target scene loads, BelowNormal otherwise (was High permanently).
- Addressables: `UseAssetBundleCrc` disabled on all 9 local groups. Evidence: Editor `AssetBundle.LoadFromFile` View 1,646 MB **15,491 ms** with CRC vs **40 ms** without; Model 426 MB **2,631 ms** vs **21 ms**. Takes effect at next content/Player build (`BuildWithPlayer`).
- Editor captures (same runner/seed, +300 frames after Running):
  - ADB mode: request → Running **3.731 s** (prev 5.664 / 20.723 s); loading max 484 ms; after Running p95 **17.61 ms**, worst PlayerLoop 19.6 ms (warm-up smooth).
  - Packed mode (pre-CRC build): **20.501 s**, loading frames p95 17.61 ms → time is CRC reads, not main-thread work. Not re-captured after the CRC change (needs content rebuild).
- Hitch investigation:
  - Activation frame: `GC.Collect` 212–217 ms + `Mono.JIT` 132–136 ms under `Application.LoadLevelAsync Integrate` → Single-mode scene load runs Unity's unused-asset sweep with full blocking GC (`gcIncremental: 0`); at battle activation it only frees the loading scene. Fix candidate: load Battle additively, `SetActiveScene`, `UnloadSceneAsync(Loading)`; risk = objects instantiated unparented before `SetActiveScene` land in the loading scene. Not implemented.
  - Menu → loading scene: `GC.Collect` 206–226 ms + `BatchDeleteObjects` 24–31 ms; same sweep, frees the menu → expected.
  - First prewarm item 47–50 ms: `Mono.JIT` ~35 ms (first squadron-context code); later items ~2–5 ms; behind fade cover. JIT (also 132–136 ms at activation, ~220 ms at startup) disappears only with IL2CPP (scripting backend is Mono). Not changed.
- Tests: compile clean; 43/43 EditMode (preloader 10 incl. 2 new split tests, cache 15, composition 18). Evidence: `Logs/LoadingFreeze/20261023T160000Z-editor-deferred-adb/analysis.md`, `../20261023T161500Z-editor-deferred-packed/`.

### Additive Target Load (2026-10-23)

- `SceneService.LoadTargetScene` → `LoadSceneAsync(target, Additive)`; on target `sceneLoaded`: `SetActiveScene(target)` → deactivate loading roots → `UnloadSceneAsync(loading)`. Removes the Single-load unused-asset sweep + blocking GC at activation (212–217 ms in Editor captures). Menu → loading stays Single (its sweep frees the previous scene).
- Loading scene `EventSystem` removed (spinner is non-interactive; `UiHitTest` handles none) → no duplicate EventSystem warning during target Awake.
- Leak check (runner `sceneLoaded` hook): 0 objects added to the loading scene during battle Awake/scope build; runtime roots land in Battle; loading scene unloaded next frame; 0 warnings/errors. Battle → MainMenu: 1 scene, 1 EventSystem.
- Activation frame now has no `GC.Collect`; `Mono.JIT` remains (128–228 ms, varies by run).
- Pre-existing, out of scope: `MainMenuScene.unity` contains serialized `MusicSource(Clone)` / `AudioDialogSource(Clone)` roots.
- Tests: LoadingUiIsolation 4, GameController 4, BattleAssetPreloader 10 pass. Evidence: `Logs/LoadingFreeze/20261023T170000Z-editor-additive-adb/analysis.md`, `../20261023T171500Z-editor-additive-check/`.

## Edge Cases

- `CachedAssetService` keys by `(Type, key)`; preload must use the same `TSource` type or the miss still blocks.
- Exit during loading → cancel preload; release completed handles.
- Battle save restore uses `SpawnRestored` (no pool take) → unaffected by prewarm slicing.
- Prewarmed squadrons stowed at `PREWARM_POSITION (0,-10000,0)`; sliced build must keep them stowed in the frame they are built.
- Transition unload (133–222 ms) is `Resources.UnloadUnusedAssets` + scene unload; separate from load path. Track in [[TODOs/Bugs/Scene_Teardown_And_Material_Leaks]] if it persists after step 7.

## Files

- `Assets/Scripts/Services/Repository/CachedAssetService.cs`, `AddressableHandleLoader.cs`, `BattleAssetPreloader.cs`, `BattleAssetPreload.cs`
- `Assets/Scripts/Services/VContainer/Scopes/ProjectScope.cs`, `SkirmishScope.cs`, `AssetLifetimeScope.cs`, `Players/LocalPlayerScope.cs`, `Players/AiPlayerScope.cs`
- `Assets/Scripts/Services/Squadron/SquadronPool.cs`, `ISquadronPrewarmer.cs`; `Services/Player/IPlayerRegistry.cs`, `PlayerRegistry.cs`; `Entities/CoreGame/BattleStartupSequence.cs`
- `Assets/Scripts/Services/SceneService/SceneService.cs`
- `Assets/Scripts/Entities/Ship/Data/ShipData.cs`, `Assets/Scripts/Services/UnitWreck/UnitWreckData.cs`
- `Assets/AddressableAssetsData/AssetGroups/Schemas/{Data,Model,View}_BundledAssetGroupSchema.asset` (read-only unless step 6 approved)

## Related

- [[TODOs/Refactoring/Battle_Startup_Sequence_Plan]] — `BattleState.Loading` sequence hosts step 3.
- [[TODOs/Optimization/Squadron_Pooling_Plan]] — origin of prewarm.
- [[TODOs/Optimization/Desktop_Asset_Compression_Plan]] — loading captures overlap.
- Hangar launch-point exception: separate bug, not in scope.
