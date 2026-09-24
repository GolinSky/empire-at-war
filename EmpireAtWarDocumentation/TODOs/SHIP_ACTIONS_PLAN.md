# Unit Actions (Ship Orders) — Implementation Plan for Codex

Read `AGENTS.md` first. Read the vault notes `UI_UX_GUIDELINES` and `UI_CODE_BUILD_GUIDE` before touching UI.
Do not split entity components into Presenter/View (entity components stay one `MonoComponent`, like
`RadarComponent`).

**Execution mode:** implement every phase below in order, **end to end, without stopping for review or
manual validation**. Write the tests listed at the end, but do not run them (AGENTS.md: tests run only on
explicit request). Finish with the "Definition of done" checks, which you perform yourself.

## Goal

One **order API** that both the player (UI + right-click) and the **enemy global AI** use to command units.
Orders reach units through **entity commands** (`IEntity.TryGetCommand<T>`), like `IMoveCommand` and
`IAttackCommand` do today. Wire the `ShipActions` panel in `Assets/Prefabs/Ui/SkirmishGame/CoreGameUi.prefab`
to it. Ships get the full set; the space station and defend platforms get Attack + Stop; mining facilities
get **no actions**.

| Id | Action | Player input → behaviour | Who supports it |
|---|---|---|---|
| `Move` | Move | Button → right-click space. Travel without stopping to engage (weapons still fire opportunistically). | Ships |
| `Attack` | Attack | Button → right-click enemy. Focus that target. | Ships, Station, Defend platform |
| `AttackMove` | Attack-Move | Button → right-click space. Advance; stop to fight enemies met en route; resume. | Ships |
| `Stop` | Stop | Button. Cancel orders, **reset main target**, stop moving; still fire at enemies in range. | Ships, Station, Defend platform |
| `Guard` | Guard | Button → right-click friendly unit. Follow it; chase enemies briefly, then return. | Ships |
| `WaypointMove` | Waypoint Move | Button → right-click points 1→2→3… → press button again to finish → ships follow the path in order. Shortcut: hold Alt + right-click repeatedly; releasing Alt finishes. | Ships |
| `Hunt` | Hunt | Button. Automatically seek and attack the nearest known enemy. | Every ship that can move **and** attack (all ships today) |
| `Retreat` | Retreat | Button → countdown → the receivers go to their side's station, or to their side's default reinforcement zone if the station is dead, then go idle. Press again with the same ships selected during the countdown to cancel. | Ships |

Out of scope: hard-point (engines / weapons / shield generator) targeting for Attack, and actions on the
per-ship-type group cards in `ShipGroupUi`.

**Receiver scope (hard rule for every action).**
- Every order is issued to an explicit receiver list. For the player that list is the current selection;
  for the enemy AI it is the task-force ships it chose.
- One selected ship → the action applies **only to that ship**. Many selected ships → **only to the
  selected ships**. Unselected ships are never touched.
- The player's receivers are a snapshot of the alive selected entities taken when the order is issued: on
  button press for immediate actions, on the confirming right-click for targeted ones. A selection change
  while an action is pending cancels the pending action.
- Receivers without the matching command are skipped (e.g. a station in a mixed selection ignores Retreat).
  A button is available when **at least one** alive selected entity supports it.

| Action | 1 receiver | N receivers |
|---|---|---|
| Move | moves to the point | compact formation around the point |
| Attack | attacks the target | all attack the same target with formation offsets (existing `DispatchAttack` math) |
| Attack-Move | attack-moves to the point | compact formation; each ship attack-moves to its own slot |
| Stop | stops | each receiver stops |
| Guard | guards the friendly | all guard the same friendly, each with its own offset; a receiver that is the guard target is skipped |
| Waypoint Move | follows the points | formation offsets computed once at the first point and reused for every later point |
| Hunt | hunts | each receiver hunts on its own |
| Retreat | retreats | compact formation around the retreat point |

## Current state (verified in source)

