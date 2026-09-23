# Ship movement simplification plan

- Created: 2026-09-23
- Status: Phases 0–2 implemented and manually verified on 2026-09-23. Phase 3 is in progress; the first runtime check found a view initialization-order error, now fixed and awaiting a skirmish retry. Phases 4–5 pending. User directed removal of `FormerlySerializedAs` after all ten ship prefabs were rewired. Phase 6 remains optional.
- Audience: Codex (or any implementing agent). Read `AGENTS.md` first. This note is advisory, so check every claim against the live source before you edit.
- Scope: `Ship`, `IShipMoveComponent`, `ShipMoveComponent`, `ShipMovementTweenPlayer`, `ShipRotationKinematics`, `ShipMoveModel`, the `ShipNavigationService` API surface, rotation feel, and the `StarDestroyer2ShipView` pivot.
- Out of scope: `ShipAvoidancePlanner` internals, `ShipDestinationRegistry` internals, AI decision logic (`ShipAIBrain`, `ShipAiDecisionModel`), weapons, and audio implementations.

Do the phases **in order**. Each phase must compile, persist its Unity assets, and leave behavior verified before the next one starts. Phases 0–2 fix bugs the user can see. Phases 3–5 are structural simplification. Do not run automated tests unless the user asks for them (see `AGENTS.md` → Unity Test Safety). Where this plan says to "write tests", write them and leave them for the user to run.

---

## 1. Current state (evidence)

### 1.1 Files and sizes

| File | Lines | Role today |
| --- | --- | --- |
| `Assets/Scripts/Components/Ship/Movement/ShipMoveComponent.cs` | 555 | MonoBehaviour: input conversion, navigation request state machine, nav-service registration, fog-of-war registration, broadside look policy, mediator callbacks, logging, Numerics conversions |
| `Assets/Scripts/Components/Ship/Movement/ShipMovementTweenPlayer.cs` | 288 | DOTween playback of hyperspace, path, look-at, bank, route line |
| `Assets/Scripts/Components/Ship/Movement/ShipRotationKinematics.cs` | 144 | Static turn and bank math |
| `Assets/Scripts/Components/Ship/Movement/ShipMoveModel.cs` | 76 | Thin pure model: speed, target position, spawn pose |
| `Assets/Scripts/Components/Ship/Movement/IShipMoveComponent.cs` | 29 | 17-member interface |
| `Assets/Scripts/Entities/Ship/Ship.cs` | ~380 | Entity controller. Also acts as movement mediator and runs opponent move-target logic |
| `Assets/Scripts/Services/ShipNavigation/ShipNavigationService.cs` | 414 | Plan, registry, and spawn resolution |
| `Assets/Scripts/Services/ShipNavigation/ShipRoutePlanner.cs` | 203 | Route and detour selection |
| `Assets/Scripts/Components/Ship/Movement/ShipBezierPath.cs` | 194 | Route geometry |

Callers of `IShipMoveComponent`: `Ship.cs`, `IdleState`, `NavigateState`, `AttackTargetState`, `FleeState`, `ShipAIBrain` (only `IsMoving`), and the fake in `Tests/Editor/NavigateStateTests.cs`.

### 1.2 Architecture issues

1. **God component.** `ShipMoveComponent` carries eight responsibilities (listed in the table above). Its request state is spread across **6 nullable/bool fields**: `_pendingTargetPosition`, `_deferredTargetPosition`, `_blockedTargetPosition`, `_lastRequestedDestination`, `_lastAcceptedDestination`, `_isNavigationReady`. Three more booleans (`_isReleased`, `_isNavigationRegistered`, `_hasBroadsideDirection`) add to it. The ship's real phase (arriving / idle / turning / moving / blocked / deferred) is never named. It has to be inferred from field combinations. `SetTargetPosition` (lines ~216–258) and `UpdateTargetPosition` (~410–439) both branch on those combinations.
2. **MVP violation.** The business rules (request dedup, pending-until-hyperspace-done, blocked retry on radar update, deferred-until-path-end) live in the MonoBehaviour. `ShipMoveModel` holds only a target position, and `IsMoving` is computed by comparing positions within tolerance (`ShipMoveModel.IsMoving`, `POSITION_TOLERANCE = 0.05`). There is no explicit state.
3. **Fat interface (`IShipMoveComponent`).**
   - Unused: `CalculateLookDirection`, `ViewTransform`. The only implementer outside production code is the test fake.
   - Wiring concerns exposed as commands: `SetMediator`, `HandleSelection`, `HandleRadarContacts`, `ApplyMoveCoefficient`.
   - Input concern: `MoveToPositionOnScreen(Vector2)` is routed through a second public overload `MoveToPosition(Vector2)` that is not on the interface. Screen→world conversion belongs to the input/UI side, not to movement.
   - `bool preserveCourse` flag parameter on `MoveToPosition`.
