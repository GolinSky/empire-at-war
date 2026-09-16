# Battle attack and projectile optimization plan

- Created: 2026-09-16
- Status: Phase 1 implemented; manual battle and performance validation pending. Later phases not started.
- Scope: Attack logic, busy state, projectile reuse and ownership, target iteration, Jobs + Burst, and a limited material/shader instancing check.

The order is **simplify and correct → measure → pool → measure → centralize scheduling → measure → simplify targeting → measure → apply Jobs + Burst → measure → selectively apply instancing**. Finish and review each phase before beginning the next. This plan does not include quality settings, lighting, shadows, resolution, or general rendering optimization.

## Evidence and assumptions

The current ordinary projectile path already reuses turret objects. It does not instantiate a GameObject for every shot. There are three separate lifetimes: the hardpoint's attack sequence, a leased projectile effect, and a scheduled damage event. Treating these as one busy flag makes the code hard to reason about.

Confirmed in current code:

- `WeaponHardPointView.AttackCoroutine` sets busy, emits a salvo, waits between shots, waits for every retained turret to become idle, then clears busy. There is no explicit hardpoint cancellation/release path.
- `WeaponComponent.Release` stops existing delayed-damage coroutines, but does not coordinate cancellation of the hardpoint firing coroutine or release its projectile views.
- Destroying an individual weapon hardpoint does not stop its in-progress salvo. The delayed-hit validation also checks the target group but not the individual target hardpoint's destroyed state. Correct both cases explicitly rather than preserving them as intended behavior.
- Ordinary `TurretView.ResetParent` detaches the effect. No corresponding owner-release cleanup exists. These objects can survive their ship within the loaded scene; the capture does not prove growth over time.
- `GetTurret` scans the entire retained list on acquisition. Idle retained `TurretView` components continue receiving `Update`.
- Each salvo shot starts a delayed-damage coroutine. Salvos allocate waits and use `WaitWhile` plus an `Any` scan to determine completion.
- Target selection scans the main group first, then additional groups newest to oldest, retaining unit order within each group. `CanAttack` rotates the weapon for in-range candidates before checking local yaw, including candidates that fail the angle check.
- `RocketLauncherHardPointView.Attack` bypasses the base busy-state contract, but the subclass currently has no serialized or construction references. This is a latent correctness issue, not a demonstrated source of battle load.

The existing capture `battle_20260916_083653_628` provides a historical baseline, not a promised performance gain:

| Metric | Observed |
| --- | --- |
| Sample | 589 frames, 10.017 seconds; Unity Editor, Apple M2, target 60 FPS |
| Weapon ticks | 42 callbacks/frame; 0.327 ms average, 0.510 ms p95 |
| Ordinary turret updates | 1,096 callbacks/frame including idle; 0.596 ms average, 0.711 ms p95 |
| Projectile acquisition | 848 requests; zero instances created in the instrumented instantiate branch |
| Total managed allocation | About 48 KB/frame; not yet attributed specifically to attacks |

`Weapon.TryFire` is nested in `Weapon.Tick`; do not add their times. Callback counts are not a count of live particles. The measured weapon cost is small enough that job overhead could outweigh its benefit at this fleet size. We will still build the requested batched Jobs path, compare it with the simplified serial path, and retain the faster execution policy for each workload.

## Behavior contract and intended ownership

Preserve these rules unless a discovered bug is separately documented with its intended correction:

- One round-robin hardpoint attempt per eligible weapon tick; successful firing advances the outer fire cooldown.
- Main-target priority, additional-group order, and first eligible unit selection. `ResetTarget` currently clears only the main target.
- Salvo size and interval, weapon delay, range/arc boundaries, and scaled-time pause behavior.
- One damage event per emitted shot. Ordinary damage is scheduled after returned travel duration; projectile busy time includes its configured delay plus travel duration. Laser damage uses growth duration, while its visual lifetime also includes hold duration. A laser contact raycast currently does not determine damage timing.
- Damage amount currently uses live target distance at impact time. Preserve that calculation at main-thread impact commit; do not substitute launch distance or a stale targeting snapshot.
- Owner release cancels future shots and outstanding damage. Existing in-flight visuals may finish under explicit pool ownership and then return; they must not survive indefinitely or apply damage after cancellation.
- Intended correction: destroying a firing hardpoint cancels its remaining, un-emitted salvo shots. Already emitted shots keep their scheduled impacts while the firing ship remains alive; releasing the whole owner still cancels them.
- Intended correction: if the selected target hardpoint is destroyed before impact, discard that impact. Do not redirect damage to another hardpoint. Apply the same rule in serial and job commits, including a generation check if identities are reused. A cancelled attack cannot become active again through an old callback or job result.
- Busy means a sequence is emitting or waiting for its leased effects to finish. Ready, cooling down, and a projectile being available are distinct concepts. Do not make a hardpoint ready earlier just to increase throughput.

