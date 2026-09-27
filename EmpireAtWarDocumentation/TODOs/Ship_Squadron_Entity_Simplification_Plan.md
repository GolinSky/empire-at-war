# Ship & Squadron entity simplification plan

**Source analysis:** [[Architecture/SHIP_SQUADRON_ENTITY_ANALYSIS]] · **Date:** 2026-09-25 · **Status:** implemented 2026-09-26 (see Execution record). Not committed; no tests run.

Goal: simple and correct code. Each step is small, independent, behaviour-preserving, and can be merged alone. No new frameworks, event buses or domain-service layers.

Constraint: components stay a single `MonoComponent` each (no Presenter/View split — follow the `RadarComponent` pattern).

## Target rules (write these into AGENTS/Rules after step 3)

1. Entities talk to each other only through `Entity` + `EntityLocator` + entity facades (`IEntityFacade`, formerly `IEntityCommand`). This is the one entity-to-entity system. `IEntity` holds no Unity types.
2. The entity (`Ship`, `Squadron`) is the only object that connects its components.
3. Entity → component: method calls on component interfaces.
4. Component → entity: C# events or read-only observer interfaces. No `SetMediator`, no injecting the entity, no calling sibling components.
5. Only the entity changes state; states report `IsComplete`.

## Steps

| #   | Step                                                                                                                                                                                                                                                                                                                        | Touches                                                                    | Verify                                                               |
| --- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------- | -------------------------------------------------------------------- |
| 0 | Rename the facade family. First rename the misnamed Zenject factories `SpaceStationFacade`, `DefendPlatformFacade`, `MiningFacilityFacade` and `ShipFacadeFactory` to `*Factory`. Then rename `IEntityCommand` → `IEntityFacade`, `TryGetCommand` → `TryGetFacade`, and the entity role interfaces and implementations `I…Command` → `I…Facade`. Rename through Serena (symbol-aware) and keep the script GUIDs. | `Entities/BaseEntity/EntityCommands`, implementations, ~20 consumers | compile; no missing script references |
| 1 | Remove `Transform` from `IHealthModelObserver`. Add `IEntityTransformFacade { Transform Transform }`, bound once in `EntityInstaller` from the injected `ViewTransform`. The 38 cross-entity readers resolve it when they pick a target and keep the result. For its own position, a unit injects `ViewTransform` directly. **`IEntity` gets no position.** | health contract, `EntityInstaller`, readers | compile; targeting, camera follow and escort unchanged |
| 2   | Health no longer releases the entity: remove `IEntityLifecycle` injection from `HealthComponent` / `SquadronHealthComponent`; `Ship` / `Squadron` subscribe to `OnDestroy` and call `Release()`.                                                                                                                            | Health components, Ship, Squadron, other entities using `IEntityLifecycle` | unit dies once, death FX once                                        |
| 3   | Replace `SetMediator` with events: `RadarComponent.EnemyDetected / ContactsUpdated`, `SelectionComponent.SelectionChanged`, `ShipMoveComponent.PositionChanged / Stopped / LookingAt`. Inject narrow `IWeaponFacing` into move for `GetFiringTurnAngle`. Delete `IUnitMediator`, `IUnitComponent`, `IShipMovementMediator`. | Radar, Selection, ShipMove, Ship, Squadron, stations if they use mediators | no `Components → Entities.Ship` using; Squadron has no empty methods |
| 4   | Ship orders: one `StartOrder()` maps `ShipOrderModel.Current` → configured state; order methods call it; `ResumeOrder()` = `StartOrder()`. Flee checked in one place.                                                                                                                                                       | `Ship`, `ShipAIBrain`                                                      | same order behaviour; 7 duplicated tails removed                     |
| 5   | States don't transition: remove `LazyInject<StateMachine1/IdleState>` from `AttackTargetState`; add `IsComplete` to every ship state; `Ship.Tick` handles completion (waypoint advance / Idle) uniformly. Remove `CompleteNavigation` special-case flags.                                                                   | ship states, `Ship`                                                        | attack ends → idle; waypoints still chain                            |
| 6   | Move order handling out of `Ship` (e.g. `ShipOrderRunner`: order model + state machine + states + brain gating). `Ship` keeps lifecycle, wiring, death. Target `Ship` < 200 lines.                                                                                                                                          | new class, `Ship`, `ShipInstaller`                                         | `ShipAIBrain` depends on runner interface, not `LazyInject<Ship>`    |
| 7   | Move `ShipOrderModel`, `ShipOrderType` to a neutral namespace (units, not ships). Align Squadron order semantics with Ship (dedupe via `Matches`, `Attack` offset decision).                                                                                                                                                | Orders, Squadron                                                           | one documented meaning per order                                     |
| 8   | Hangar listens to hard point **model** destroyed event instead of polling the `HardPoint` view.                                                                                                                                                                                                                             | `HangarComponent`                                                          | hangar shuts down on destruction                                     |
| 9   | Cleanup: rename `StateMachine1` → `StateMachine`; remove unused `NavigateState.SetScreenDestination`, `IsTheSameWorldDestination`, `StateMachine1.ExitState`, unused `ShipAiDecision` values if still unused; merge duplicate `if (playDeathEffects)`; pass `deltaTime` into `HuntState` / `ShipAIBrain`.                   | small                                                                      | compile                                                              |
| 10  | Split `IShipMoveComponent` by consumer (states need move/look/stop/range; Ship needs the rest).                                                                                                                                                                                                                             | move interface                                                             | compile                                                              |

## Explicitly not planned

