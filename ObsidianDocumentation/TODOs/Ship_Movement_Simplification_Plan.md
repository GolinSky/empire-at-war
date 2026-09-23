# Ship movement simplification plan

- Created: 2026-09-23 · Revised: 2026-09-23 (rev 2)
- Status: Phases 0–2 **done and verified**. The old Phase 3 ("split into Model / Presenter / View") is **rejected by the user**. It added layers instead of removing them. This revision replaces it: Phase 3 now **merges the movement code back into one `ShipMoveComponent : MonoComponent<ShipMoveModel>`** that follows the project's component pattern. Phases 4–5 are small follow-ups.
- Audience: Codex. Read `AGENTS.md` first. Check every claim against the live source before you edit.
- Out of scope: `ShipAvoidancePlanner`, `ShipDestinationRegistry`, AI (`ShipAIBrain`), weapons, audio, `Ship.cs` behavior.

> **Guiding rule for this revision:** the goal is *fewer* types, files, and lines. Adding an interface, a wrapper, or a pass-through class is not simplification. If a change adds a type, it must remove more than it adds.

---

## 1. Done (Phases 0–2): keep as is

- **Phase 0:** all 10 `*ShipView.prefab` serialize unprefixed fields (`hyperSpaceEase`, `lineRenderer`, `bodyTransform`, `logNavigationDecisions`). `FormerlySerializedAs` was removed.
- **Phase 1:** StarDestroyer2 root is at the hull center: `BodyPivot` ≈ `(0, 1.47, 0)` and collider center ≈ `(0, 1.47, 0)`. Other ships were measured (table at the end). StarDestroyer1 (7.89) and HeavyDreadnought (8.98) are still off-center and **wait for user approval**.
- **Phase 2:** per-frame yaw stepper with acceleration (`ShipRotationKinematics.StepYaw`), bank from actual yaw rate (`CalculateBankFromYawRate` + `SmoothDampAngle`), `TurnAcceleration` data field, turn-radius-bounded Bezier handles. Rotation tweens are gone. DOTween is used only for the hyperspace jump.

Do not redo or refactor any of this, apart from the class merge in Phase 3.

---

## 2. Review of the Phase 3 split (what went wrong)

The ship's movement code used to live in one class (`ShipMoveComponent`, 555 lines). It is now spread across 5 types and ~1,050 lines:

| File | Lines | Problem |
| --- | --- | --- |
| `ShipMovePresenter.cs` | 473 | A plain C# class that implements `IMonoComponent`, bound `AsSingle`, and is not a `MonoComponent<T>`. It breaks the component pattern every other entity component follows. It is not shorter than the original, and the old logic moved over nearly unchanged. |
| `ShipMoveView.cs` | 95 | Pure pass-through: every method forwards 1:1 to `ShipMovementTweenPlayer`. It also exposes getters that exist only for the presenter (`ShipName`, `LogNavigationDecisions`, `RootTransform`). |
| `IShipMoveView.cs` | 28 | A 13-member interface that exists only to join the two halves. |
| `ShipMovementTweenPlayer.cs` | 280 | Fine. This is the real motion code. |
| `ShipVisionRegistration.cs` | 46 | A new class for two lines of fog-of-war registration. |
| `ShipMoveModel.cs` | 211 | Holds **two sources of truth** for state: `Phase` **plus** 5 nullable destinations (`requested`, `accepted`, `queued`, `deferred`, `blocked`) **plus** `IsHyperSpaceComplete`, `_hasTargetPosition`, and `_targetPosition`. The events `PhaseChanged`, `DestinationAccepted`, and `Stopped` have **no subscribers**. `IsMoving(position)` is dead code. |

Other leftovers:
- `ShipMovePresenter.Tick` polls `_view.IsTurningToRoute` to flip the model phase. The call chain is presenter → view → tween player → presenter.
- The unused interface members are still there: `ViewTransform`, `CalculateLookDirection`, and the duplicate `MoveToPosition(Vector2)` overload.
- `SetMediator` still uses `?? throw new ArgumentNullException`. Other components just assign.
- Script GUID `675872bfa3914db5aaf6d0f8c8d65ba8` now belongs to `ShipMoveView.cs`, and the prefabs' `m_EditorClassIdentifier` still says `ShipMoveComponent`.
- `SerializedReferenceTests.cs` references both `ShipMoveComponent` and `ShipMoveView` type names.

---

## 3. Project component rules (read the code; this is the pattern to follow)