Proposed responsibilities (names may be adjusted to existing conventions):

| Layer | Responsibility |
| --- | --- |
| Model: `AttackSequenceState`, targeting rules, impact records | Pure C# state and rules; no `UnityEngine` references, coroutines, GameObjects, or health-view lookups |
| Presenter/system: scene-scoped `CombatAttackCoordinator` | Own registrations, simulation clock, snapshot construction, execution order, job dependencies, cancellation and main-thread commits |
| Entity adapter: existing `WeaponComponent` | Bridge commands/owner lifecycle into the coordinator and resolve current domain targets through interfaces |
| Views: hardpoint and projectile views | Render aim, particles and beams; report effect completion; contain no target-selection or damage rules |
| Projectile pool | Own all effect instances and leases, including detached active effects; manage available/active collections and teardown |
| Jobs adapter | Convert domain snapshots to persistent native buffers and run Burst numeric selection/progression; keep Unity Collections/Jobs details outside the pure model |

Use one coordinator to batch combat work, with focused state/pool helpers. Do not turn it into a general manager for movement, radar, rendering, or all unit behavior. Reuse existing interfaces and DI where possible. Whole-unit ECS conversion and new projectile trajectory simulation are outside this plan.

## Phase 1 — Simplify attack flow and fix busy/cancellation behavior

### Pre-change timeline (recorded before implementation)

- Ordinary and dual hardpoints began a salvo immediately, emitted one shot per iteration, waited `DelayBetweenShots` after every shot including the last, then waited until all retained turret views reported idle. The hardpoint was busy throughout.
- Each emitted ordinary shot scheduled damage after its returned travel duration; its turret stayed busy for configured projectile delay plus travel duration. Laser damage used growth duration, while its visual also held after growth.
- Owner release stopped pending damage coroutines but did not stop an active hardpoint salvo. Destroying a firing hardpoint did not stop its remaining shots. A target group destroyed before impact was rejected, but a destroyed target hardpoint within a surviving group was not.

- [ ] Record the current target, salvo, busy and impact timeline for ordinary, dual and laser weapons before changing it. Include owner death during a salvo and target death before impact.
- [ ] Express the sequence with explicit states such as Ready, Emitting, WaitingForEffects and Released. Keep cooldown timestamps separate; derive busy from one authoritative sequence state.
- [ ] Separate eligibility, aim, beginning a sequence, emitting one shot, and finishing/cancelling a sequence into readable operations. Keep the existing scheduler temporarily so this phase changes one concern at a time.
- [ ] Reject overlapping starts. Provide a single cancellation/release operation that clears sequence bookkeeping and prevents later shot/damage callbacks. Invoke it from the owner lifecycle before views disappear.
- [ ] Apply the explicit destruction rules above: stop remaining emissions when a firing hardpoint dies, retain already emitted impacts until owner release, and skip delayed hits whose target hardpoint has died. Cover each case separately in manual validation.
- [ ] Define one projectile completion contract for particle and laser effects. Ensure every successful lease completes exactly once and cannot be reused while still active.
- [ ] If the rocket subclass is retained, route it through the same busy/start/cancel contract. Do not add new rocket prefab usage.
- [ ] Add development-only counters/checks for starts, rejected busy starts, emitted shots, scheduled/applied/cancelled impacts, active sequences and unmatched completions. Track invariants through explicit registrations, not scene searches each frame.

**Likely files:** `WeaponComponent`, `WeaponHardPointView`, `BaseTurretView`, `TurretView`, `LaserTurretView`, and the unused rocket override only for its shared contract. Extract the sequence model without unrelated formatting or weapon balance changes.