- No generic event bus, message types, ECS rewrite or hierarchical state machine library.
- No forced state machine for `Squadron` — its order `switch` + `SquadronPilot` is readable at current size.
- `FromComponentsInHierarchy` in installers stays until prefab roots get explicit serialized references (separate, low priority).
- `WeaponComponent` (449 lines) and `CombatAttackCoordinator` (719) need their own review.


## Execution record — 2026-09-26

All steps were implemented on branch `fix/battle-related-bugs`. The working tree is uncommitted. Unity compiles with zero errors, and the Console shows no errors. **Automated tests and Play Mode were not run.**

**User decisions applied:**
- `IEntity` gets no position.
- `IHardPointModel` keeps its `Transform` for now.
- Entity commands are renamed to facades.

| # | Result |
|---|---|
| 0 | Renamed the Zenject factories: `SpaceStationFacade` → `SpaceStationFactory`, `DefendPlatformFacade` → `DefendPlatformFactory`, `MiningFacilityFacade` → `MiningFacilityFactory`, `ShipFacadeFactory` → `ShipFactory` and `UiFacade` → `UiFactory`. Renamed `IEntityCommand` → `IEntityFacade`, `TryGetCommand` → `TryGetFacade`, all entity `I…Command`/`…Command` → `…Facade`, and the `EntityCommands` folders and namespaces → `EntityFacades`. The files were moved together with their `.meta` files, so the GUIDs are kept. None of these types is serialized. |
| 1 | Removed `Transform` from `IHealthModelObserver`. Added `IEntityTransformFacade` and `EntityTransformFacade`, bound once in `EntityInstaller` from `ViewTransform`. Added fail-fast `IEntity.GetFacade<T>()` next to `TryGetFacade`. Replaced 39 cross-entity reads plus the proton-beam caster read. `AttackTargetState` and `GuardState` cache the order target's Transform in `SetData`. |
| 2 | Deleted `IEntityLifecycle`. The health components only raise `OnDestroy`. `Ship`, `Squadron`, `SpaceStation`, `DefendPlatform` and `MiningFacility` subscribe in `Initialize` and release themselves. |
| 3 | Deleted `IUnitMediator`, `IUnitComponent` and `IShipMovementMediator`. The radar has a new `ContactsUpdated` event; new enemies arrive through `Enemies.ItemAdded`, the same pattern as `StationCombatPresenter`. Selection is observed through `ISelectionModelObserver.OnSelected`. Movement raises `DestinationChanged`, `LookingAt` and `Stopped`, and reads the firing angle through the new one-method `IWeaponFacing`. No `Components → Entities.Ship` using remains. `Squadron` has no empty methods. |
| 4–6 | New `ShipOrderRunner` owns the order model, state machine, states and flee override. Order methods record the order and call `StartOrder()`, which also replaces `ResumeOrder`. `Tick` handles flee and completion: the next waypoint, otherwise Idle. `ShipAIBrain` only decides `IsFleeing`; it has no state machine, no `LazyInject<Ship>`, and `deltaTime` is passed in. `ShipOrderFacade` talks to the runner. `Ship` went from 463 to 247 lines. The under-200 target is not met: the rest is the `IShipEntity` interface in the same file plus event wiring. |
| 5 | `IBaseState` is now `IsComplete / Enter / Tick(deltaTime) / Exit`. States never switch states; `AttackTargetState` lost its lazy state-machine and idle references. |
| 7 | `ShipOrderModel`/`ShipOrderType` → `UnitOrderModel`/`UnitOrderType` in `Entities/BaseEntity/Orders`, GUIDs kept. Added `UnitOrderModel.MatchesWaypoints`. `Squadron` now ignores repeated Move, AttackMove, Attack, Waypoint and Retreat orders, like `Ship`. Squadrons deliberately ignore the attack formation offset because fighters swarm, and a comment says so. |
| 8 | `HangarComponent` finds its hangar `IHardPointModel` at `Initialize` by matching the serialized view's transform, and shuts down on `OnDestroyed`. It no longer polls the view. All 6 hangar prefabs were checked: their hangar hard point is in `HealthComponent.ShipUnits`. |
| 9 | `StateMachine1` → `ShipStateMachine`. The name `StateMachine` would clash with the namespace of the same name. Removed `ExitState`, the `NavigateState` screen-destination API, `IShipMoveComponent.MoveToPositionOnScreen` and the now-unused camera injection in `ShipMoveComponent`. Merged the duplicate `if (playDeathEffects)` in `Ship`. **Kept** `ShipAiDecision.Navigate/Attack`: the decision model and its tests still produce them. |
| 10 | Split out `IShipMovement`, the 8 members that states, runner, brain, facade and SFX need. `IShipMoveComponent : IShipMovement` adds the entity-only members. |

**Rules** were added to `AGENTS.md` → *Entity Communication*.

**Behaviour notes to verify in play:**
- A completed Guard or Hunt order now goes to Idle like every other order. It no longer calls an explicit `Stop()`, so the "stopped" voice line plays only if the ship was actually moving.
- An explicit Stop while the AI is fleeing keeps fleeing. Before, it stopped the ship, and the next brain decision fled again.
- A health component no longer releases itself on death; its entity releases it through `EntityComponentLifecycle`.

**Tests were updated to compile, not run:**
- `ShipStopTests` now tests `ShipOrderRunner.Stop`.
- The test fakes implement `GetFacade`, and their fake health doubles as the transform facade.
- `NavigateStateTests` and `DefendPlatformTests` were adjusted.
- `ShipNavigationServiceTests` uses the new `Construct` signature.