4. **Mediator round-trip.** `ShipMoveComponent` → `IShipMovementMediator` (implemented by `Ship`) → `IAudioDialogShipComponent`. `Ship` only forwards (`OnPositionChanged`, `OnLookAtTarget`, `OnStopped`). This could be model events that the audio side subscribes to.
5. **`Ship` does movement bookkeeping.** `CompleteNavigation()` polls `IsMoving || IsBlocked` every tick to leave `NavigateState`. `_opponentMoveTarget`, `_hasOpponentMoveTarget`, `ResumeOpponentNavigation`, and `StartOpponentNavigation` are AI-side concerns inside the entity.
6. **Navigation service API uses flag arguments.** `Plan(agent, forward, requestedDestination, obstacleContacts, heightTolerance, clearance, mapRange, preserveCourse, reserveAsPending)` takes 9 parameters. `clearance` always equals `agent.NavigationRadius` and `heightTolerance` is always `HEIGHT_TOLERANCE = 0.5`. The two bool flags produce `IsDeferred`/`IsStationary` results, which the caller then re-interprets.
7. **Two tween sequences write the same `transform.rotation`.** `_rotationSequence` (look-at) and `_translationSequence` (path turn and route follow) both rotate the root. `StopPath()` kills only the translation sequence. `PlayPath()` kills both. The look-at tween restarts whenever the target direction changes by more than 1°. That happens almost every frame while the target moves, and each restart resets the body-bank tween.
8. **Bank math duplicated three ways.** `CalculateBankAngle` (per-frame), `CalculateLookBankAngle` (tween target), and the inline tween in `PlayPath`.
9. **Fail-fast noise and rule conflicts.** Constructor-style `?? throw new ArgumentNullException` in `SetMediator` and null checks in `HandleRadarContacts` and `ShipNavigationService.Plan` conflict with the `AGENTS.md` guidance on routine defensive checks (the constructor rule does not apply literally, but the spirit does). `Initialize()` has a `//todo: move to test` for the serialized-reference checks.
10. **Unrelated responsibility.** Fog-of-war vision-source registration sits in `ShipMoveComponent.Initialize()`.
11. **Numerics conversion helpers are private statics** (`//todo: move this to utils`).

### 1.3 Critical: serialized field rename in the working tree

The uncommitted diff on `ShipMoveComponent.cs` renamed `_lookAtEase/_hyperSpaceEase/_lineRenderer/_bodyTransform/_logNavigationDecisions` to unprefixed camelCase **and removed** the `[FormerlySerializedAs]` attributes. All 10 prefabs that use the component still serialize the old keys (for example, `StarDestroyer2ShipView.prefab` lines ~929–934: `_lineRenderer`, `_bodyTransform`, …):

```
Acclamator, Arquitens, HeavyDreadnought, Lucrehulk, Munificent, Providence,
Recusant, StarDestroyer1, StarDestroyer2, Venator  (Assets/Prefabs/Models/Ships/*ShipView.prefab)
```

After a Unity reload, `lineRenderer` and `bodyTransform` load as `null` and `Initialize()` throws `InvalidOperationException` for **every ship**. Phase 0 fixes this.

### 1.4 StarDestroyer2 wrong rotation center: root cause

Hierarchy of `StarDestroyer2ShipView.prefab`:

```
StarDestroyer2ShipView (root: ShipMoveComponent, BoxCollider, LineRenderer)   pos (0,0,0)
├── BodyPivot            localPos (0.1206, 1.4700, 10.4224)   <- _bodyTransform (bank/roll only)
│   └── StarDestroyer (model prefab 4de91b60…) localPos (-0.1206, -1.4700, -10.4224)
├── SelectedCanvas       localPos (0, 0, 7.46)
├── Weapon               localPos (0, 0.77, 6.46)   (+ hardpoint instances)
├── lamps3export         (prefab instance, lamps children)
├── ShieldGenerator      localPos (-0.01, 3.62, 1.27)
├── Engines              localPos (0, 0.02, -0.85)
└── AudioViewComponent   (prefab instance)
BoxCollider.m_Center = (0.1206, 1.4700, 10.4224), size (13.77, 7.16, 24.36)
```