- Commands: `IEntityCommand`, `IMoveCommand`, `IAttackCommand`, `IHealthCommand`,
  `IEntitySelectionCommand` in `Entities/BaseEntity/EntityCommands/`. `Entity.TryGetCommand<T>` scans the
  `IEntityCommand[]` bound in the entity's installer.
- Ship command classes, bound in `Entities/Ship/ShipInstaller.cs` (~lines 119–143) and split by `PlayerType`:
  - player: `PlayerShipCommand` (`Components/Commands/Ship/`, `IMoveCommand` → `Ship.MoveTo`) and
    `PlayerAttackShipCommand` (→ `Ship.Attack`);
  - opponent: `EnemyShipCommand` (empty) and `EnemyAttackShipCommand` (→ `Ship.AssignAttackTarget`).
- Ship behaviour: `Entities/Ship/Ship.cs` drives a per-ship `StateMachine1` with `IdleState`
  (hold position + face nearest enemy in weapon range, no chasing), `NavigateState`, `AttackTargetState`,
  `FleeState`. Player orders call `_shipAIBrain.Enable(false)`.
- Opponent ships have two AI layers:
  - **Global/strategic**: `EnemyStrategicDecisionModel` picks an `EnemyStrategicState` (`CaptureZone`,
    `HuntFleet`, `AssaultBase`, `DefendBase`, `Hold`, `RebuildFleet`, `RetreatValue`).
    `Services/Enemy/EnemyTaskForceExecutor.Execute` turns it into per-ship calls on **`IShipEntity`**:
    `AssignMoveTarget`, `AssignAttackTarget`, `HoldPosition`. It has its own formation code (compact +
    `BattleFormationModel` with stable per-ship offsets). `EnemyUnitCommander` also calls `AssignMoveTarget`
    / `HoldPosition` (`MoveShipsOutOfDefaultZone`). `EnemyStrategicContextBuilder` builds the ship lists from
    `IShipService`. It re-issues orders every decision, so the ship side must ignore repeats.
  - **Per-ship/tactical**: `Entities/Ship/Mediator/ShipAIBrain` (flee on low shields, attack the assigned
    target), fed by `Ship.AssignAttackTarget`. `Ship` keeps `_opponentMoveTarget` / `_hasOpponentMoveTarget`
    and `ResumeOpponentNavigation()` for opponent moves.
  - `EnemyShipAbilityController` handles enemy abilities. Leave it unchanged.
- Main target: `AttackTargetState.Enter` calls `IWeaponComponent.AddTarget(..., AttackType.MainTarget)`;
  `AttackTargetState.Exit` and `IdleState.Enter` call `IWeaponComponent.ResetTarget()`.
- Stationary units: `SpaceStation`, `DefendPlatform`, `MiningFacility` bind only `SelectionCommand` +
  `HealthCommand`. For the station and platforms, `StationCombatPresenter` (`Components/Weapon/`) sets the
  weapon main target when an *enemy gets selected*. Mining facility has no weapons.
- Right-click (`InputType.ShipInput`) currently reaches three listeners, each checking its own case:
  `ShipUiController.HandleInput` (empty space → formation move; skips when a unit is under the cursor),
  `SelectionService.HandleActionInput` (enemy → ability target submit or `DispatchAttack`), and
  `UnitOrderFeedbackUiController.HandleInput` (repeats both checks to play markers).
- Ability targeting mode: `ShipAbilityService` (`IsWaitingForTarget`, `SubmitTarget`, `CancelTargeting`,
  `TargetingChanged`).
- Reinforcement zones: `ReinforcementZoneView` has `StartingOwner` and `IsCapturable`;
  `ReinforcementZonesSystem` (`Services/ReinforcementZones/`). `TryGetDefaultSpawnPosition` uses the
  **current** owner and a random offset, so it is not suitable as a retreat point.
- UI: `CoreGameUi` / `ICoreGameUi` / `CoreGameUiController` (`Entities/CoreGame/Ui/`).
  **Uncommitted local changes exist in these three files (`SetContentLayout`). Preserve them.**
  `ShipActions` hierarchy: `Header(TitleText, HeaderDivider)`, `AttackMode`, `AttackOnMove`, `Stop`,
  `Guard`, `CustomTrajectory`, `GoAround`. Each is a `Button` + `MPImage` with an `Icon` child. No script yet.