**Review gate:** No duplicate start, stuck busy state, post-release emission or post-release damage. Busy duration, shot counts and impact times match the behavior contract. Pause/resume, target reset, hardpoint destruction and owner death all terminate or resume correctly. Compile and inspect diagnostics; capture representative firing and compare CPU/allocation cost. Correctness/readability is the purpose of this phase; require no meaningful regression, not an invented FPS target.

## Phase 2 — Make projectile reuse and ownership explicit

- [ ] Replace full retained-list acquisition scans with an available stack/queue and explicit active leases. Preserve per-weapon configuration; a reused instance must not inherit a previous owner's speed, damage-related duration, color or target state.
- [ ] Give every instance an owner in the pool, even when its Transform is detached. Owner release retires idle effects and transfers remaining active effects to a finite drain-to-completion path, or returns them immediately only where visuals permit.
- [ ] Return each effect exactly once after its actual completion contract. Do not use damage time alone as the visual-reuse deadline.
- [ ] Disable idle components/GameObjects appropriately so idle retained effects stop receiving update callbacks. Validate that particle tails, lasers and completion notifications still finish correctly.
- [ ] Prewarm from measured concurrent demand for each effect/configuration key. Grow in controlled increments when required; record high-water marks and expansion counts. Cap retained idle capacity and retire excess after use.
- [ ] Do not drop gameplay shots to satisfy a visual pool cap. Any optional visual overflow policy must be explicit and must preserve damage/sequence behavior.
- [ ] Add pool created/reused/active/available/returned/retired counts, acquisition cost, and ownerless-active count. Teardown must account for every instance.

**Likely files:** Hardpoint/projectile views, their factory/pool seam, owner-release integration, and capture counters. Begin with the smallest pool lifetime that can safely drain effects after owner release; share across owners only with correct reconfiguration.

**Review gate:** Repeated fire → ship death → reinforcement cycles do not accumulate detached objects. After effects finish there are no unowned active views; retained capacity is bounded and intentional. Warmed steady firing creates no new instances within measured demand. Idle update callbacks fall, acquisition time does not grow linearly with retained pool size, and shots/damage remain identical. Measure cold spawning separately from warmed reuse.

## Phase 3 — Replace per-shot coroutines with a dedicated attack scheduler

- [ ] Introduce a scene-scoped coordinator through existing battle DI. Register/unregister weapon presenters explicitly; support ships and stations that use the same weapon path.
- [ ] Move salvo progression and delayed damage into reusable state/impact records with a scaled simulation clock. Eliminate per-shot enumerators, delegates, `WaitForSeconds`, `WaitWhile/Any`, and linear removal of coroutine handles.
- [ ] Use dense active records and a due-time structure suited to the measured workload. Begin with a simple due-time scan or queue; add a heap/time buckets only if evidence justifies it.
- [ ] Model next shot time, shots remaining, pending impacts and effect completion separately. Keep stable owner/target identifiers plus generation/version checks so reused registrations cannot receive old work.
- [ ] Use a stable event sequence for equal due times. Revalidate owner and target before committing damage; cancellation removes/invalidates all associated work according to the owner-versus-hardpoint rules. Compute distance-dependent damage from current positions at impact, matching the existing path.
- [ ] Preserve the existing frame-quantized coroutine behavior deliberately. In particular, decide against silently emitting a catch-up burst after a long frame; document comparisons to the old end-of-frame waits.
- [ ] Keep this implementation serial first so it becomes the reference for Jobs. Remove the replaced path after parity is demonstrated; avoid maintaining two separate combat rule implementations.

**Review gate:** Shots per salvo, target IDs, impact order/timestamps, cooldowns and cancellation agree with the reference scenario, including pauses and long frames. The attack scheduling path allocates no managed memory per shot after warmup, confirmed by attribution rather than only global GC bytes. Queue size drains after combat/release. Compare scheduler CPU time and p95/p99 against Phase 2 before advancing.

## Phase 4 — Simplify enemy and hardpoint iteration