- The earlier "fix body pivot" commit (`dd23473f`) added `BodyPivot` at the hull center with the model counter-offset. That fixes **roll (bank)**, which rotates `bodyTransform`.
- **Yaw is applied to the root transform.** `ShipMovementTweenPlayer` rotates `_rootTransform`, and `ShipRotationKinematics.Step` writes `root.rotation`. The mesh origin is still at the root, and the hull center is **10.42 units forward** of the root. The ship therefore yaws around a point near its stern and sweeps its bow through a 10-unit-radius arc. The navigation position (`transform.position`), radar position, destination registry, and selection all use that stern point too.
- `StarDestroyer1ShipView` probably has the same problem: collider center `(−0.026, 4.876, 7.812)`. Other ships have small offsets (<2.2 units). Lucrehulk (z 2.2) and Acclamator (z 1.5) are borderline.

### 1.5 "Rotation is too fast": probable causes

Data (`Assets/Settings/Data/Ship/*ShipData.asset`, `RotationSpeed` in deg/s): Arquitens 45, Munificent 35, Acclamator 30, Recusant 25, Venator/Providence/HeavyDreadnought 7.5, SD1/SD2 5, Lucrehulk 2.5.

Candidates, in order of likely effect. **Measure before tuning** (Phase 2.1).

1. **Off-center yaw pivot (SD1/SD2).** The bow moves at `ω·r`. At 5°/s with r = 10.4, the bow sweeps about 0.9 u/s, which is close to the ship's forward speed (1.2). Even a slow turn looks like a fast swing.
2. **No angular acceleration.** Turn-in-place uses `DORotateQuaternion(...).SetEase(Ease.Linear)` in `PlayLookAt` and in the `PlayPath` pre-turn, so the ship starts and stops at full rotation speed instantly. Route following uses `Quaternion.RotateTowards` at full rate every frame. Capital ships feel snappy and weightless.
3. **Bank saturates.** `CalculateBankAngle` divides the requested turn by `degreesPerSecond * deltaTime`. At 60 FPS and 5°/s, that step is 0.08°, so any heading error above 0.08° yields full `BodyRotationMaxAngle` bank, and bank flips sign abruptly. Body roll also uses `RotationSpeed` as its rate. Aggressive banking reads as fast rotation.
4. **Look-at restarts.** In Idle and Attack states, `LookAtTarget` is called every frame. Each >1° change kills the tween and restarts yaw and bank from zero ease, which causes jitter.
5. **Route curvature ignores turn radius.** `BuildDirectRoute` (non-turnaround branch) and `BuildAvoidanceRoute` use fixed control-point factors (0.35 / 0.15) that do not depend on `minimumTurnRadius`. Short, sharp curves make the tangent change faster than `RotationSpeed`. The heading then lags and the hull "crabs", while the path line suggests a snap turn.
6. **Data values** for the small ships (25–45°/s) may simply be high. Tune them only after 1–5 are fixed.

---

## 2. Target design

Follow MVP from `AGENTS.md`. Keep namespaces: movement in `EmpireAtWar.Components.Ship.Movement`, navigation in `EmpireAtWar.Services.ShipNavigation`.

```
ShipMovementModel (pure C#, no UnityEngine; System.Numerics like today)
  - Phase: MovementPhase { Arriving, Idle, Turning, Moving, Blocked }
  - Destination (accepted), RequestedDestination (what the order asked for)
  - Speed/RotationSpeed/TurnAcceleration/BankMaxAngle/NavigationRadius/Height (from IShipMoveData)
  - SpeedCoefficient
  - events: PhaseChanged, DestinationAccepted(Vector3 numerics), Stopped, FacingRequested
  - rules: request dedup, queue-while-Arriving, retry-while-Blocked, deferred-while-Turning

ShipMovementPresenter (pure C#, IInitializable / ITickable / ILateDisposable)
  - implements IShipMoveComponent (slim) + IShipNavigationAgent
  - owns: IShipNavigationService calls, model transitions, drives IShipMoveView
  - no DOTween, no Transform

IShipMoveView  <- ShipMoveView (MonoBehaviour, replaces ShipMoveComponent on prefabs)
  - Position, Forward (read)
  - SetPose(position, yawRotation), SetBank(angle)
  - PlayHyperSpace(from, to, duration, onComplete)
  - ShowRoute(Vector3[]), HideRoute(), SetRouteVisible(bool)
  - serialized: bodyTransform, lineRenderer, hyperSpaceEase

ShipMotionDriver (pure C#, per-frame kinematics; replaces ShipMovementTweenPlayer path/look tweens)
  - heading + angularVelocity with max rate and acceleration
  - follows route (arc-length) or turns in place to a facing
  - bank = f(actual yaw rate), smoothed
```

