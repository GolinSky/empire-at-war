# Battle performance review

Reviewed 2026-09-14 for low FPS in Unity Editor Play Mode. Gameplay fixes and ECS migration are recommendations only; the implementation in this change is diagnostic instrumentation.

**A wholesale ECS migration is premature.** The current ordinary projectile path already uses particles and reusable turret objects. The most useful first step is to distinguish CPU combat work, allocations, and rendering during the same battle. These are code and asset findings, not measured percentages of frame time.

**Observed environment.** Unity 6000.4.7f1 on Apple M2, High Fidelity quality, VSync 0, target frame rate -1, Game view 1618 × 891 at inspection. The Editor was outside Play Mode with a clean MainMenuScene open. No representative battle capture was taken. Frame Timing Stats is disabled in project settings, so GPU/frame-timing counters may be unavailable. Settings were not changed.

## Findings, in investigation order

**Highest priority: detached projectile objects outlive their owner.** `WeaponHardPointView.AttackCoroutine` calls `ResetParent` after each ordinary shot. `TurretView.ResetParent` sets `transform.parent = null`. The weapon retains these objects in `_turrets`, but neither the hardpoint nor `WeaponComponent.Release` cleans them up. Ship release/death animation only handles the ship and its hierarchy. Fired particle turrets can therefore remain active scene roots after their ship dies, consuming memory and receiving `Update` calls even after their busy timer expires. This can accumulate across deaths/waves in the same loaded scene; scene unload normally destroys these roots, so it is not a demonstrated leak across scene unloads. Laser turrets have a different inheritance path and stay parented.

Give detached VFX an explicit owner/pool lifecycle and return or destroy them after the final effect, including when their weapon is released. Preserve in-flight visuals and damage/cancellation semantics deliberately. To verify the cost, compare turret-update call counts before firing, during combat, and after ships die. Counts that remain high while ship-tick counts fall support this diagnosis. This cleanup should precede a Jobs/ECS migration.

Sources: [shot ownership](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/ViewComponents/Health/WeaponHardPointView.cs), [detachment](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/ViewComponents/Weapon/TurretView.cs), [weapon release](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/Weapon/WeaponComponent.cs), [ship release](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Entities/Ship/Ship.cs).

1. **Combat and spawn logging can add Editor overhead.** `LaserTurretView.OnTargetContact` logs every first contact, and `WeaponComponent.ApplyDamage` logs a warning when a delayed target is no longer valid. The latter can be an ordinary battle outcome when several shots target a dying hardpoint. Existing Console history also contains repeated spawn-time warnings: the latest 100 retained entries included 32 disabled-AudioSource warnings and 32 Zenject ShipComponentsData installation warnings. This is historical evidence of logging, not a measurement of current log rate or proof of sustained FPS loss. Check the CPU Timeline for logging/stack-trace work; consider removing routine contact logs and fixing noisy initialization paths before changing architecture.

   Sources: [laser contact](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/ViewComponents/Weapon/LaserTurretView.cs), [damage scheduling](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/Weapon/WeaponComponent.cs).

2. **The turret pool retains peak demand and scans all entries for each acquisition.** `WeaponHardPointView.GetTurret` searches `_turrets`, does not stop at the first available entry, and instantiates when all entries are busy. It loads/configures that new turret and retains it. `TurretView.Update` continues to be dispatched to retained components, with `LookAt` while busy. This is partial pooling already; it is not a fresh GameObject per ordinary shot. Expect spikes when pool demand rises and steady overhead from a large retained population. Measure acquisition versus instantiation counts before choosing prewarming, a free list, or active-only updates. Any pool limit needs an explicit visual/gameplay overflow policy.

   Sources: [pool acquisition and salvos](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/ViewComponents/Health/WeaponHardPointView.cs), [turret updates](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/ViewComponents/Weapon/TurretView.cs).

3. **Per-shot managed scheduling creates allocation and bookkeeping pressure.** The salvo coroutine allocates `WaitForSeconds` per shot, then uses `WaitWhile` and an `Any` scan to wait for busy turrets. Damage scheduling adds a callback and delayed coroutine per projectile; completed attacks are removed from a list. Higher shot counts mean more coroutine wakeups, allocations, and linear removals. Measure GC bytes/frame and coroutine/GC samples. If significant, use one simulation-time impact queue and batched salvo scheduling while preserving cancellation, target validation, pause, and time-scale behavior.

   Sources: [salvo coroutine](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/ViewComponents/Health/WeaponHardPointView.cs), [pending damage](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/Weapon/WeaponComponent.cs).

4. **Target selection can grow with ships × candidate hardpoints.** `WeaponComponent.Tick` attempts one weapon hardpoint in round-robin order and applies a successful-shot delay. `TryFireWeapon` then scans target groups and their hardpoints; `CanAttack` repeatedly reads transforms, computes distance, direction, and angle. Destroyed groups are skipped but stay in the additional-target list until release. `AttackData` and `HealthComponent.GetShipUnits` also materialize collections during target-data work; those are not necessarily per-frame allocations. Measure target-scan time and call counts. Candidate pruning, cached target IDs/positions, and staggered reacquisition are earlier steps than an ECS rewrite.

   Sources: [target scan](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/Weapon/WeaponComponent.cs), [range/angle checks](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/ViewComponents/Health/WeaponHardPointView.cs), [target data](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/AttackComponent/AttackData.cs), [hardpoint arrays](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/Health/HealthComponent.cs).