Reference implementations: `Components/Radar/RadarComponent.cs`, `Components/Selection/SelectionComponent.cs`, `Components/Health/HealthComponent.cs`, base `Components/BaseSystem/MonoComponent.cs`.

1. `public class XComponent : MonoComponent<XModel>, IXComponent, IInitializable[, ITickable/IFixedTickable][, ILateDisposable]`.
2. `IXComponent : IComponent` (+ `IUnitComponent` when it has `SetMediator(IUnitMediator)`). The interface is declared in its own file for this component.
3. Dependencies come through `[Inject] private void Construct(XModel model, ...)`, which calls `SetModel(model)` first and then assigns fields. There is no constructor on the MonoBehaviour.
4. State and rules live in `XModel : PureModel` (pure C#, bound `Container.Bind<XModel>().AsSingle()` in the installer). The component reads it through the protected `Model` property.
5. Unity references are `[SerializeField] private` unprefixed camelCase fields on the component itself.
6. Lifecycle: `Initialize()` sets things up. `public override void Release()` cleans up and is idempotent through `_isReleased`. `LateDispose()` calls `Release()`.
7. Mediator: `public void SetMediator(IMediator mediator) { _mediator = mediator; }`, a plain assignment.
8. Installer: `Container.BindInterfacesAndSelfTo<XComponent>().FromComponentsInHierarchy().AsCached();`.
9. Helpers that are pure math or playback may be plain internal classes (`ShipRotationKinematics`, `ShipMovementTweenPlayer`), owned and constructed by the component. **No view/presenter split and no view interface.**

---

## 4. Phase 3 (new): merge back into one `ShipMoveComponent`

### 3.1 Target file set in `Assets/Scripts/Components/Ship/Movement/`

| Keep / create | Delete |
| --- | --- |
| `ShipMoveComponent.cs` (MonoComponent, GUID `675872bf…`) | `ShipMovePresenter.cs` + `.meta` |
| `IShipMoveComponent.cs` (slimmed, 3.5) | `ShipMoveView.cs` (renamed, see 3.2) |
| `ShipMoveModel.cs` (slimmed, 3.4) | `IShipMoveView.cs` + `.meta` |
| `MovementPhase.cs` | `ShipVisionRegistration.cs` + `.meta` |
| `ShipMovementTweenPlayer.cs` → rename class and file to `ShipMotion` (optional, and only through Unity so the `.meta` follows) | |
| `ShipRotationKinematics.cs`, `ShipBezierPath.cs`, `ShipBezierRoute.cs`, `ShipAvoidancePlanner.cs`, `IShipMoveData.cs` unchanged | |

Net result: −4 types, about −400 lines.

### 3.2 Keep the prefab script reference (do this first)

1. Rename `ShipMoveView.cs` → `ShipMoveComponent.cs` **through Unity** (`AssetDatabase.RenameAsset`) so the `.meta` and GUID `675872bfa3914db5aaf6d0f8c8d65ba8` stay the same. Then rename the class inside to `ShipMoveComponent`.
2. Keep the serialized field names exactly as they are: `hyperSpaceEase`, `lineRenderer`, `bodyTransform`, `logNavigationDecisions`. The prefabs already use them, so no `FormerlySerializedAs` is needed.
3. Delete `ShipMovePresenter.cs`, `IShipMoveView.cs`, and `ShipVisionRegistration.cs` through Unity (`AssetDatabase.DeleteAsset`).
4. After the code compiles: `AssetDatabase.Refresh()`, then `ForceReserializeAssets` on the 10 ship prefabs **by explicit path**, then `SaveAssets()`. This refreshes `m_EditorClassIdentifier`. Verify with `grep -n "lineRenderer:\|bodyTransform:" Assets/Prefabs/Models/Ships/*ShipView.prefab`: 10 hits each, same fileIDs as before.

### 3.3 `ShipMoveComponent` shape

```csharp
public class ShipMoveComponent : MonoComponent<ShipMoveModel>, IShipMoveComponent,
    IShipNavigationAgent, IInitializable, ITickable, ILateDisposable
{
    private const float MINIMUM_NAVIGATION_RADIUS = 1f;
    private const float HEIGHT_TOLERANCE = 0.5f;

    [SerializeField] private Ease hyperSpaceEase;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform bodyTransform;
    [SerializeField] private bool logNavigationDecisions;

    // injected: ICameraService, Vector3 startPosition, PlayerType, IMapModelObserver,
    //           IStationFacingService, IShipNavigationService, FogOfWarSystem, IRadarModelObserver
    // owned:    ShipMovementTweenPlayer _motion (constructed in Initialize)

    [Inject] private void Construct(ShipMoveModel model, ...) { SetModel(model); ... }

    public void Initialize() { /* spawn resolve, pose, register nav, hyperspace, fog register (Player only) */ }
    public void Tick()       { _motion.Tick(Time.deltaTime); }
    public void LateDispose() => Release();
    public override void Release() { /* idempotent: nav unregister, fog unregister (Player), _motion.Release() */ }
    ...
}
```

Rules:
- Read `transform` directly (`transform.position`, `transform.forward`, `name`, `logNavigationDecisions`). Remove every `_view.X` indirection.
- Fog of war comes back into `Initialize`/`Release` from `ShipVisionRegistration`. Do **not** move it into `RadarComponent`, because that component is shared with DefendPlatform, MiningFacility, and SpaceStation.
- **Remove the Turning polling.** Delete the check in `Tick` that reads `IsTurningToRoute` and calls `Model.StartMoving()`. Also remove `Turning` from the model's `IsMoving`, because callers only need "moving or not" (see 3.4). If `Turning` has no remaining reader, delete it from `MovementPhase` and delete `IsTurningToRoute` from the motion helper's public API.
- `SetMediator(IShipMovementMediator mediator) { _movementMediator = mediator; }`, a plain assignment like Radar and Selection. Keep the mediator itself. It matches the entity's pattern, and replacing it is out of scope.
- Keep the request logic (dedup, queue while arriving, blocked retry on radar contacts, pursue-deferred, re-plan on arrival if the spot is occupied) **behaviorally identical**, but write it against the slimmed model (3.4). Do not add new branches.
- Size target: **≤ 300 lines**. If it is over, first look for duplicated branches between `SetTargetPosition`, `UpdateTargetPosition`, and `ApplyNavigationPlan`. They share the "arriving vs ready" split and can become one `Plan(destination, mode)` method. Do not extract new classes to hit the number.

### 3.4 Slim `ShipMoveModel` (one source of truth)

`Phase` is the state. Keep **one** pending slot, because only one of queued, blocked, or deferred can be meaningful at a time:

```csharp
public sealed class ShipMoveModel : PureModel
{
    public MovementPhase Phase { get; private set; } = MovementPhase.Arriving;
    public NumericsVector3 Destination { get; private set; }        // accepted target
    public NumericsVector3? PendingDestination { get; private set; } // Arriving: queued · Blocked: retry · Moving: deferred (pursue)
    public NumericsVector3? LastRequest { get; private set; }        // dedup of identical orders
    public bool IsMoving => Phase == MovementPhase.Moving;          // (+ Arriving with pending, if callers need it; keep today's semantics)
    public bool IsBlocked => Phase == MovementPhase.Blocked;
    // data passthroughs: Speed, Height, RotationSpeed, TurnAcceleration, HyperSpaceDuration, BodyRotationMaxAngle, NavigationRadius
    // spawn pose: ConfigureSpawnPose, JumpPosition, StartRotation, HyperSpacePosition (unchanged)
    // transitions: Request, Accept, Block, Defer, TakePending, FinishArrival, Arrive, StopAt, ApplyMoveCoefficient
}
```

Delete: `IsHyperSpaceComplete` (it is `Phase != Arriving`), `_hasTargetPosition`/`_targetPosition`/`TargetPosition`/`SetTargetPosition`/`HasTargetPosition` (use `Destination`), `IsMoving(position)`, `AcceptedDestination`/`RequestedDestination`/`QueuedDestination`/`DeferredDestination`/`BlockedDestination`, `ReserveDestination`, `ClearPendingDestinations` (fold into `StopAt`/`Accept`), and the unused events `PhaseChanged`, `DestinationAccepted`, `Stopped`.

**Before collapsing the three slots into one**, check every current write site in `ShipMovePresenter` (`QueueDestination`+`BlockDestination` in arriving are the same value; `DeferDestination` only happens while moving). If any path needs two slots at once, stop and report instead of guessing.

Target: model ≤ 120 lines. Update `Tests/Editor/ShipMoveModelTests.cs` to the new API with the same scenarios: arriving→queue→idle→move, move→blocked→retry→move, stop from each phase, pursue-deferred. Write the tests; **do not run them** unless the user asks.

### 3.5 Slim `IShipMoveComponent`

Remove `ViewTransform` and `CalculateLookDirection` (no callers), plus the public `MoveToPosition(Vector2)` overload on the class (inline it into `MoveToPositionOnScreen`). Update the fake in `Tests/Editor/NavigateStateTests.cs`. Keep all other members and their signatures. Callers (`Ship`, the states, `ShipAIBrain`) must not change.

### 3.6 Installer and tests

- `ShipInstaller.BindComponents`: replace the three lines (`ShipMoveView`, `ShipMovePresenter`, `ShipVisionRegistration`) with
  `Container.BindInterfacesAndSelfTo<ShipMoveComponent>().FromComponentsInHierarchy().AsCached();`
- `Tests/Editor/SerializedReferenceTests.cs`: drop `SHIP_MOVE_VIEW_FULL_NAME` and point the check at `ShipMoveComponent` (`lineRenderer`, `bodyTransform`).
- `Tests/Editor/ShipNavigationServiceTests.cs`: fix any references to the removed types or model members.
- Grep for leftovers: `grep -rn "ShipMovePresenter\|ShipMoveView\|IShipMoveView\|ShipVisionRegistration" Assets/Scripts` must return nothing.

### 3.7 Acceptance

- Movement is one MonoComponent + one model + helpers. No `IShipMoveView`, no presenter.
- `ShipMoveComponent` ≤ 300 lines, `ShipMoveModel` ≤ 120 lines.
- Compiles with no console errors, and the prefabs keep their references (3.2 step 4).
- The manual checklist (§6) passes. Behavior is identical to the end of Phase 2.

---

## 5. Follow-ups (only after Phase 3 is accepted)

**Phase 4, route-line selection** (small): `HandleSelection` stays on the interface because it matches how `Ship.OnSelect` fans out today. No change is needed unless the user asks.

**Phase 5, navigation service signature** (optional, ask first): drop the `heightTolerance`/`clearance` params from `ShipNavigationService.Plan`/`TryResolveInitialFinalPosition` (always `0.5` and `agent.NavigationRadius`), and replace the `preserveCourse`/`reserveAsPending` bools with one enum. Update `ShipNavigationServiceTests`. Do not add new types beyond that one enum.

**Pivot fixes for StarDestroyer1 / HeavyDreadnought**: same procedure as Phase 1 (use the main hull mesh or collider center, and shift every root child and the collider). **User approval required first.**

---

## 6. Rules and manual checks

- Use Serena for C#. Use Unity tooling for asset rename, delete, and reserialize, and never hand-edit prefab YAML. After asset changes: `Refresh` → `ForceReserializeAssets(<explicit paths>)` → `SaveAssets` → check the console.
- `const` in UPPER_SNAKE_CASE, `_camelCase` private fields, unprefixed `[SerializeField]`. One type per file. No `GetComponent`/`Find`. No constructor null guards.
- Do not run tests unless asked. One commit for Phase 3, message prefixed `movement:`.

Manual checklist (the user runs it):
- [ ] Skirmish loads, and all ship types hyperspace in without exceptions.
- [ ] Move order: route line shows when selected, the ship turns smoothly, follows the route, arrives, and returns to Idle.
- [ ] An order given during hyperspace entry runs after arrival.
- [ ] Blocked destination resumes after a radar update.
- [ ] Attack: pursues, stops in range, turns broadside smoothly.
- [ ] Engines destroyed → ship slows.
- [ ] SD2 rotates around its hull center.

## Measurements

| Ship | Bounds center local (x, z) | Offset | Measured yaw deg/s | Data RotationSpeed |
| --- | --- | --- | --- | --- |
| Acclamator | (0.00, 0.87) | 0.87 | | 30 |
| Arquitens | (0.00, 0.31) | 0.31 | 17.2 sampled while turning | 45 |
| HeavyDreadnought | (3.35, 8.33) | 8.98 | | 7.5 |
| Lucrehulk | (0.00, −2.31) | 2.31 | | 2.5 |
| Munificent | (−0.02, 0.52) | 0.52 | | 35 |
| Providence | (0.00, 0.94) | 0.94 | | 7.5 |
| Recusant | (0.00, 0.00) | 0.00 | | 25 |
| StarDestroyer1 | (0.00, 7.89) | 7.89 | | 5 |
| StarDestroyer2 (before) | (2.71, 9.50) | 9.88 | 4.5–4.6 sampled while turning | 5 |
| Venator | (0.01, −1.09) | 1.09 | | 7.5 |

StarDestroyer2's aggregate renderer bounds are skewed by the TrenchPlating and Base meshes. Phase 1 used the collider center (0.1206, 10.4224). After the edit, the collider center is (0, 0) in xz and the HullPlatingSml center is (−0.12, −0.05).