Slim `IShipMoveComponent` (target):

```csharp
public interface IShipMoveComponent : IComponent
{
    Vector3 CurrentPosition { get; }
    bool IsMoving { get; }          // Phase is Turning or Moving
    bool IsBlocked { get; }         // Phase == Blocked
    float NavigationRadius { get; }
    float NavigationSpeed { get; }
    float HyperSpaceDuration { get; }
    void MoveTo(Vector3 destination);            // plain order: replan, may turn in place
    void Pursue(Vector3 destination);            // today's preserveCourse: true
    void FaceBroadside(Vector3 targetPosition);  // today's LookAtTarget
    float GetRange(Vector3 targetPosition);
    void Stop();
}
```

Removed from the interface: `ViewTransform`, `CalculateLookDirection`, `MoveToPositionOnScreen`, `SetMediator`, `HandleSelection`, `HandleRadarContacts`, `ApplyMoveCoefficient`. Each gets an explicit home in the phases below. This is the target, not a prescription for Phase 3. Phases 3–5 get there incrementally.

---

## 3. Phases

### Phase 0: Restore serialized references (blocking bug)

1. In `ShipMoveComponent.cs`, re-add `using UnityEngine.Serialization;` (already imported) and annotate:
   ```csharp
   [FormerlySerializedAs("_lookAtEase")] [SerializeField] private Ease lookAtEase;
   [FormerlySerializedAs("_hyperSpaceEase")] [SerializeField] private Ease hyperSpaceEase;
   [FormerlySerializedAs("_lineRenderer")] [SerializeField] private LineRenderer lineRenderer;
   [FormerlySerializedAs("_bodyTransform")] [SerializeField] private Transform bodyTransform;
   [FormerlySerializedAs("_logNavigationDecisions")] [SerializeField] private bool logNavigationDecisions;
   ```
2. `unity command eval --code 'UnityEditor.AssetDatabase.Refresh();'`, then wait for the recompile.
3. Re-serialize **only** the 10 ship prefabs listed in §1.3 with `AssetDatabase.ForceReserializeAssets(new[]{...paths...})` and `AssetDatabase.SaveAssets()`.
4. Verify on disk that each prefab now contains `lineRenderer:` / `bodyTransform:` (unprefixed) with the same fileIDs as before. `grep -n "_lineRenderer\|_bodyTransform" Assets/Prefabs/Models/Ships/*.prefab` must return nothing.
5. Check the Unity console for import or serialization errors.
6. Leave the `FormerlySerializedAs` attributes in place. The Phase 3 view rename removes them together with the component.

Acceptance: no prefab keeps `_`-prefixed keys, and entering a skirmish spawns ships without `InvalidOperationException` (manual check by the user).

### Phase 1: Fix StarDestroyer2 yaw pivot (asset-only)

Goal: the root transform (yaw pivot and navigation position) sits at the hull's horizontal center. Roll stays around `BodyPivot`.

1. **Measure first** with a read-only eval. Load the prefab contents, compute the union of all `Renderer.bounds` under the root, and log its center in root-local space (`root.InverseTransformPoint(bounds.center)`). Expect about `(0.12, ~1.47, 10.42)`, the same as the collider center. Use the measured value as `Δ = (dx, 0, dz)` (horizontal only; keep Y so `Height` data stays valid).
2. Apply one Unity-API mutation (`PrefabUtility.LoadPrefabContents` → edit → `SaveAsPrefabAsset` → `UnloadPrefabContents` → `SaveAssets`):
   - For **every direct child of the root** (BodyPivot, SelectedCanvas, Weapon, lamps3export, ShieldGenerator, Engines, AudioViewComponent, and any other), set `localPosition -= Δ`. The model inside `BodyPivot` keeps its counter-offset. Hardpoints under `Weapon` move with their parent.
   - `BoxCollider.center -= Δ`, which gives about `(0, 1.47, 0)`.
   - Do not touch the root transform or any component other than those above.
   - Expected result: `BodyPivot ≈ (0, 1.47, 0)`, `Weapon ≈ (−0.12, 0.77, −3.96)`, `SelectedCanvas ≈ (−0.12, 0, −2.96)`, `Engines ≈ (−0.12, 0.02, −11.27)`, `ShieldGenerator ≈ (−0.13, 3.62, −9.15)`.