5. **Lasers and unit sensing add separate physics and update costs.** Each active laser growth/hold update can perform a raycast and update its LineRenderer endpoints. Radar uses `OverlapBoxNonAlloc` per ship on its timer and processes the resulting colliders; the nonallocating query does not eliminate query or filtering cost. Ships also tick navigation and state logic. Measure these separately. If radar dominates, stagger queries and narrow candidates; if laser visual raycasts dominate, evaluate less frequent visual collision checks while retaining required hit behavior.

   Sources: [laser update](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/ViewComponents/Weapon/LaserTurretView.cs), [radar](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/Radar/RadarComponent.cs), [ship tick](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Entities/Ship/Ship.cs), [attack movement](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Entities/Ship/StateMachine/AttackTargetState.cs).

6. **The rocket subclass has a latent cadence issue, but is not evidence of the current slowdown.** `RocketLauncherHardPointView.Attack` bypasses the base hardpoint busy state. However, its script has no serialized references and no dynamic construction was found. The `TurretType.Rocket` data entry does not activate this subclass. If it is adopted later, overlapping fire can inflate retained pool demand, although the outer fire delay and finite busy duration still constrain steady-state overlap. Do not describe this as a proven infinite leak.

   Source: [rocket launcher](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/ViewComponents/Health/RocketLauncherHardPointView.cs).

**Rendering may dominate independently of those C# paths.** `Projectile.prefab` and `DualProjectile.prefab` enable particle lights, capped at one light per particle system. More simultaneously active emitters can therefore add more lights. The referenced Point Light has shadows disabled, so these projectiles do not each introduce a shadow map. The active High Fidelity URP asset nevertheless enables HDR, depth texture, four shadow cascades, 1,000-unit shadow distance, large shadow atlases, and up to eight additional lights per object. Capture render/GPU work before attributing this to projectile simulation. Particle collisions and triggers are disabled on these prefabs; ordinary shots do not create a PhysX rigidbody per projectile.

The serialized DualProjectile is looping, but `TurretView.SetData` explicitly sets `main.loop = false` at runtime. Its serialized loop flag alone is not a runtime bug. Its culling behavior and retained renderer/light count are still useful to inspect under load. Particle count limits describe capacity, not the number actually alive.

Sources: [projectile prefab](/Users/golinsky/Projects/empire-at-war/Assets/Prefabs/Vfx/Projectile.prefab), [dual projectile](/Users/golinsky/Projects/empire-at-war/Assets/Prefabs/Vfx/DualProjectile.prefab), [point light](/Users/golinsky/Projects/empire-at-war/Assets/Prefabs/Light/Point%20Light.prefab), [active URP asset](/Users/golinsky/Projects/empire-at-war/Assets/Resources/Settings/URP-HighFidelity.asset).

## Jobs, DOTS, and ECS direction

