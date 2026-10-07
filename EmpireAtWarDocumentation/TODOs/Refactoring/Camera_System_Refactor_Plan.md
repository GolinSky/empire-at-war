---
category: Refactoring
status: in-progress
created: 2026-09-30
---
# Camera System Refactor Plan

## Goal
- Remove the silent `_camera.enabled` fallback from `CameraInput`; the input lock is enforced by the consumer.
- Move `CameraPanSmoothing` out of camera code into a generic math utility.
- Fix default scroll-zoom direction.
- Turn `CinematicCameraSettings` into its own `Data` ScriptableObject using `[field: SerializeField]` auto-properties.

## Files
- `Assets/Scripts/Services/Camera/CameraInput.cs`
- `Assets/Scripts/Services/Camera/CameraService.cs` (235 lines — over 200, see TODO)
- `Assets/Scripts/Components/Utils/VelocitySmoothing.cs`, `Assets/Scripts/Tests/Editor/VelocitySmoothingTests.cs`
- `Assets/Scripts/Services/Camera/CinematicCameraData.cs`, `CinematicClassProfile.cs`, `CameraData.cs`
- `Assets/Settings/Data/Models/Camera/CinematicCameraData.asset` (cinematic tuning); `CameraData.asset` (manual camera tuning)
- `Assets/Settings/Input/EmpireAtWar.inputactions` (`Camera` map)
- Consumers: `CinematicCameraPresenter`, `CinematicInterestScorer`, `Tests/Editor/CinematicCameraModelTests.cs`
- Installer: `SkirmishServiceInstaller` (`BindScriptableObject<CameraData>(Repository)`)

## 1. `CameraInput` disabled-map guard

### Facts
- Removed both `_camera.enabled` fallbacks; `CameraService` now enforces the lock.
- `InputLockService` disables the `Camera` + `Battle` maps while any lock is held: `CinematicCameraPresenter.Enter`, `MiniMapController`, `ReinforcementService` placement.
- `CameraService.Tick()` returns before reading `Move` when locked; `LockChanged(true)` clears residual velocity.
- Disabled actions already return default: `IsPressed()` → `false`, `ReadValue<T>()` → `default`. So keyboard/drag/zoom are inert without the guard.
- Only leak without the guard: edge scroll reads `IPointerInput.Position` (Pointer map stays enabled) → camera would drift during cinematic/minimap/placement.

### Decision
- Chosen: `CameraService` owns the lock rule. Inject `IInputLock`; on `LockChanged(true)` reset `_keyboardVelocity`; skip movement in `Tick()` while `IsLocked`.
- Remove both `_camera.enabled` checks from `CameraInput`.
- Why: one explicit owner of "camera does not move while locked"; input reader stops masking a caller bug.
- Avoid: re-adding the check elsewhere in `CameraInput`, or a `Debug.Assert`-only guard.

### Steps
1. [x] Inject `IInputLock` into `CameraService.Constructor`; subscribe/unsubscribe `LockChanged` in `Initialize`/`LateDispose`.
2. [x] `Tick()`: return before reading `Move` while `_inputLock.IsLocked`; lock → `_keyboardVelocity = Vector2.zero`.
3. [x] Delete the two `if (!_camera.enabled)` blocks in `CameraInput`.
4. [ ] Manual: enter cinematic / drag minimap / reinforcement placement with pointer on screen edge → camera must not move; release → edge scroll resumes, no residual glide.

## 2. `CameraPanSmoothing` → generic utility

### Facts
- `VelocitySmoothing.MoveTowardsTarget(current, input, maxSpeed, accel, decel, dt)` has no camera knowledge: clamp input → target velocity → `Vector2.MoveTowards` with accel/decel rate.
- Single caller: `CameraService.Tick()`. Tests: `VelocitySmoothingTests` (3 cases; renamed, not executed).
- Shared math lives in the external `com.script-utils.math` package (`GolinSky/ScriptUtilities`, namespace `Utilities.ScriptUtils.Math`); in-project helpers live in `Assets/Scripts/Components/Utils/`.

