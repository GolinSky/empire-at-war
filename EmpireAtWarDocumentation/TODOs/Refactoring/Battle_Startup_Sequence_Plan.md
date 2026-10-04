---
category: Refactoring
status: in-progress
created: 2026-10-04
tags:
  - plan
  - refactor
  - core-game
  - startup
  - map
---

# Battle Startup Sequence Plan

## Goal

- Remove the visible battle-start artifacts: fog fade-in, first-frame lag, camera drifting in a random direction.
- One explicit, ordered, async battle startup owned by `SkirmishOrchestrator`; map generation runs async behind a future fade.
- Battle lifecycle and game speed exposed as notifiers; systems gate themselves by observing state.

## Root Causes (verified 2026-10-04)

- Fog: `FogOfWarSystem.Start` fills mask with 0 → `Update` fades toward targets at `fadeSpeed = 3` → ~0.3 s visible reveal.
- Camera drift (hypothesis, verify with first-frame pointer log): `CameraService.Tick` runs from frame 1; `CameraInput.Move` edge-scrolls from `_pointer.Position`, likely `(0,0)` before first mouse event → inside 16 px edge → pans down-left; speed × `Time.unscaledDeltaTime` spike on load frames.
- No startup: `MapInstaller.InstallBindings` generates + builds the map synchronously; every `IInitializable` then starts its own part on the same frame in uncontrolled order.
- `BattleVictoryService.Tick` evaluates from frame 1 → with async loading, no bases/ships exist → immediate false result.

## Decisions

- `BattleState { NotInitialized, Loading, Running, Paused, Ended }`; `GameSpeed { Normal, Fast }`.
  - Why: `Pause` is a state, not a speed; `Initialized`/`Idle` merged into `Running`.
- `SkirmishOrchestrator` implements `INotifier<BattleState>` and `INotifier<GameSpeed>`; `AddObserver` pushes the current value immediately.
  - Avoid: `SkirmishSessionModel`, `ISkirmishSessionModelObserver`, `GameTimeMode` — deleted.
- **Gating rule:** each system owns its start/stop condition by observing `BattleState`.
  - Avoid: fixing lifecycle problems via Zenject binding/execution order, `LazyInject`, or `Time.timeScale`.
  - `Time.timeScale` is only the speed/pause feature, never a correctness gate.
- **Steps vs reactors:**
  - Step = the next startup step needs its result → orchestrator calls and awaits it explicitly.
  - Reactor = needs data once it exists, or starts/stops with state → observes a notifier; orchestrator never references it.
- Map-dependent systems (zones, sites, obstacles, minimap, tooltips, planet) **subscribe to the map notifier**.
  - Why: data dependency on the map, not on the orchestrator; injecting them makes the orchestrator a god class; AI services live in `WithId` sub-containers and cannot be injected cleanly.
- Station spawning is a **step** via `IPlayerRegistry` (`IStationSpawner` per player).
  - Why: camera, fog reveal and victory depend on stations; observer order is hidden and fragile.
  - Avoid: `IBattleStartupStep` list with order ints — order scattered across files.
- `CoreGameUiController` observes state/speed and drives `ICoreGameUi`; the view subscribes to nothing (`SetModel` removed).
- Camera may move while `Paused`. Esc / pause menu ignored during `Loading`.
- Async via Unity 6 `Awaitable` (Unity `6000.4.7f1`); no UniTask.

## Startup Cycle

```text
NotInitialized
 └─ Orchestrator.Initialize → RunStartupAsync(ct)
Loading     input locked (IInputLock handle) · Esc ignored · reactors inactive
 ├─ [fader.ShowAsync]                                            (later)
 ├─ await mapLoader.LoadAsync(ct)
 │    ├─ generate MapLayout on background thread
 │    ├─ await BuildAsync (main thread, frame-sliced)
 │    └─ Publish(BattleMap) → zones · sites · obstacles · minimap · tooltips · planet (sync)
 ├─ foreach player: playerRegistry.GetStationSpawner(id).Spawn()
 ├─ camera.MoveTo(local station) · fog.RevealImmediately()
 ├─ await 2 frames
 ├─ [fader.HideAsync]                                            (later)
Running     input unlocked · timeScale from GameSpeed
 ⇅ Paused   player toggle or pause menu · timeScale 0 · camera still movable
Ended       BattleResult received · timeScale 0 · HUD disabled
 └─ ExitSkirmish → timeScale 1 → cancel startup ct → leave scene
```