## Architecture: one order API, two callers

```
PLAYER                                                         ENEMY GLOBAL AI
UnitActionsView (MonoBehaviour)                                EnemyStrategicDecisionModel (unchanged)
   │ ActionPressed                                                  │ EnemyStrategicDecision
UnitActionsPresenter ──sets──► UnitActionTargetingModel          EnemyTaskForceExecutor / EnemyUnitCommander
   │ immediate orders                  ▲ reads                      │ receivers = task-force ships
   │                     PlayerOrderInputHandler                    │
   │                     (the ONLY right-click handler)             │
   ▼                                   ▼                            ▼
            IUnitOrderService  (Services/UnitOrders) ◄──────────────┘
            issuer-agnostic: IssueX(receivers, …); formation math; retreat destination; OrderIssued
                           │ TryGetCommand<T> on each receiver
                           ▼
   Entity commands: IMoveCommand, IAttackCommand, IAttackMoveCommand, IStopCommand, IGuardCommand,
                    IWaypointMoveCommand, IHuntCommand, IRetreatCommand, IFocusFireCommand
                           ▼
   Ship (+ ShipOrderModel, states; ShipAIBrain = opponent tactical layer) / StationaryAttackCommand
```

Decoupling rules:
- `IUnitOrderService` knows **nothing** about input, selection, UI, or which side is calling. No
  `PlayerType.Player` constants inside it; side-specific data (retreat destination) comes from the
  receivers' own `PlayerType`.
- Player-only concerns live in `PlayerOrderInputHandler` (right-click), `UnitActionsPresenter` (buttons),
  and `UnitActionTargetingModel` (pending mode). The enemy AI never touches them.
- Enemy-only concerns stay in `Services/Enemy/*` (what to order, whom, when). It never calls `Ship`
  methods directly for orders; only `IUnitOrderService`.
- Ship-level order semantics are the **same for both sides**. Differences between sides are limited to
  the opponent's tactical brain (flee), described in Phase 1.

MVP split for the player feature:
- **Model**: `UnitActionTargetingModel` (pending action id, waypoints as `FormationPoint`, events);
  per-ship `ShipOrderModel`. No `UnityEngine`.
- **View**: `UnitActionsView` + `UnitActionButton`. They only render and raise `ActionPressed`.
- **Presenter**: `UnitActionsPresenter` (availability, pending flow, mutual exclusion with abilities,
  Escape cancel).
- **Services**: `UnitOrderService` (orders) and `PlayerOrderInputHandler` (right-click interpretation).

## Phase 0 — Order service + single player right-click handler (no behaviour change)

1. `Services/UnitOrders/IUnitOrderService.cs` + `UnitOrderService.cs` (pure C#, bound in the skirmish scene
   installer next to `SelectionService` / `ShipAbilityService`). Move into it the formation move from
   `ShipUiController.MoveToPosition` and the formation attack from `SelectionService.DispatchAttack`.
   API (grows in later phases):
   ```csharp
   void IssueMove(IReadOnlyList<IEntity> receivers, Vector3 point);
   void IssueMove(IReadOnlyList<IEntity> receivers, IReadOnlyList<Vector3> destinations);        // explicit slots (AI)
   void IssueAttack(IReadOnlyList<IEntity> receivers, IEntity target);
   void IssueAttack(IReadOnlyList<IEntity> receivers, IEntity target, IReadOnlyList<Vector3> offsets); // explicit offsets (AI)
   event Action<UnitOrder> OrderIssued;   // UnitOrder: action id, issuer PlayerType, point, target, waypoints
   ```
   The explicit-slot overloads let the enemy executor keep its stable `BattleFormationModel` offsets.