### Decision
- Chosen: rename to `VelocitySmoothing` (static, `MoveTowardsTarget(...)`) in `Assets/Scripts/Components/Utils/`; rename tests to match.
- Decision: user chose the in-project `Assets/Scripts/Components/Utils/` location; do not upstream into `com.script-utils.math`.
- Move via Unity-aware tooling so `.meta` GUIDs survive.

### Steps
1. [x] Move/rename file + type; update `CameraService` call and namespace import.
2. [x] Rename `CameraPanSmoothingTests` → `VelocitySmoothingTests`.
3. [x] Compile clean.

## 3. Scroll zoom direction

### Facts
- Bindings: `ZoomScroll` = `<Mouse>/scroll/y` (`invert` processor); `Zoom` = 1D axis `R` negative, `F` positive.
- `CameraService.ZoomCamera`: `newPosition = CameraPosition - CameraForward * delta * speed * dt`.
- Wheel forward → binding negates positive `scroll.y` → camera moves along `forward` → zoom in.
- `R` → zoom in, `F` → zoom out; flipping the sign in `ZoomCamera` would also swap the keys.

### Decision
- Fix only the wheel: add the `invert` processor to the `ZoomScroll` binding in `EmpireAtWar.inputactions`. Keep `ZoomCamera` math and `R`/`F` unchanged.
- `InvertZoom` player setting keeps working as a toggle on top.

### Edge Cases
- Players who enabled `InvertZoom` to work around the bug will now get inverted zoom → acceptable; setting still toggles.
- `InputBindingService.RestoreOverride` applies path-only overrides; it does not override processors. Runtime rebind confirmation remains pending.

### Steps
1. [x] Add `invert` processor to `ZoomScroll` binding; regenerate `GameInputActions.cs` if it changes.
2. [ ] Manual: wheel forward zooms in; `R` zooms in; `F` zooms out; `InvertZoom` on reverses wheel + keys.

## 4. `CinematicCameraSettings` → `CinematicCameraData`

### Facts
- `CinematicCameraData : Data` owns 13 scalar auto-properties and `ClassProfiles`; each uses `[field: SerializeField]` and `= default`.
- `CinematicCameraPresenter` receives `CinematicCameraData` directly; `CinematicInterestScorer` and model-test fixtures use the new type.
- `BindScriptableObject<T>` loads by type name → needs an Addressable entry with address `CinematicCameraData` in the `Data` group.
- Serialized values were copied from `CameraData.asset` into `CinematicCameraData.asset` (min 3, max 8, linger 1.5, sample 0.5, sharpness 3, wide 2.5, damageMemory 4, damageWeight 6, radius 250, enemyWeight 1, maxEnemies 5, repeatPenalty 0.35, jitter 0.3, 7 class profiles).

### Implementation
```csharp
[CreateAssetMenu(fileName = nameof(CinematicCameraData), menuName = "Data/Cinematic Camera Data")]
public class CinematicCameraData : Data
{
    [field: SerializeField] public float MinShotDuration { get; private set; } = default;
    [field: SerializeField] public float MaxShotDuration { get; private set; } = default;
    // ... remaining 11 scalar properties use = default
    [field: SerializeField] public CinematicClassProfile[] ClassProfiles { get; private set; } = default;

    public CinematicClassProfile GetClassProfile(ShipClass shipClass) { /* unchanged */ }
}
```
- `CinematicClassProfile` → `[field: SerializeField] public ShipClass ShipClass { get; private set; }` etc.; keep constructor.