3. Re-run the measurement. The renderer bounds center must be within `|x|,|z| ≤ 0.25` of the root.
4. Check whether any other asset references positions on this prefab's children by offset: minimap marker (`MiniMapUnitMarkerPresenter` uses the root transform, so it is fine), health bar and selection canvas (children, so they move), and radar (root position). Nothing else should need changes. Report anything unexpected.
5. **Audit the other ships** with the same read-only measurement for all 10 prefabs. Produce a table of `name | bounds center local xz | offset magnitude`. **Do not fix other prefabs in this phase.** StarDestroyer1 (≈7.8 z) is the expected second offender. List it for the user to approve.
6. Optional (write, do not run): EditMode test `Tests/Editor/ShipPrefabPivotTests.cs` that loads each `*ShipView.prefab` and asserts the renderer bounds center local `xz` magnitude ≤ `NavigationRadius * 0.1` (or 1.0). Mark the prefabs the user has not approved as `[Ignore("pending pivot fix")]`.

Acceptance: in play mode, SD2 turns in place around its visual middle, with the bow and stern sweeping symmetric arcs. The selection ring and route line start at the hull center.

### Phase 2: Rotation feel (turn rate, acceleration, bank)

2.1 **Measure** (temporary; guard with the existing `logNavigationDecisions` flag and remove it at the end of the phase):
- In `ShipMovementTweenPlayer`, sample root yaw once per second and log `actualYawRate` (deg/s), the model's `RotationSpeed`, and the body bank angle.
- Ask the user to reproduce "too fast" on SD2 and one small ship (Arquitens). Record the numbers in this note under *Measurements*.

2.2 **Angular acceleration.** Add `TurnAcceleration` (deg/s²) to `IShipMoveData` / `ShipData` (`[field: SerializeField]`, with a default equal to `RotationSpeed` so a ship reaches full rate in about 1 s). Add to `ShipRotationKinematics`:
```csharp
public static Quaternion StepYaw(
    Quaternion current, Vector3 targetDirection,
    ref float angularVelocity,        // signed deg/s, persisted by caller
    float maxRate, float acceleration, float deltaTime)
```
- Signed remaining angle `θ` from `Vector3.SignedAngle(currentForward, target, up)`.
- Braking: desired `ω* = sign(θ) · min(maxRate, sqrt(2·acceleration·|θ|))`.
- `angularVelocity = MoveTowards(angularVelocity, ω*, acceleration·dt)`; rotate by `angularVelocity·dt` about up. Snap and zero when `|θ| < 0.1°` and `|ω| < acceleration·dt`.
- Write unit tests in `ShipRotationKinematicsTests.cs`: it never exceeds maxRate, it never overshoots, it reaches the target in finite time, and it accelerates from 0.

2.3 **Bank from actual yaw rate.** Replace `CalculateBankAngle` and `CalculateLookBankAngle` with one function:
`bank = -BodyRotationMaxAngle * Clamp(angularVelocity / RotationSpeed, -1, 1)`. Smooth the body with `Mathf.SmoothDampAngle` (bank smoothing time constant `BANK_SMOOTH_TIME = 0.6f`) instead of `RotateTowards(…, RotationSpeed·dt)`. Delete the two old functions and their tests, then update the remaining tests.

2.4 **Replace rotation tweens with the stepper.**
- `PlayLookAt` stops creating DOTween sequences. It stores a desired facing, and a per-frame `Tick(dt)` steps yaw with `StepYaw`. A new facing call only changes the target and does not reset velocity or bank. That removes the restart jitter from §1.5 (item 4).
- The `PlayPath` pre-turn (`plan.TurnDuration > 0`) uses the same stepper. The path follow waits until `|θ| < 1°`, then starts route progress. `TurnDuration` from the planner becomes an estimate only (UI/logging).
- Route following: position still advances by arc length (`EvaluateNormalizedDistance` with `progress += speed·dt / route.Length`), and yaw uses `StepYaw` toward the tangent.
- The root rotation now has **one writer**. `_rotationSequence` is deleted, and the translation sequence remains only for hyperspace.
- Driving needs `Tick`: `ShipMoveComponent` implements Zenject `ITickable` (the container already binds interfaces via `BindInterfacesAndSelfTo<ShipMoveComponent>`). Use `Time.deltaTime` once per tick.