- [ ] Extract deterministic eligibility/selection from `CanAttack` into a side-effect-free calculation. Capture weapon world/parent orientation and relevant positions once; apply the final aim on the main thread.
- [ ] Preserve first-match target priority and the visible final aim after rejected in-range candidates. Verify local yaw wrapping, rotated/scaled parents, boundary angles, zero-distance directions and moving targets before replacing the old calculation.
- [ ] Maintain ordered candidate spans per eligible weapon: main group first, then additional groups newest to oldest, preserving hardpoint order. Share position/alive snapshots across weapons rather than repeatedly reading the same Transform.
- [ ] Remove destroyed/stale registrations while preserving order. Rebuild candidate membership when target commands, group membership or lifecycle changes, not by allocating nested collections on every attempt.
- [ ] Use cheap alive/faction/range rejection before arc math. Use squared distances and equivalent geometry where parity is established. Do not silently replace first-match selection with nearest-target selection or sticky target caching.
- [ ] Track candidate visits, snapshot work, selection time and invalidations. Add spatial pruning or a lower reacquisition frequency only as a measured follow-up if candidate work remains large and behavior stays acceptable.

**Review gate:** The simplified serial selector chooses the same targets and aim outcomes for the reference scenarios. Steady-state candidate preparation/selection does not allocate; repeated Transform reads and dead-target scans fall. Compare the entire preparation + selection + commit cost, not just the innermost loop. This phase supplies the same numeric input/output contract to the Jobs implementation.

## Phase 5 — Apply batched Jobs + Burst to targeting and attack progression

Implement and measure 5A before 5B, with a checkpoint after each.

**5A: numeric target selection**

- [ ] Declare direct dependencies for the APIs used without upgrading unrelated packages. Current lock resolves Burst **1.8.29**, Collections **6.4.0** and Mathematics **1.3.3** transitively; Entities is not required.
- [ ] Reuse native buffers for weapon state, candidate spans, target snapshots and result slots. Grow only at capacity boundaries. Avoid an all-weapons × all-targets intermediate matrix when candidate spans suffice.
- [ ] Schedule one batch across eligible weapons. Each job entry scans its ordered span and returns the chosen target index plus any final aim result needed for parity. Burst handles range/arc math and selection; no managed `AttackData`, `IHardPointModel`, Transform, or Unity object access in worker code.
- [ ] Schedule after input snapshots are final, complete once at the established commit point, and commit in stable registration/sequence order. Explicitly preserve simulation ordering relative to movement/commands; do not introduce one-frame targeting latency as an accidental optimization.
- [ ] Revalidate target/owner generation, alive state, busy state and relevant command version before firing. For invalidated snapshots, use the serial reference path when required to preserve same-frame behavior, and count these fallbacks.

**5B: numeric attack sequence progression**

- [ ] Batch cooldown/salvo/impact due-state advancement over active attack records. Produce bounded per-record due-shot/due-impact requests with stable sequence IDs, not worker-thread callbacks.
- [ ] Main-thread commits acquire/play pooled effects, feed actual returned shot duration back into impact state, calculate live impact-time distance/damage, apply health commands and process completion/cancellation. Preserve one impact per emitted shot and the documented ordinary/laser timing differences.
- [ ] Keep Unity particle APIs, Transform/LineRenderer writes, direct physics calls, health mutation and lifecycle events on the main thread. Laser raycasts remain outside this migration; batch physics only in a later, independently measured change if needed.
- [ ] Complete dependent work before resizing, reusing or disposing buffers and during scene teardown/domain reload. Ensure one writer owns each native slot and results cannot survive an owner-generation change.
- [ ] Compare job execution with the same serial rules at small, representative and stress fleet sizes. Avoid one job per weapon followed immediately by `Complete`. Choose a measured crossover threshold; use the serial path below it.

**Review gate:** No target/shot/damage/cancellation mismatches, job safety errors, native-buffer leaks or lifecycle races. Measure snapshot construction, scheduling, job duration, main-thread wait/`Complete`, commit, allocations and overall combat CPU cost. Accept the Jobs path only where the total cost improves repeatedly. If 5B costs more than the serial scheduler, retain the serial progression and record the result; do not force a slower path merely to use Jobs.

This provides a dedicated attack system without requiring a whole-unit or projectile ECS rewrite. Revisit Entities only if remaining measured storage/lifecycle costs justify a separate proposal.

## Phase 6 — Material/shader checks and selective GPU instancing

### Audit already completed for this plan