### Steps
1. [x] Rename type/file `CinematicCameraSettings` → `CinematicCameraData : Data`; convert fields to `[field: SerializeField]` auto-properties initialized to `default`; tuning lives in the asset.
2. [x] Convert `CinematicClassProfile` fields the same way.
3. [x] Remove `Cinematic` from `CameraData`.
4. [x] Create `Assets/Settings/Data/Models/Camera/CinematicCameraData.asset` via Unity API, copying the current values above; `SetDirty` + `SaveAssets`.
5. [x] Add Addressable entry (address `CinematicCameraData`) to the `Data` group via Unity API — no group-structure changes.
6. [x] `SkirmishServiceInstaller`: `Container.BindScriptableObject<CinematicCameraData>(Repository);`.
7. [x] `CinematicCameraPresenter`: inject `CinematicCameraData` instead of reading `cameraData.Cinematic`; drop `CameraData` if unused.
8. [x] Tests: create `CinematicCameraData` with `ScriptableObject.CreateInstance`, copy serialized asset tuning in setup, destroy the fixture in teardown.
9. [x] Re-serialize `CameraData.asset` so stale `<Cinematic>k__BackingField` is dropped; check console for import errors.
10. [ ] Manual: start cinematic in skirmish → shot lengths 3–8 s; resized ships fit the frame through all shot types.

## Implementation
- Implemented sections 1–4 on 2026-09-30; manual acceptance steps remain unchecked.
- Unity finished compilation: `isCompiling=false`, `scriptCompilationFailed=false`; no new console errors after refresh and asset migration.
- Confirmed all 13 scalar values and seven profiles match the pre-migration snapshot; new asset is saved and imported.
- Confirmed Addressables entry `CinematicCameraData` in existing `Data` group; no folder/group structure changes.
- Confirmed moved script/test GUIDs preserved; `CameraData.asset` no longer contains the cinematic backing field.
- `GameInputActions.cs` regenerated by Unity; only wheel processor changed. `git diff --check` passed.
- Automated tests were not run. Serena reported stale `ICameraPreferences` diagnostics; Unity compilation succeeded.

## Cinematic Framing Fix — 2026-10-07
- Fixed class distances were too close for resized ships. Moving units now use `IMoveFacade.NavigationRadius` and `ICameraService.FieldOfView` to calculate a minimum framing distance; class distance remains the lower bound.
- `CinematicShotSolver.CalculateFramingDistance`: `max(classDistance, radius × 1.25 / (sin(FOV / 2) × 0.9))`. Radius is the existing navigation clearance proxy, not measured renderer bounds.
- Chase and low/high shots aim at the subject anchor; distance-scaled forward aim offsets previously pushed the hull toward the frame edge.
- Verification: `CinematicCameraModelTests` 10/10 passed; all five shot types, both sides and three progress values tested at radii 10/90/290/1060 and FOV 30°/60°. Unity compilation and scoped `git diff --check` passed; no new console errors.
- In-game visual acceptance remains unchecked. Structures retain class framing. No assets or controls changed.

## Cinematic Clipping Fix — 2026-10-07
- Reproduced cropped hulls during opposite-side transitions with the previous straight-line `Vector3.Lerp` path: both regression cases failed at transition step 4/20.
- `CinematicShotSolver.InterpolatePose` now interpolates camera offsets around the current look point with `Vector3.Slerp`; radius never drops below the destination shot distance. Re-aiming at the look point avoids rotation lag exposing the frame edge.
- Wide shots add focus-offset clearance based on camera FOV, keeping the selected hull framed when the focus lies between units.
- Verification: `CinematicCameraModelTests` 17/17 passed; 4,000 transition samples, offset-wide shots and recovery from a subject reaching the camera position. Unity compilation and scoped diff checks passed.
- Navigation radius remains the hull-size proxy. This does not add collision avoidance against unrelated nearby ships or scenery; Play Mode visual acceptance remains pending.

## TODO
- Manual acceptance remains pending. On 2026-10-07 `MainMenuScene` was clean; EditMode tests ran, Play Mode was not entered.
- Remaining: lock/release behavior for cinematic, minimap drag and reinforcement placement; wheel/R/F/invert/rebind behavior; cinematic shot timing and framing.
- `CameraService` is now 235 lines. Suggest splitting pan/zoom/clamp into a pure `CameraRig` model — **not in scope without approval**.
- `ZoomCamera` early-returns when `newPosition.y` is out of range, so the following `Clamp` is dead and zoom stops short of the bounds. Out of scope; flag.
- No automated tests run unless requested.