2.5 **Curvature bounded by turn radius.** In `ShipBezierPath.BuildDirectRoute` (the `Dot ≥ 0` branch) and `BuildAvoidanceRoute`, scale control handles to at least `minimumTurnRadius * QUARTER_CIRCLE_CONTROL_FACTOR` (capped at the segment length × 0.5) so the minimum radius of curvature is about `minimumTurnRadius`. Pass `minimumTurnRadius` from `ShipRoutePlanner.Build` into `BuildAvoidanceRoute` (new parameter). Delete the unused `ShipBezierPath.BuildDirect` / `BuildAvoidance` array helpers. Update `ShipAvoidancePlannerTests.cs:120`, which uses `BuildAvoidance`, to call `BuildAvoidanceRoute(...).Samples`.

2.6 **Data tuning (only after 2.1–2.5, with user sign-off).** Propose new `RotationSpeed` / `TurnAcceleration` values in a table here based on the measurements. Starting suggestion: small ships ×0.6 (Arquitens 45→27, Munificent 35→21, Acclamator 30→18, Recusant 25→15). Keep capitals unchanged. `TurnAcceleration = RotationSpeed` for small ships and `RotationSpeed × 0.5` for capitals. Edit the `.asset` files through Unity API (`SerializedObject`), then `SetDirty` + `SaveAssets`.

Acceptance: measured yaw rate never exceeds `RotationSpeed`. Turns ease in and out. Bank tracks turn rate and no longer flips. SD2 facing a moving target turns smoothly without stutter.

### Phase 3: Split ShipMoveComponent into Model / Presenter / View

Precondition: Phases 0–2 merged. Keep behavior identical and make only structural changes here.

3.1 **Numerics helpers.** Move `ToNumerics`/`ToUnity` into `Assets/Scripts/Utilities/.../NumericsConversionExtensions.cs` (find the existing `Utilities.ScriptUtils.Math` namespace, reuse it, and follow `PROJECT_ORGANIZATION`). Make them extension methods: `v.ToNumerics()`, `q.ToUnity()`.

3.2 **Model: `ShipMoveModel` gains explicit state.** Add `MovementPhase` enum (its own file) and move the request rules out of the component:

| Today (component fields) | Model concept |
| --- | --- |
| `_isNavigationReady == false` + `_pendingTargetPosition` | `Phase == Arriving`, `QueuedDestination` |
| `_blockedTargetPosition` | `Phase == Blocked`, `RequestedDestination` |
| `_deferredTargetPosition` | `DeferredDestination` (only while `Turning`/`Moving` in pursue mode) |
| `_lastRequestedDestination`/`_lastAcceptedDestination` dedup | `RequestedDestination` + `Destination`; method `IsSameRequest(requested)` |
| `IsMoving` by position tolerance | `Phase is Turning or Moving` |

- Events: `event Action<MovementPhase> PhaseChanged; event Action<NumericsVector3> DestinationAccepted; event Action Stopped;`.
- Pure C#, no `UnityEngine`. Extend `ShipMoveModelTests.cs` with transition tests (Arriving→queue→Idle→Moving, Moving→Blocked→radar update→Moving, Stop from each phase).

3.3 **View: `ShipMoveView : MonoBehaviour, IShipMoveView`.** It keeps only Transform, body, LineRenderer, and hyperspace tween. Serialized: `bodyTransform`, `lineRenderer`, `hyperSpaceEase` (and `lookAtEase`, if still used after Phase 2; it probably is not).
- **Swap the script on the 10 prefabs without losing references.** The preferred approach is to keep the **same script GUID**: rename the class and file `ShipMoveComponent.cs` → `ShipMoveView.cs` together with its `.meta` through Unity (`AssetDatabase.RenameAsset`/`MoveAsset`). Serialized fields with the same names then carry over, and the `FormerlySerializedAs` attributes from Phase 0 can be deleted after a reserialize. Verify the GUID `675872bfa3914db5aaf6d0f8c8d65ba8` is unchanged.
- The view keeps `MonoComponent<T>` only if the `IMonoComponent` lifecycle (`EntityComponentLifecycle` in `Ship`) requires it. Check `EntityComponentLifecycle` and `MonoComponent` before deciding.