Read-only Unity asset inspection covered all **11 prefabs under `Assets/Prefabs/Models/Ships`** (including the reinforcement preview) and **3 projectile prefabs**. It found **49 distinct material asset paths** referenced by existing renderers, with material instancing disabled on each. One path is a model with embedded material data, so this is not a count of all possible material subassets. The dynamically created laser renderer was checked separately through its code and configured material. The current platform supports instancing and SRP Batcher is enabled. These are eligibility findings, not a demonstrated speedup.

| Asset/category | Current setup | Planned action |
| --- | --- | --- |
| Repeated ship hulls/submeshes | Mostly URP Lit; Venator capital and Arquitens use Complex Lit; some ship/lamp materials use Autodesk Interactive | Group repeated mesh + submesh + shared material combinations. Check actual shader variants and SRP batching, then benchmark a representative repeated unit before enabling a broader set |
| Package default `Lit.mat` / embedded Lucrehulk material | Some ship renderers reference package-owned or model-embedded material assets | Do not edit package-cache assets. If an override is justified, use a project-owned material and explicit affected prefab references |
| `Projectile.prefab` / `ProjectileMaterial.mat` | Particle render mode **Stretch**; renderer instancing flag already true; URP Particles/Unlit; material flag false | Current renderer mode is not a mesh-particle instancing candidate. The installed shader has procedural instancing support, but a material toggle alone does not change this limitation |
| `DualProjectile.prefab` / `DualProjectileMaterial.mat` | **Stretch**, renderer flag true; **Legacy Shaders/Particles/Additive (Soft)**; material flag false | Record renderer and shader compatibility blockers. A URP shader or mesh-particle conversion is a separate visual change requiring appearance and performance comparison |
| `LaserProjectile.prefab` / `LazerProjectileMaterial.mat` | `LaserTurretView.Awake` creates a LineRenderer; configured material uses URP Particles/Unlit | Do not treat this as repeated MeshRenderer geometry that a material checkbox can instance |
| Shields / spawn preview | `ShieldView` and `UnitSpawnView` obtain per-renderer material instances for animation/tint | Audit actual sharing and per-instance properties before selecting an instancing approach; do not globally replace them with property blocks without checking the SRP Batcher tradeoff |

### Implementation and acceptance

- [ ] Add an opt-in Editor audit/report listing renderer type, mesh/submesh, shared material identity, shader, material instancing flag, particle render mode and SRP compatibility/blockers. It must report eligibility separately from actual instanced draw evidence.
- [ ] For compatible repeated unit geometry, compare the existing SRP path with a scoped instancing candidate. Enable instancing only for verified candidate materials and preserve appearance. Do not globally disable SRP Batcher or bulk-toggle every material.
- [ ] Verify actual instanced draws with Frame Debugger and measure CPU submission/frame cost in the same scenario. A checked material flag or lower draw count alone is insufficient acceptance evidence.
- [ ] For current stretched particles and lasers, mark the simple checkbox optimization inapplicable. If a mesh-particle/shader experiment is justified, keep it isolated, preserve stretching/color/softness/beam appearance, and keep it only after a measured benefit. Do not turn this phase into a VFX/rendering rewrite.
- [ ] Import/save only changed assets through official Unity tooling and check for serialization, shader and import errors. Retain a precise list of affected materials/prefabs for rollback.

**Review gate:** Compatibility report has no unsupported assumptions; instancing is visibly active where claimed; appearance is unchanged; representative repeated measurements show a benefit. Otherwise retain the current material path and mark the candidate “no demonstrated benefit” or “incompatible with current renderer.” This phase can legitimately finish without asset changes.