## Components

- `SkirmishOrchestrator`: sequence, `BattleState`, `GameSpeed`, time scale. Depends on `IBattleMapLoader`, `IPlayerRegistry`, `ICameraService`, `IFogOfWarSystem`, `IInputLock`, later fader. Target ≤200 lines; else extract `BattleStartupSequence` owned and awaited by the orchestrator.
- `BattleMapLoader` (new, scene): `IBattleMapLoader.LoadAsync(CancellationToken)` + `INotifier<BattleMap>`; replays published map to late observers.
- `BattleMap` (payload): `MapLayout`, `ZoneViews`, `SiteViews`, `Obstacles`.
- `MapInstaller`: bindings only (settings, generator, loader, `MapModel` holder behind `IMapModelObserver`); no generation at bind time.
- `IStationSpawner`: registered by `PlayerService` / `EnemyService` in `IPlayerRegistry` (same pattern as `RegisterSiteBuilder`).

## Reactors

| System | Observes | Change |
|---|---|---|
| `MapObstacleContactProvider` | `BattleMap` | injected list → registered on publish |
| `MiniMapController`, `WorldTooltipPresenter` | `BattleMap` | obstacles added on publish, not in ctor |
| `ReinforcementZonesSystem` | `BattleMap` | build presenters from `ZoneViews` on publish; drop `ReinforcementZoneView[]` injection |
| `CaptureSitesSystem` | `BattleMap` | same with `SiteViews` |
| `PlanetSpawner` | `BattleMap` | spawn on publish |
| `PlayerService` | — | `Initialize` registers `IStationSpawner`; no spawn |
| `EnemyService` | `BattleState` | registers spawner; `_productionStrategy.Start()` on `Running`; `Tick` only while `Running` |
| `BattleVictoryService` | `BattleState` | evaluate only while `Running` |
| `CameraService` | `BattleState` | `Tick` only in `Running` / `Paused` |
| `CoreGameUiController`, `FactionUiController`, `SuperWeaponPresenter`, `UnitActionsPresenter`, `CinematicCameraPresenter` | `BattleState` (+ `GameSpeed` for CoreGameUi) | replace `IsBattleEnded` with `Ended` |
| `PauseMenuRouteController` / Esc route | `BattleState` | ignore Esc during `Loading` |

## Rules

1. Every notifier replays its current value on `AddObserver` → subscription order irrelevant.
2. Map observers are synchronous and build only their own state; no cross-system calls. Needs await → promote to step.
3. Gate by state, not by time scale or DI order.
4. Startup errors: one top-level try/catch → `Debug.LogException` + rethrow; no swallowing.
5. Startup cancelled via `CancellationTokenSource` in `LateDispose`.

## Implementation

1. [x] State/speed: enums, orchestrator notifiers, 5 presenter consumers, `CoreGameUiController` → view, CoreGameUi prefab sprite re-key (`timeSprites` by paused, `speedUpSprites` by `GameSpeed`), delete model + observer + `GameTimeMode`, update `SkirmishOrchestratorTests`, `UnitActionsPresenterTests`, `UiCanvasArchitectureTests`.
2. [x] Sequence: `Loading`, input lock, Esc ignored, `BattleMapLoader` + `BattleMap` notifier, map reactors, `IStationSpawner` via registry, victory/AI/camera gated on state.
3. [x] Async map: background generation, frame-sliced `MapLayoutView.BuildAsync`, `MapInstaller` bindings only, `MapModel` holder.
4. [x] Camera/fog fixes: no edge scroll until pointer reports a real position or app unfocused; clamp `unscaledDeltaTime` in `CameraService.Tick`; `IFogOfWarSystem.RevealImmediately()`.
5. [ ] Fader in reserved slots (separate follow-up).
6. [ ] Acceptance in Play Mode: no fog fade-in, no camera drift, no first-frame hitch, Zenject graph resolves (orchestrator ↔ `CameraService` cycle relies on method injection), Esc ignored while loading, pause/speed/menu/end/exit.

## Progress (2026-10-04)