3.4 **Presenter: `ShipMovePresenter`** (pure C#; constructor injection of `ShipMoveModel`, `IShipMoveView`, `IShipNavigationService`, `IMapModelObserver`, `IStationFacingService`, `PlayerType`, `Vector3 startPosition`, `IRadarModelObserver` if still needed).
- Implements `IShipMoveComponent`, `IShipNavigationAgent`, `IInitializable`, `ITickable`, `ILateDisposable`.
- Owns `ShipMotionDriver` (the Phase 2 stepper extracted from `ShipMovementTweenPlayer`), nav-service registration and unregistration, and the obstacle contact list.
- The broadside choice (`LookAt` lines ~328–366) moves here as `FaceBroadside`.
- `ShipMovementTweenPlayer` is deleted once the driver and view cover it.

3.5 **Installer.** In `ShipInstaller.BindComponents`:
```csharp
Container.BindInterfacesAndSelfTo<ShipMoveView>().FromComponentsInHierarchy().AsCached();
Container.BindInterfacesAndSelfTo<ShipMovePresenter>().AsSingle();
```
Ensure `IShipMoveComponent` resolves to the presenter only (the view must not implement it). Check the `Vector3 startPosition` binding source in `DynamicEntityInstaller` before moving the injection.

3.6 **Fog of war.** Move `_fogOfWarSystem.RegisterVisionSource(...)` out of movement into the component that owns vision: `RadarComponent`, or a tiny `ShipVisionRegistration` initializable bound for `PlayerType.Player`. Pass the view transform explicitly (the `EntityBindType.ViewTransform` id binding exists). Also unregister on release if the API supports it. Check `FogOfWarSystem` first.

Acceptance: `ShipMoveView` < 120 lines, `ShipMovePresenter` < 200 lines, `ShipMoveModel` < 200 lines. No `GetComponent`/`Find`. All previous behaviors preserved: hyperspace entry, queued order during entry, blocked retry, pursue mode, stop, and route line on selection.

### Phase 4: Slim the interface and remove the mediator

1. Delete `CalculateLookDirection` and `ViewTransform` from the interface and the test fake.
2. **Screen orders.** Move the screen→world conversion to the caller. `Ship.MoveTo(Vector2)` (via `NavigateState.SetScreenDestination`) comes from `IUnitMediator`/player command. Convert with `ICameraService.GetWorldPoint(screen, currentPosition)` in the player command/controller that already has camera access, and call `MoveTo(Vector3)`. Then delete `NavigateState.SetScreenDestination`, `_screenDestination`, `_useScreenDestination`, and `MoveToPositionOnScreen`. Find the Vector2 caller chain first (`PlayerShipCommand`, `ShipUiController.MoveToPosition(Vector2)`), and confirm the camera service is reachable there. If it is not, inject it into the player command.
3. **`preserveCourse` → two verbs.** `MoveTo(Vector3)` and `Pursue(Vector3)`. Update `AttackTargetState` (lines ~166, ~215) to call `Pursue`. `FleeState` and `NavigateState` keep `MoveTo`.
4. **Mediator → events.** Delete `IShipMovementMediator`. The audio dialog side subscribes to model events: `DestinationAccepted` → `HandleMove`, `Stopped` → `HandleStopped`, and a `FacingRequested(target)` event → `HandleAttack`. Wire the subscription in `Ship.Initialize` (it already holds `_audioDialogShipComponent`, which is optional for opponents), or better in a small presenter bound only for `PlayerType.Player`. Remove `SetMediator` from the interface.
5. **Radar contacts.** `Ship.HandleRadarContacts` forwards to movement. Replace it by having the presenter subscribe directly to the radar model's contacts event, if `IRadarModelObserver` exposes one. Otherwise keep one explicit method `UpdateObstacles(IReadOnlyList<RadarContact>)` on the presenter, not on `IShipMoveComponent`. Remove the null check on `contacts` (routine defensive check).
6. **Selection.** `HandleSelection` → the view subscribes to `ISelectionModelObserver` (already bound in `ShipInstaller`) through the presenter. Remove it from the interface and from `Ship.OnSelect`.
7. **Engines.** `ApplyMoveCoefficient` → the presenter subscribes to the engines hardpoint (`HardPointModel.OnHardPointHealthChanged`), or `Ship` keeps calling a presenter method not exposed on `IShipMoveComponent`. Choose the smaller change and keep it explicit.
8. **Phase-driven completion.** Replace `Ship.CompleteNavigation()` polling with a subscription to `ShipMoveModel.PhaseChanged`: `Idle` while in `NavigateState` → `SetState(_idleState)`. Keep the opponent-resume logic as is in this phase.

Acceptance: `IShipMoveComponent` matches §2. `Ship.cs` no longer implements `IShipMovementMediator`. `NavigateStateTests` fake compiles with the slim interface.

### Phase 5: Navigation service API cleanup

1. Drop `heightTolerance` and `clearance` parameters from `Plan` and `TryResolveInitialFinalPosition`. Read `agent.NavigationRadius` inside, and move `HEIGHT_TOLERANCE = 0.5f` into the service as a constant.
2. Replace `preserveCourse`/`reserveAsPending` bools with an enum `ShipPlanMode { Immediate, Pursue, Reserve }`.
3. Replace `IsStationary`/`IsDeferred` bools on `ShipNavigationPlan` with `ShipPlanOutcome { Move, Deferred, Blocked }`.
4. Drop redundant `ArgumentNullException` checks on `agent`/`obstacleContacts` in `Plan` (`GetRegistrationId` already fails fast on unknown agents).
5. Move `IShipNavigationAgent` and `ShipNavigationPlan` into their own files (the one-type-per-file rule).
6. Update `ShipNavigationServiceTests.cs` accordingly.

Acceptance: `Plan(agent, forward, destination, contacts, mapRange, mode)` has 6 parameters. No bool flags.

### Phase 6 (optional, needs user decision): Opponent move-target out of Ship

`_opponentMoveTarget`, `_hasOpponentMoveTarget`, `ResumeOpponentNavigation`, and `StartOpponentNavigation` in `Ship.cs` belong to the AI (`ShipAIBrain`). Moving them is outside the movement scope. **Ask the user before doing it.**

---

## 4. Rules for the implementer

- Use Serena for C# symbol navigation and edits. Use Unity tooling (`unity command …`, official `unity mcp`) for prefabs and assets. Never hand-edit prefab YAML for structural changes.
- After every asset mutation: `AssetDatabase.Refresh()` → `ForceReserializeAssets(<explicit paths>)` → `SaveAssets()` → check the console.
- Naming: `const` → `UPPER_SNAKE_CASE`, private fields → `_camelCase`, `[SerializeField]` → unprefixed camelCase. One top-level type per file.
- No constructor null guards. No `?.` on required dependencies. No `GetComponent`/`Find`.
- Keep each commit to one phase. Commit messages should be prefixed `movement:` and list the phase number.
- Do not touch `Assets/AddressableAssetsData` or Obsidian config.
- Stop and ask the user when: another prefab needs a pivot fix (Phase 1.5), data values change (Phase 2.6), or Phase 6 starts.

## 5. Manual verification checklist (per phase, done by the user)

- [ ] Skirmish loads, and all ship types hyperspace in without exceptions.
- [ ] Right-click move: path line visible when selected, ship turns and follows, arrives, and state returns to Idle.
- [ ] Order during hyperspace entry is executed after arrival.
- [ ] Move into an obstacle or crowded area: blocked, then resumes after radar update.
- [ ] Attack order: pursue keeps course, ship stops in range and turns broadside smoothly.
- [ ] Engines hardpoint destroyed → ship slows.
- [ ] SD2 rotates around its hull center (Phase 1+).
- [ ] Yaw rate ≤ `RotationSpeed`, and turns ease in and out (Phase 2+).

## Measurements

_(fill during Phase 1.1, 1.5, and 2.1)_

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

StarDestroyer2's aggregate renderer bounds are skewed by the TrenchPlating and Base meshes. The collider center was (0.1206, 10.4224); the main HullPlatingSml mesh center was (0.00, 10.37). With user approval, Phase 1 used the collider center for the horizontal pivot offset. After the edit, collider center is (0.00, 0.00) in xz and HullPlatingSml center is (−0.12, −0.05). The aggregate renderer bounds threshold in Phase 1.3 is not a valid acceptance check for this model.