2. `Services/UnitOrders/PlayerOrderInputHandler.cs` (pure C#): the **only** subscriber to
   `IInputService.OnInput` for `ShipInput`. It observes the player selection (`IObserver<ISelectionSubject>`)
   and resolves each right-click in this order:
   1. ability waiting for a target → `ShipAbilityService.SubmitTarget` on an enemy, otherwise cancel (existing rules);
   2. pending action in `UnitActionTargetingModel`, or Alt held → interpret for that action (Phase 2);
   3. default: enemy under the cursor → `IssueAttack`; space that isn't an obstacle (move
      `IsMapObstacleTap` here) → `IssueMove`.
3. Delete the right-click handling from `ShipUiController` and `SelectionService`.
   `UnitOrderFeedbackUiController` stops parsing input and plays markers from `OrderIssued`, only when
   `order.Issuer == PlayerType.Player`.

## Phase 1 — Entity commands, ship order model, ship behaviour (both sides)

New interfaces in `Entities/BaseEntity/EntityCommands/` (one type per file):

```csharp
public interface IStopCommand : IEntityCommand { void Stop(); }
public interface IAttackMoveCommand : IEntityCommand { Vector3 WorldPosition { get; } float NavigationRadius { get; } void AttackMoveTo(Vector3 worldPosition); }
public interface IGuardCommand : IEntityCommand { void Guard(IEntity friendly, Vector3 offset); }
public interface IWaypointMoveCommand : IEntityCommand { void MoveAlong(IReadOnlyList<Vector3> waypoints); }
public interface IHuntCommand : IEntityCommand { void Hunt(); }
public interface IRetreatCommand : IEntityCommand { bool IsRetreatPending { get; } float RetreatRemaining { get; } void Retreat(Vector3 destination, float delay); void CancelRetreat(); }
public interface IFocusFireCommand : IEntityCommand { void FocusFire(IEntity target); } // stationary weapons
```

Ship command binding (same for both sides):
- Replace `PlayerShipCommand`, `PlayerAttackShipCommand`, `EnemyShipCommand` and `EnemyAttackShipCommand`
  with **one** `Entities/Ship/EntityCommands/ShipOrderCommand.cs` implementing `IMoveCommand`,
  `IAttackCommand`, `IAttackMoveCommand`, `IStopCommand`, `IGuardCommand`, `IWaypointMoveCommand`,
  `IHuntCommand`, `IRetreatCommand`, forwarding to `Ship`. Bind it for **both** `PlayerType`s in
  `ShipInstaller`. Keep binding `SelectionCommand` / `HealthCommand` as today. If
  `IShipCommand` / `EnemyShipCommand` end up unused, delete them. If the class passes ~200 lines, split it
  per order under `Entities/Ship/EntityCommands/<Order>/`.
- Check every existing `TryGetCommand<IMoveCommand>` usage (`CoreGameUiController.HasMovableSelection`,
  `UnitOrderFeedbackUiController`, abilities `AssaultAbility` / `ConcentrateFireAbility`) still behaves
  correctly now that opponent ships also expose `IMoveCommand`. Filter by `PlayerType` where the old code
  relied on opponents lacking it.

**Order lifecycle: one current order per ship.**
- New pure model `Entities/Ship/Orders/ShipOrderModel.cs`, bound per ship in `ShipInstaller` and injected
  into `Ship`, `ShipAIBrain` and the new states. It holds **one** current order:
  `ShipOrderType Current { None, Move, Attack, AttackMove, Guard, WaypointMove, Hunt, Retreat }` plus that
  order's data: destination, attack/guard target, guard offset, waypoint queue, retreat destination and
  countdown. Positions are stored as `FormationPoint`, so the model has no `UnityEngine`.
- The only mutators are `Replace(type, data)` and `Clear()` (= `Replace(None)`). `Replace` wipes **all**
  previous order data first, so nothing can leave behind a waypoint queue or a ticking retreat countdown.
- **Idempotency** (needed because the enemy AI re-issues orders every decision): each `Ship` order method
  returns early when the incoming order equals the current one (same type, same target id, destination
  within a small epsilon). A repeated Retreat keeps its running countdown. `Ship.Attack` already does
  this with `IsTheSameTarget`; generalize it.