- Phases 1–4 implemented; Unity recompile clean (`Assembly-CSharp`, `Assembly-CSharp-Editor`), no console errors; `CoreGameUi.prefab` reserialized and re-keyed sprites read back (`false→Icon_Pause`, `true→Icon_Play1`, `Normal→Icon_Next2`, `Fast→Icon_next1`).
- Not run: Play Mode, automated tests.
- Orchestrator 195 lines; steps extracted to `BattleStartupSequence` behind `IBattleStartupSequence` (test seam).
- Deviations from plan:
  - `BattleMap` also carries `StationObstacles` (built by loader from `MapGenerationSettings.GetStationRadius`).
  - Fog reveal runs **after** the 2-frame wait: stations register vision in their context `Start`.
  - Startup waits 1 frame before map load: AI/player sub-container kernels register spawners in their `Start`.
  - `FogOfWarSystem.Start` → `InitializeArea(scale)` called first in `BuildAsync`; component disabled until then.
  - Settings snapshot → warm-up: one read per `DictionaryWrapper` (`GetSize`, `GetStationRadius`, `GetRockLayer`) on main thread; worker only reads.
  - `ReinforcementZoneMiniMapPresenter` / `CaptureSiteMiniMapPresenter` build markers on first `Running` (zones/sites exist after map publish).
  - `MiniMapController` registers its UI route on map publish (view reads the map once on creation).
  - `StationFacingService` computes each rotation on first request (cached); roster arg removed.
  - `EconomyService` pays income only while `Running`. `EnemySiteConstructionController` / `EnemySquadronCommander` left ungated: no sites/squadrons before `Running`.
  - Speed toggle while paused changes speed, stays paused; unpause keeps speed.
  - Orchestrator keeps `LazyInject` for `INotifier<BattleResult>` and `IUserStateNotifier` (ctor cycles, not lifecycle).
- `IPointerInput.HasPosition` added (set on first `Position.performed`).

## Edge Cases

- Background generation: generators use `System.Random`, `Mathf`, vector structs only — thread-safe. Snapshot `MapGenerationSettings` (ScriptableObject) and `MapFeatureRadii` (reads prefab radii) on the main thread before switching threads.
- `MapLayoutView.Build` calls `fogOfWarSystem.ScaleArea` — must run before fog `Start`, or fog init moves to an explicit call. Verify order once build is async.
- `MiniMapController` calls `Physics.SyncTransforms()` before reading obstacle bounds — keep it in the publish handler.
- Timer-driven tickers (`EconomyService` income, `EnemySiteConstructionController`, `EnemySquadronCommander` decision timers) start in `Initialize`; audit and gate on `Running`.
- Exit during `Loading` → cancel token; no step runs after scene teardown.
- Menu pause stores requested speed; leaving menu restores it.

## Files

- `Assets/Scripts/Entities/CoreGame/SkirmishOrchestrator.cs`
- `Assets/Scripts/Services/SceneContext/Skirmish/MapInstaller.cs`, `SkirmishMainInstaller.cs`, `PlayerCoreInstaller.cs`, `AiPlayerInstaller.cs`
- `Assets/Scripts/Entities/Map/MapLayoutView.cs`, `Entities/Map/Generation/MapLayoutGenerator.cs`
- `Assets/Scripts/Services/BattleService/BattleVictoryService.cs`
- `Assets/Scripts/Services/Player/PlayerService.cs`, `Services/Enemy/EnemyService.cs`, `Services/Player/IPlayerRegistry.cs`
- `Assets/Scripts/Services/ReinforcementZones/ReinforcementZonesSystem.cs`, `Services/CaptureSites/CaptureSitesSystem.cs`
- `Assets/Scripts/Services/ShipNavigation/MapObstacleContactProvider.cs`, `Entities/MiniMap/Controller/MiniMapController.cs`
- `Assets/Scripts/Components/ViewComponents/FogOfWarSystem.cs`, `Services/Camera/CameraService.cs`, `Services/Camera/CameraInput.cs`
- `Assets/Scripts/Entities/SkirmishSession/Model/*` (delete), `Components/Commands/SkirmishGame/GameTimeMode.cs` (delete)

## Related

- Supersedes the `SkirmishSessionModel` decision in [[TODOs/Refactoring/SkirmishOrchestrator_Refactoring_Plan]].
- Touches camera rules in [[TODOs/Refactoring/Camera_System_Refactor_Plan]] (`CameraService` owns input-lock rule).