Unity references: [GPU instancing and pipeline restrictions](https://docs.unity3d.com/6000.4/Documentation/Manual/GPUInstancing.html), [SRP Batcher compatibility and scoped alternatives](https://docs.unity3d.com/Manual/SRPBatcher-Incompatible.html), [particle instancing shader requirements](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/ParticleSystemRenderer-enableGPUInstancing.html), [particle mesh-render-mode requirement](https://docs.unity3d.com/Manual/PartSysInstancing.html). Installed URP 17.4.0 shader source was also inspected; the Built-in Pipeline particle examples must not be copied into URP unchanged.

## Checkpoint procedure after every phase

1. Review the scoped diff and behavior contract. Use one implementation writer; use an independent reviewer where useful. Keep each phase independently revertible without undoing earlier accepted phases or unrelated user changes.
2. Compile, inspect relevant console/import diagnostics, and perform the manual battle/lifecycle scenarios. Do not run automated tests unless the user explicitly requests them. Do not discard dirty scenes to validate a change.
3. Capture matched small, representative and stress battles, keeping fleet composition, shot mix, camera, Editor layout and time scale consistent. Repeat enough captures to distinguish changes from run variation. Include firing, cold pool growth, warmed firing and post-death cleanup. Editor Play Mode is the primary reported environment; confirm larger performance claims in a Development Player later.
4. Compare CPU mean/p95/p99, allocation attribution, candidate visits, shots/impacts, active/idle/orphan effect counts and scheduler/job costs. Use native CPU Timeline for coroutine or wait attribution when CSV totals cannot answer the question. Keep Deep Profile off initially. A frame-rate cap can hide CPU headroom gains; FPS alone is not the acceptance criterion.
5. Write the result below: what changed, captures used, before/after metrics, behavior checks, remaining risks, and **accept / revise / revert**. Begin the next phase only after the current gate is resolved. If user battle input/capture is needed, leave that phase explicitly awaiting validation rather than marking it complete.

The existing capture command uses a ten-second window; duration itself is not the acceptance criterion. Samples must cover the relevant battle/lifecycle event. Add only the diagnostic counters required by the current phase, using bounded buffers and no per-frame logs or scene searches.

| Phase | Status | Evidence and decision |
| --- | --- | --- |
| 1. Readable attack states and busy/cancellation | Implemented; awaiting manual validation | Explicit sequence state and effect leases, owner/hardpoint cancellation, target-hardpoint impact rejection, rocket shared path, and development counters. Unity recompile completed without errors; one filtered EditMode health-model smoke test passed; independent diff review found no material defect. No battle or performance capture was run, so firing/pause/lifecycle parity and CPU/allocation impact remain unverified. |
| 2. Projectile ownership and reuse | Not started | — |
| 3. Serial attack/impact scheduler | Not started | — |
| 4. Target iteration and numeric rules | Not started | — |
| 5A. Jobs + Burst targeting | Not started | — |
| 5B. Jobs + Burst sequence progression | Not started | — |
| 6. Instancing | Initial audit complete; experiments not started | No asset changes or speedup claimed |

## Source map and execution constraints

Primary existing code: `Assets/Scripts/Components/Weapon/WeaponComponent.cs`; `Components/ViewComponents/Health/WeaponHardPointView.cs`; `Components/ViewComponents/Weapon/{BaseTurretView,TurretView,LaserTurretView}.cs`; `Components/AttackComponent/AttackData.cs`; `Components/Health/{HealthComponent,HardPointAdapter}.cs`; ship/station combat presenters and battle installers. Paths after the first are relative to `Assets/Scripts`.

Diagnostic baseline: `Docs/BATTLE_PERFORMANCE_REVIEW.md`, `Assets/Scripts/Services/Timing/BattlePerformanceCapture*.cs`, `BattleProfilerMarkers.cs`, and the capture files under the application's `BattleCaptures` directory. The earlier review's rendering observations are historical and are not part of this implementation scope.

Use Serena for live C# symbols and the official `unity` CLI for Unity operations. Before creating new asset folders, read [[Architecture/PROJECT_ORGANIZATION|PROJECT_ORGANIZATION]] through the configured vault tooling as required by repository policy. Preserve metadata and serialized field names. Do not use Graphify, run unrequested tests, or modify Obsidian configuration. Jobs guidance: [scheduling and completion](https://docs.unity3d.com/Manual/job-system-creating-jobs.html); use current package documentation for the installed versions during implementation.

Planning contributors: `battle_code_review` / code_explorer (configured gpt-5.6-luna, medium reasoning) confirmed lifecycle/busy/target behavior; `attack_jobs_plan` / unity_architect (configured gpt-5.6-sol, high reasoning) defined batching and migration boundaries and reviewed the completed plan. Its destruction and impact-distance findings are incorporated above. Effective models were not exposed by the runner. Parent audited materials and synthesized this plan. No gameplay, material or prefab changes were made while creating it.