- Every public order method on `Ship` (`MoveTo`, `Attack`, `AttackMoveTo`, `Guard`, `MoveAlong`, `Hunt`,
  `Retreat`, `Stop`) starts with the idempotency check, then `_orderModel.Replace(...)` (Stop uses `Clear()`).
- Code that continues an order reads the model, never local fields:
  - `Ship.CompleteNavigation()` pops the next waypoint only if `Current == WaypointMove` and the queue isn't
    empty; otherwise it goes to `IdleState` and calls `Clear()`;
  - the retreat countdown ticks in `Ship.Tick()` only while `Current == Retreat`;
  - `GuardState`, `HuntState` and `AttackMoveState` exit to `IdleState` when `Current` no longer matches.
- Expose `ShipOrderType CurrentOrder` read-only on `IShipEntity`, for the enemy AI.
- Clear the model on ship release.

Order behaviour:
- **Move**: existing `NavigateState`. It never switches to `AttackTargetState`.
- **Attack**: existing `AttackTargetState`, with the target from the order.
- **Stop**: `Ship.Stop()`, in this order:
  1. `_orderModel.Clear()`. A retreat in its countdown never fires, and a waypoint path or retreat already
     underway doesn't continue.
  2. `_weaponComponent.ResetTarget()` **explicitly**, so a main target set by Attack, Guard, Hunt,
     Attack-Move or an ability is dropped even when no state transition runs `Exit`.
  3. `_stateMachine.SetState(_idleState)`: stops movement and holds position, still firing at enemies in
     range. If `StateMachine1.SetState` skips `Enter` for the current state, also call
     `_shipMoveComponent.Stop()` directly.
- **Attack-Move**: new `AttackMoveState`. Navigate. Each update, if the radar has an engageable enemy,
  attack it with the `AttackTargetState` logic; extract the `IdleState.CanEngage` rule into a shared helper
  rather than copying it. When the target dies or leaves the radar, continue. On arrival go to `IdleState`.
- **Waypoint Move**: queue in the model; `NavigateState` handles each leg; after the last one → `IdleState` + `Clear()`.
- **Guard**: new `GuardState`. Keep the guard offset around the friendly; engage enemies within the guard
  radius; if a chase goes past `maxChaseDistance` from the friendly, reset the main target and return.
  If the friendly is destroyed → `IdleState` + `Clear()`.
- **Hunt**: new `HuntState`. Pick the nearest known enemy of the ship's opposite side (fog-visible for the
  player; any radar/locator-known enemy for the opponent), attack it, and repeat; with none left → idle.
- **Retreat**: Phase 3.
- Tunables (guard/follow radius, chase distance, hunt re-target interval, retreat countdown) go in a new
  `UnitOrderSettings` ScriptableObject under `Assets/Settings`. Create the asset and inject it.

AI brain per side:
- Player ships: every order calls `_shipAIBrain.Enable(false)`, as today.
- Opponent ships: every order calls `_shipAIBrain.Enable(true)`. `ShipAIBrain` becomes a **tactical
  override** only. It reads the attack target from `ShipOrderModel` instead of its own `_assignedTarget`.
  It may switch to `FleeState` on low shields, and when fleeing ends it calls `Ship.ResumeOrder()`, which
  re-enters the state matching `ShipOrderModel.Current` (`Idle` for `None`). It never writes the order model.
- Remove `IShipEntity.AssignMoveTarget / AssignAttackTarget / HoldPosition`, `Ship._opponentMoveTarget`,
  `_hasOpponentMoveTarget`, `ResumeOpponentNavigation()` and `StartOpponentNavigation()` once Phase 4
  migrates their callers. The order model replaces them.

Stationary units (station, defend platform; **not** mining facility):
- New `StationaryAttackCommand : IFocusFireCommand, IStopCommand` wrapping `IWeaponComponent` +
  `IAttackDataFactory`: `FocusFire` calls `AddTarget(..., AttackType.MainTarget)` and `Stop` calls
  `ResetTarget()`. Bind it in `SpaceStationInstaller` and `DefendPlatformInstaller` for both sides.
