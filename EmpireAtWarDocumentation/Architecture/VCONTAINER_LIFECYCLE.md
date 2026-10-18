# VContainer Lifecycle

## Goal

- One ordered dispatcher per scope drives every project callback: `OrderedLifecycleCoordinator`.
- Keep Zenject-era guarantees VContainer lacks: per-phase order, ordered teardown, tick gate, per-class profiler markers.

## Rules

- Implement only `EmpireAtWar.Lifecycle` interfaces: `IInitializable`, `ITickable`, `ILateTickable`, `IFixedTickable`, `IOrderedDisposable`, `ILateDisposable`.
- Never implement `VContainer.Unity` entry points (`IInitializable`, `IStartable`, `IPostInitializable`, `IAsyncStartable`, `ITickable`, `IPost*` …). Only `OrderedLifecycleCoordinator` may. Same-named interfaces on one type → callback runs twice.
- Teardown that must be ordered → `IOrderedDisposable` / `ILateDisposable`. Plain `System.IDisposable` → container disposes it separately, reverse creation order, outside `LifecycleOrder`. Use it only when order does not matter.
- Never implement both `IOrderedDisposable` and `IDisposable` on a container-built type → disposed twice.
- Order values live in `LifecycleOrders` constants only; no numeric literals in `[LifecycleOrder]` outside tests.
- Every scope derives `AssetLifetimeScope` and calls `base.Configure(builder)` → registers the coordinator. Root scopes that skip the base (`ProjectScope`) call `builder.RegisterLifecycle()` themselves. Never call it twice in one scope → callbacks run twice.

## Implementation

- `RegisterLifecycle()` → `OrderedLifecycleCoordinator` entry point + `LifecycleTickGate` + rethrowing entry-point exception handler (fail fast).
- Initialize, Tick, FixedTick, LateTick → ascending order. Dispose, LateDispose → descending (higher first). Ties → registration order (stable sort).
- `[LifecycleOrder]` read once per type (static cache); profiler markers cached per type and phase: `<Type.FullName>.Tick()`.
- `LifecycleTickGate.SetTicking(false)` stops all ticks of one scope (pooled squadrons).
- Coordinator ignores ticks after `Dispose`.

## Important Values

| Constant | Phase | Value |
|---|---|---|
| `INPUT_LOCK_INITIALIZE` | Initialize | -400 |
| `PLAYER_SCOPES_INITIALIZE` | Initialize | -300 |
| `CORE_HUD_INITIALIZE` | Initialize | -200 |
| `HEALTH_INITIALIZE` / `ORDER_INPUT_INITIALIZE` | Initialize | -100 |
| `SQUADRON_FLIGHT_INITIALIZE` | Initialize | -90 (after health) |
| `HUD_PRESENTERS_INITIALIZE` | Initialize | 100 |
| `SHIP_SFX_TICK` | Tick | 1000 |
| `FIGHTER_POSE_LATE_TICK` / `COMBAT_LATE_TICK` | LateTick | -2000 / -1000 |
| `SHIP_SFX_LATE_TICK` / `NAVIGATION_LATE_TICK` | LateTick | 1000 / 2000 |
| `ENTITY_SCOPES_DISPOSE` / `PLAYER_SCOPES_DISPOSE` / `PLANETS_DISPOSE` | Dispose | 1000 / 300 / 200 |
| `SHIP_SFX_DISPOSE` | Dispose | -1000 (last) |
| `CAPTURE_SITE_BUILD_UI_LATE_DISPOSE` / `INPUT_LOCK_LATE_DISPOSE` | LateDispose | 100 / -2000 |

## Edge Cases

- Child scopes dispose before parent services: `EntityScopeOwnership` (1000) and `PlayerScopeBootstrap` (300) run first.
- Coordinator depends on `VContainer.Internal.ContainerLocal<T>` (non-public API). Re-run `VContainerCompositionTests` and `LifecycleContractTests` after any VContainer upgrade.

## Files

- `Assets/Scripts/Services/VContainer/Lifecycle/` — interfaces, `LifecycleOrders`, coordinator, `RegisterLifecycle`.
- `Scopes/`, `Scopes/Players/`, `Scopes/Entities/` — scope hierarchy.
- `Battle/Modules/`, `Battle/Modes/` — optional features, game-mode services.
- `Registrations/` — shared registration helpers (`TooltipRegistrations`, `UiRegistrations`, `BattleDataRegistrations`, …).
- Tests: `Tests/Editor/LifecycleContractTests.cs` (no VContainer entry points in project types; one coordinator per child scope), `VContainerCompositionTests.cs` (phase order).
