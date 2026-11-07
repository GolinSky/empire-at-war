---
category: Optimization
status: todo
created: 2026-11-06
updated: 2026-11-06
tags:
  - plan
  - optimization
  - startup
  - addressables
---
# Battle start hitches (reveal window)

## Goal
- Remove the stutter players see when a skirmish battle appears (fade-out → first seconds of `Running`).
- Prove each fix with a matched Development Player capture; Editor numbers only locate causes.

## Scope
- Owns hitches from battle-scene activation to +5 s, especially after the loading cover fades.
- Not owned: preload/loading-screen work, IL2CPP/Jenkins and cold first-spawn → [[TODOs/Optimization/Loading_Freeze_Plan|Loading Freeze Plan]].
- Navigation is cleared: hazard raster prebuilt during load (5.4–6.1 ms, under cover); no plan in any slow frame → [[Done/Optimization/Graph_Pathfinding_Plan]].

## Evidence (2026-11-06, Editor Play Mode, user-requested)
- Scenario: default skirmish via `MainMenuScope.Container.Resolve(SkirmishRouteUiController).StartGame()`; Unity Profiler, Deep Profile off, 2,000-frame buffer, recording until activation + 8 s.
- Captured 2,000 frames / 13.2 s; 25 frames > 25 ms, all between −2.6 s and +4.4 s around activation; +4.4 → +8.4 s clean.

| t vs activation | Frame | Dominant cost | Player? |
|---|---|---|---|
| −2.56 s | 369 ms | `GC.Collect` 282 ms (menu → loading Single load sweep) | yes, under loading screen |
| −1.85 s | 139 / 287 ms | Addressables `DelayedActionManager.LateUpdate` → `Loading.ReadObject` / `AwakeFromLoad` | likely smaller |
| −1.42 s | 1,292 ms | `EditorLoop` | no |
| 0 s | 489 ms | `Application.LoadLevelAsync Integrate` 389 ms | yes, under cover |
| +0.94 s | 93 ms | `GPUResidentDrawer.ProcessRenderers` 48.6 ms, `InstanceCullingBatcher.BuildBatch` 29 ms | yes, under cover |
| +1.44 s | 84 ms | startup `Awaitable` step; `Mono.JIT` 28 ms, `Battle.Navigation.GridBuild` 6.1 ms | JIT no |
| +1.86 s | 72 ms | `SquadronPool.PrewarmItem` 55 ms (`Mono.JIT` 39 ms) | JIT no |
| +2.72 / +3.34 s | 52 / 38 ms | startup `Awaitable` steps, partly JIT | partly |
| **+3.92 → +4.43 s** | **35–117 ms × 6** | Addressables integration on main thread (`IntegrateAllThreadedObjects`, `Loading.AwakeFromLoad` 52–91 ms, `File.Close` 55 ms, `AsyncReadManager.SyncRequest` 32 ms) | **likely, visible** |

- `BattleStartupSequence.RunAsync`: `Cover` → map load → shot prewarm → squadron prewarm → station spawn → `CompletePrewarmAsync` → reveal → 1 frame → `FadeOutAsync(0.5 s)`.
- Matching cause for the visible window: `BattleAssetWarmup` starts on first `BattleState.Running`, loads `BattleAssetPreload.DeferredPrefabKeys` (ship views, asteroid/Aotr structures, wrecks) `MAX_IN_FLIGHT = 4` per batch.
- Earlier ADB capture (2026-10-23) reported warm-up "smooth" (p95 17.61 ms after `Running`); today's run shows 6 frames of 35–117 ms → needs recheck under the same settings.
- Standalone scripting backend is IL2CPP (`scriptingBackend Standalone: 1`) → all `Mono.JIT` and `EditorLoop` costs are Editor-only.
- Light scenario: human idle, ≤ 12 ships in 5 min; first AI purchase ≈ 12 s.

## Decision
- Measure in a Development Player before changing pacing.
  - Why: Editor AssetDatabase loads and JIT inflate post-activation frames; the earlier ADB warm-up capture contradicts today's.
  - Avoid: moving all deferred prefabs back under the cover without data (Loading Freeze Plan deferred them to shorten load time).
- Open user decision after Phase 1 data: longer covered load **or** slower post-reveal warm-up.

## Implementation

### Phase 0 — attribution
- [ ] Add `ProfilerMarker`s: `Battle.Startup.Reveal`, `Battle.Startup.FadeOut` (around `FadeOutAsync`), `Battle.Warmup.Batch` with batch index and keys.
- [ ] Add one log line per warm-up batch: keys, start/end frame, elapsed ms (development builds only).
- [ ] Rerun the Editor scenario above; confirm the +3.9 s spikes sit inside `Battle.Warmup.Batch` and after `FadeOut` starts.

### Phase 1 — Player baseline
- [ ] Development Player (IL2CPP, packed Addressables), same skirmish, Profiler attached or `Profiler.logFile` raw capture from menu → +10 s after `Running`.
- [ ] Record: frames > 33 ms between `FadeOut` start and +5 s; per-batch warm-up cost; activation frame.

### Phase 2 — fix (pick from Phase 1)
- [ ] A: warm-up `MAX_IN_FLIGHT` 4 → 1 plus a frame yield between completions; keep `backgroundLoadingPriority = BelowNormal` (or `Low`) during warm-up.
- [ ] B: start warm-up after a quiet delay (e.g. 2–3 s after `Running`) or when no load is in flight.
- [ ] C: load only the local player's and visible-enemy rosters' likely first purchases under the cover; defer the rest (wrecks last).
- [ ] D (if GPU Resident Drawer appears after reveal in Player): register map renderers earlier or across frames.

### Phase 3 — acceptance
- [ ] Development Player matched capture: no frame > 33 ms from `FadeOut` start to +5 s; covered load time not worse by > 0.5 s.
- [ ] No first-spawn regression (cold spawn check stays with Loading Freeze Plan).
- [ ] User in-game feel check.

## Important Values
- Fade-out: `FADE_OUT_DURATION = 0.5 f`.
- Warm-up: `MAX_IN_FLIGHT = 4`; trigger first `BattleState.Running`.
- Plan queue budget elsewhere: navigation ≤ 4 plans/frame, 2 ms.

## Edge Cases
- Save restore / `BattleSimulation`: deferred prefabs join the required batch → warm-up has nothing to do; keep that path.
- Spawn before warm-up finishes → `CachedAssetService.Load` blocks on its own handle; slower warm-up widens that window.
- Exit during warm-up → cancellation in `LateDispose` must still release handles.

## Files
- `Assets/Scripts/Entities/CoreGame/BattleStartupSequence.cs`
- `Assets/Scripts/Services/Repository/BattleAssetWarmup.cs`, `BattleAssetPreload.cs`, `CachedAssetService.cs`
- `Assets/Scripts/Services/SceneService/SceneService.cs`
- `Assets/Scripts/Services/Squadron/SquadronPool.cs`

## TODO
- Evidence source: Editor Profiler buffer (not saved to disk); capture Phase 0/1 data to `Logs/BattleStartHitch/<timestamp>/`.