- Remove the selection-driven main-target code from `StationCombatPresenter.UpdateState` (keep the
  radar-driven `HandleEnemyAdded`). `UnitOrderService.IssueAttack` sends `IFocusFireCommand` to stationary
  receivers. Remove the `HasSelectedStationaryWeapon` workaround in `UnitOrderFeedbackUiController`.

Extend `IUnitOrderService` with `IssueStop`, `IssueAttackMove`, `IssueGuard`, `IssueWaypointMove`,
`IssueHunt` (all taking `IReadOnlyList<IEntity> receivers`), following the receiver table above.

## Phase 2 — Player action UI panel + targeting flow

1. `UnitActionId` enum (`Move, Attack, AttackMove, Stop, Guard, WaypointMove, Hunt, Retreat`) under
   `Entities/UnitActions/` (namespace `EmpireAtWar.Entities.UnitActions`, folders `Model/`, `Ui/`,
   `Controller/` like other features).
2. `UnitActionButton` (MonoBehaviour): `[SerializeField] UnitActionId actionId; Button button; Image icon; GameObject pendingHighlight;`
   `UnitActionsView : MonoBehaviour, IUnitActionsView` sits on the `ShipActions` GameObject with
   `[SerializeField] List<UnitActionButton> buttons;` and a `TMP_Text` for the retreat countdown.
   Interface: `event Action<UnitActionId> ActionPressed; void SetVisible(bool); void SetAvailable(UnitActionId, bool); void SetPending(UnitActionId?); void SetRetreatCountdown(float? seconds);`
3. Prefab. Use the Unity API, then refresh/reserialize/save per `AGENTS.md`:
   - Rename the buttons through Unity: `AttackMode→Attack`, `AttackOnMove→AttackMove`, `Stop`, `Guard`,
     `CustomTrajectory→WaypointMove`, `GoAround→Hunt`. Duplicate one each for `Move` and `Retreat` (8 total).
   - Icons: create 8 sprites under `Assets/Art/Ui/UnitActions/` (simple placeholder glyphs following
     `UI_UX_GUIDELINES` sizes/import settings) and assign them to each `Icon` image.
   - Add `UnitActionsView` + `UnitActionButton`s and wire every serialized reference. Expose the view from
     `CoreGameUi` via `[SerializeField] UnitActionsView unitActionsView` + `ICoreGameUi.UnitActionsView`,
     keeping the uncommitted `SetContentLayout` changes.