Jobs/Burst can be adopted without converting the project to ECS. DOTS is the broader stack; Entities provides ECS. Jobs target parallel CPU work, so they will not by themselves solve transparent-particle overdraw or logging. [Unity Job System](https://docs.unity3d.com/6000.4/Documentation/Manual/job-system-overview.html), [Entities documentation](https://docs.unity3d.com/Packages/com.unity.entities@1.4/manual/index.html).

The current package lock resolves Burst 1.8.29 and Collections 6.4.0 transitively. Entities is not declared in the manifest or lock. No packages were added or upgraded for this review.

| Evidence from capture | First direction | When Jobs/ECS becomes useful |
| --- | --- | --- |
| Turret updates remain high after owners die | Fix detached-object ownership and release | ECS is not needed for lifecycle correctness |
| Instantiate spikes / repeated pool expansion | Prewarm measured capacity; improve pool acquisition and reuse | ECS is not needed to fix cold spawning |
| GC and coroutine work grows with shot rate | Batched salvo/impact scheduling in ordinary C# | Jobs if trajectory or collision math subsequently dominates |
| Target scan / unit simulation dominates | Stable IDs, packed snapshots, spatial candidates, staggered decisions | Jobs + Burst for independent numeric work over arrays |
| GPU / render submission dominates | Reduce effect overlap, emitter/renderer count, shadows, and material changes | ECS simulation alone will not fix this; rendering must change too |
| High entity counts remain CPU-bound after batching | Pilot one contained projectile system | Entities/ECS if storage, lifecycle, and scheduling benefits justify migration |

A projectile pilot should keep simulation state in contiguous data: position, velocity, target ID, remaining lifetime/impact time, damage parameters, and active state. Batch numeric updates and emit an impact list. Resolve damage and update pooled visuals on the main thread. For the existing timed-hit particle path, a simple central impact queue may be sufficient; do not add trajectory simulation without a gameplay need.

For units, start with targeting or steering over position/faction/range snapshots. Keep health, commands, lifecycle coordination, and Unity object access behind the current interfaces. A full unit ECS migration crosses `HealthModel`, `AttackData`, hardpoint views, navigation, and dependency injection, so it should follow a measured pilot. If implemented later, Model owns pure state/rules, Presenter owns coordination/impact application, and View owns rendering/input. The large `LaserTurretView` is a candidate for separating beam state from rendering in that later scope.

## How to compare captures

**Capture code added in this change.** While a battle is running in Editor Play Mode, select **Tools → Performance → Capture Battle (10 Seconds)**. There is no scene/prefab setup. The sampler creates its runtime host only on request, buffers up to 3,000 frames, skips the startup frame, and writes files after capture. It ends after approximately ten real-time seconds of normal play, or earlier at the frame limit. Keep Play Mode running and unpaused during the interval. Starting it again first exports the previous capture; stopping Play Mode or recompiling also exports collected data.

The Console prints the output folder: `Application.persistentDataPath/BattleCaptures`. Each capture produces `battle_<timestamp>.csv` and `battle_<timestamp>_summary.txt`. The summary contains frame-time median/p95/p99/max, per-metric statistics, and Unity/device/quality/resolution/VSync/time-scale metadata. CSV columns include unscaled frame duration, CPU main/render and GPU timings when supported, GC allocations, draw/SetPass/batch/triangle counts, and battle marker durations with invocation counts. Full-frame CPU/GPU zero values are treated as unavailable; a legitimate zero allocation or zero marker invocation remains zero. The report's `fps_at_frame_pacing` values are reciprocals of the listed frame-time percentiles, not separately calculated FPS percentiles.

| Marker | What its invocation count means |
| --- | --- |
| `Battle.Ship.Tick` | Unreleased ship simulation ticks |
| `Battle.Weapon.Tick` | Weapon tick calls, including early exits |
| `Battle.Weapon.TryFire` | Target search and attack-start attempts |
| `Battle.Projectile.GetOrCreate` | Turret acquisition requests |
| `Battle.Projectile.Instantiate` | New turret instances created |
| `Battle.Projectile.TurretUpdate` | Enabled ordinary turret Update callbacks, including idle turrets |
| `Battle.Projectile.LaserUpdate` | Active laser update calls |
| `Battle.Radar.Scan` | Timer-triggered radar scans; multiple fixed steps can occur in one frame |

These counts are not a particle census or an exact count of flying projectiles. Turret callback counts deliberately include idle retained objects to expose ownership/cleanup issues. Captures use bounded memory and no per-frame CSV formatting, file IO, scene searches, or diagnostic logging. The host/buffers remain available for repeated captures during the session; recorders are disposed after capture. Instrumented scopes and the capture coordinator are limited to Editor/Development builds.

Code: [capture coordinator](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Services/Timing/BattlePerformanceCapture.cs), [report writer](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Services/Timing/BattlePerformanceCaptureReport.cs), [menu](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Editor/BattlePerformanceCaptureMenu.cs). A Development Player can call `EmpireAtWar.Services.Timing.BattlePerformanceCapture.StartCapture()` / `StopCapture()` from an existing development integration; no player hotkey or UI was added.

Capture the same camera, resolution, quality, time scale, and approximate fleet composition for three intervals: idle fleets, heavy firing, and after firing stops. Record visible unit counts and weapon mix with the capture. Start once battle load has settled; use a separate capture for initial spawning if spawn spikes are the symptom.

Keep the Editor layout and Game/Scene view visibility consistent. Editor rendering counters can include Editor work; they are not automatically isolated to the Game camera. Avoid changing selection, opening inspectors, or importing assets during the interval.

Compare frame-time median/p95/p99/max against GC bytes, draw/SetPass counts, pool acquisition/creation counts, and named battle samples. A 60 FPS target has a 16.67 ms frame budget. Parent and child profiler samples overlap: do not add weapon-tick and try-fire time together. Try-fire includes target selection and the synchronous start of an attack; use its child samples/self time for attribution. Main/render thread time may include waiting, and GPU timing can arrive late. Missing GPU data is not evidence that the GPU is fast.

For exact call stacks, record the same interval with Unity's CPU Profiler Timeline and save the native capture. Keep Deep Profile off initially because its overhead can change the result. The CSV diagnostic is complementary to a native CPU/GPU timeline. Editor measurements answer the reported symptom; confirm any substantial optimization in a Development Player on the target hardware before committing to an architecture migration. [Unity profiling guidance](https://docs.unity3d.com/6000.4/Documentation/Manual/profiler-profiling-applications.html).

**Validation:** Unity imported the new scripts and metadata; compilation completed with `failed=false` and no compiler errors. The new menu appears in Unity's menu listing. The scene remained MainMenuScene, clean and unchanged; no Play Mode entry or automated test execution was performed. No battle capture was generated in this session, so the diagnostic has compile/static-review validation, not an end-to-end battle recording. Gameplay edits are profiler scopes only. No assets, quality settings, packages, or Obsidian configuration were changed.