4. `UnitActionsPresenter` (pure C#, `IInitializable, ILateDisposable, IObserver<ISelectionSubject>`). It
   gets the view through `CoreGameUiController`, which creates `CoreGameUi`; no lookups.
   - Availability on a player selection change: `Move→IMoveCommand`,
     `Attack→IAttackCommand|IFocusFireCommand`, `AttackMove→IAttackMoveCommand`, `Stop→IStopCommand`,
     `Guard→IGuardCommand`, `WaypointMove→IWaypointMoveCommand`, `Hunt→IHuntCommand`,
     `Retreat→IRetreatCommand` (and the battle hasn't ended). Hide the panel when nothing is available.
     Cancel any pending action.
   - Immediate actions (`Stop`, `Hunt`, `Retreat`) call `IUnitOrderService` with the selection's entities.
     Stop also cancels any pending action, including a waypoint placement in progress (the placed points
     are discarded).
   - Targeted actions (`Move`, `Attack`, `AttackMove`, `Guard`, `WaypointMove`) only set
     `UnitActionTargetingModel.Pending`. `PlayerOrderInputHandler` interprets the next click:
     - Move / AttackMove: space (not an obstacle, not a unit) → issue, then clear pending;
     - Attack: enemy → issue, then clear pending;
     - Guard: a friendly with `IEntitySelectionCommand` → issue, then clear pending;
     - a click on an invalid target is ignored and the action stays pending.
   - Escape (`IInputService.OnEscapePressed`) or pressing the same button again cancels. Entering action
     targeting calls `ShipAbilityService.CancelTargeting()`; ability targeting starting
     (`TargetingChanged` → waiting) cancels the pending action.
   - **Waypoint Move**: the button enters placement mode. Each right-click on space appends a point and
     shows its marker. Pressing the button again issues `IssueWaypointMove` if there's at least one point,
     then leaves the mode. Escape cancels without issuing.
     **Alt shortcut**: Alt + right-click with nothing pending enters placement mode implicitly and appends;
     more Alt + right-clicks append; releasing Alt finishes like the button. Add to `IInputService`
     `bool IsWaypointModifierPressed` + `event Action OnWaypointModifierReleased`.
5. Feedback: extend `IUnitOrderFeedbackUi` as needed (waypoint markers, guard marker), driven by `OrderIssued`.

## Phase 3 — Retreat

- `IUnitOrderService.IssueRetreat(IReadOnlyList<IEntity> receivers)`:
  1. resolve the destination once, from the receivers' `PlayerType` (the first receiver's side; receivers
     are always one side),
  2. compute compact formation destinations around it for receivers with `IRetreatCommand`,
  3. call `Retreat(destination, settings.RetreatCountdown)` on each.
- Destination for side `S`: if `IEntityLocator.IsStationOperational(S)`, use
  `MapData.GetStationPosition(faction of S)`, with the formation offset out of the station's collider so
  ships don't path into it. Otherwise use the center of the **default reinforcement zone**: the
  non-capturable zone with `StartingOwner == S`. Add `bool TryGetDefaultZoneCenter(PlayerType, out Vector3)`
  to `IReinforcementZonesSystem` (expose `StartingOwner` / `IsCapturable` on the zone presenter if needed).
  The destination is fixed when the order is issued.
- Per ship: `Retreat` → `Replace(Retreat, destination, delay)`. That drops the previous order's data, but
  the current state machine state continues during the countdown: a fighting ship keeps fighting, and a
  moving ship finishes its current leg and then goes idle. When the countdown ends, navigate like Move (no
  stopping to engage). On arrival → `IdleState` + `Clear()`. The ship stays controllable; no despawn, no
  battle end.
- `IsRetreatPending` = `Current == Retreat` and the countdown hasn't finished. `CancelRetreat()` = `Clear()`
  only when `Current == Retreat`.
- Player cancel: pressing Retreat when **every** selected retreat-capable ship is `IsRetreatPending` calls
  `CancelRetreat()` on them; otherwise it (re)issues. Stop or any other order replaces the retreat, both
  during the countdown and while underway.
- UI: while any selected ship is `IsRetreatPending`, the presenter shows the smallest `RetreatRemaining` on
  the Retreat button, updated from a tick.

## Phase 4 — Enemy global AI uses the same order API

Goal: the enemy strategy layer issues the **same actions** as the player, through `IUnitOrderService`.
The strategy decisions themselves (`EnemyStrategicDecisionModel`) don't change.

1. Receivers: the executor needs `IEntity`s. Add `long EntityId { get; }` to `IShipEntity`, set from the id
   `EntityInstaller` binds (`BindEntityExt(id)`). Resolve with `IEntityLocator.GetEntity(id)` in
   `EnemyStrategicContextBuilder` so `EnemyStrategicContext` carries each ship's `IEntity` next to its
   `IShipEntity`. If that id isn't injectable into `Ship` without a cycle, inject `Lazy<IEntity>` into
   `Ship` and expose `IEntity Entity` instead. Pick whichever compiles cleanly.
2. `EnemyTaskForceExecutor` depends on `IUnitOrderService` and maps each strategic state to actions:

   | `EnemyStrategicState` | Committed ships | Uncommitted ships |
   |---|---|---|
   | `CaptureZone` | `IssueAttackMove` to the capture zone (was plain move; now they fight what they meet) | `IssueStop` |
   | `HuntFleet` | `IssueAttack(EnemyFleetTarget, stable battle offsets)`; if no target is known → `IssueHunt` | `IssueStop` |
   | `AssaultBase` | `IssueAttack(EnemyBaseTarget, stable battle offsets)` | `IssueStop` |
   | `DefendBase` | `IssueGuard(OwnBase)` with stable battle offsets; if `OwnBase == null` → `IssueStop` | `IssueStop` |
   | `RetreatValue` | `IssueRetreat(all ships)` (station, or default zone if the station is dead; this also removes today's `OwnBase` null risk) | — |
   | `Hold`, `RebuildFleet` | `IssueStop` | — |

   Keep the executor's stable `BattleFormationModel` offsets and pass them through the explicit-offset
   overloads. Keep the fastest-first sort for capture.
3. Re-issue policy: the executor runs every decision. Ship idempotency (Phase 1) absorbs repeats of
   Move / Attack / AttackMove / Guard / Hunt / Retreat. For Stop, the AI issues it only to ships whose
   `IShipEntity.CurrentOrder != None`, so an idle ship's main target isn't reset every tick.
4. `EnemyUnitCommander.MoveShipsOutOfDefaultZone`: `AssignMoveTarget` → `IssueMove` (explicit destination
   overload), `HoldPosition` → `IssueStop` (same `CurrentOrder != None` rule).
5. Delete the now-unused `AssignMoveTarget`, `AssignAttackTarget`, `HoldPosition` and the opponent move
   fields on `Ship` (see Phase 1).
6. Update existing enemy tests (`EnemyShipAbilityControllerTests`, any executor/commander tests) and their
   fakes to the new API.

AI usage guidelines (document them as XML comments on the executor):
- The global AI decides **what** and **who**; ship states and the tactical brain decide **how**.
- Prefer Attack-Move over Move whenever enemies may be on the route; plain Move is for repositioning only
  (leaving the spawn zone).
- Use Guard, not Move, to defend a structure, so defenders return after chasing.
- Use Hunt only as the fallback when the target is unknown.
- Retreat is issued once per decision change; idempotency keeps the countdown from restarting.

## Tests (write, do not run)

Edit-mode tests under `Assets/Scripts/Tests/Editor/`, using the fakes in `SelectionInputTests` as the model:
- `UnitOrderService`: each `IssueX` calls only the receivers passed in; receivers without the command are
  skipped; formation vs explicit-slot overloads; `OrderIssued` carries the issuer side; the retreat
  destination uses the station, or falls back to the default zone when the station is dead, per side.
- `PlayerOrderInputHandler`: right-click priority (ability > pending action / Alt > default).
- `UnitActionsPresenter`: availability matrix, pending/cancel, mutual exclusion with abilities, waypoint
  toggle and Alt flows; Stop cancels a pending placement.
- Receiver scope, for **every** action: one of three selected → only that one; two of three → only those two.
- `ShipOrderModel`: `Replace` wipes previous data; `Clear` empties; the idempotency check.
- Ship Stop: resets the main target and clears the order, whatever the state. Stop mid-waypoint → no next
  waypoint. Stop during a retreat countdown → no retreat. Stop mid-retreat → idle.
- `EnemyTaskForceExecutor`: the state → action mapping table above, stable offsets passed through, and
  Stop not re-issued to ships already at `CurrentOrder == None`.
- Prefab: `UnitActionsView` has exactly one button per `UnitActionId` and every serialized reference assigned.

## Definition of done (Codex checks this itself)

1. Unity recompiles with **no errors** in the console (`unity` CLI / `unity mcp`).
2. Every changed prefab/asset (`CoreGameUi.prefab`, `UnitOrderSettings` asset, icon sprites, ship data)
   is refreshed, re-serialized by explicit path, and saved per `AGENTS.md`, with no import errors.
3. No references remain to the removed APIs (`AssignMoveTarget`, `AssignAttackTarget`, `HoldPosition`,
   `DispatchAttack`, `ShipUiController.MoveToPosition`, the old ship command classes).
4. Exactly one `ShipInput` subscriber handles orders (`PlayerOrderInputHandler`).
5. The tests above are written and compile.
6. The final summary lists the changed files and any class that ended up over 200 lines.
