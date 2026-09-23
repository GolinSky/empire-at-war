# Unity Jobs implementation audit and fix plan

Audit date: 2026-09-17. Analysis and planning only.

## Executive decision

- **Scope:** all discovered first-party Jobs usage: combat target selection and attack due-state evaluation, including their callers, native buffers, result consumers, and teardown. Vendor/generated systems were excluded from the broad audit; installed dependency source was read where relevant.
- **Revision:** `61a8e8bb3fcdf5b1f6d79880fc4a247008463438`, branch `main`. The working tree already contained material, prefab, rendering/settings, editor-audit, and documentation changes. No existing changes were altered. The audited combat source had no working-tree diff.
- **Overall recommendation: keep with targeted fixes.** Keep the feature-level coordinator and explicit buffer ownership. Fix targeting equivalence before tuning scheduling. The value of the due-state job remains **insufficient evidence**.
- **Correctness:** no missing completion edge, conflicting native writes, or normal-path use-after-free was found in the inspected code. Two targeting problems need attention: threshold-dependent snapshot timing and a confirmed degenerate-direction rotation mismatch. Inspection is not a runtime safety certification.
- **Approach:** batched target scans are a plausible Jobs workload; a job performing two comparisons per attack record needs stronger justification. Keep existing gameplay/DI boundaries and same-frame results.
- **Performance:** existing battle measurements are real, but there is no matched serial-versus-Jobs comparison and the CSV recorder omits both job pipelines. No speedup or slowdown is established.
- **Production changes during this audit:** none. This report is the only repository file added. No automated tests, builds, Play Mode transitions, package changes, or asset changes were performed.

## Implementation follow-up — 2026-09-17

The audit above records the original inspection at `61a8e8bb`. The following work was performed afterward on the same branch:

| Plan item | Follow-up result |
|---|---|
| 1. Stable target snapshot | Weapon Tick now queues intent. The coordinator captures every queued weapon and candidate at one LateTick boundary before choosing serial or Jobs evaluation. Request order, same-frame commit, version/generation checks, and the live stale-result fallback remain in place. |
| 2. Numerical parity | The target job requests an explicit serial reevaluation for vertical/zero/near-vertical geometry, tilted parent rotation, nonfinite numeric inputs, and close range/yaw boundaries. The serial path and fallback evaluate the same LateTick snapshot through `WeaponTargetSelector`. |
| 3. Trustworthy capture | The CSV now includes whole target and due phases, serial/Jobs branches, capture/prepare, schedule, completion, and apply scopes; per-frame workload, fallback, native capacity, growth, and pending counts; and source/variant settings in metadata. `completed_unity_frame` identifies the combat frame sampled in each row. Schedule/completion time is main-thread dispatch/wait, not worker execution. |
| 4. Due-job value | A synthetic EditMode benchmark compared the existing serial search, direct serial collection with the same sort/commit path, and the Jobs path. The instrumented direct-serial path measured lower time than the instrumented Jobs path at all measured record counts 1–256 and due densities. The production due strategy remains unchanged until normal/peak gameplay workload and target hardware are known. |
| 5. Target batching | No threshold or batch-size tuning was made; no complete-path battle comparison exists yet. |

The benchmark ran three repetitions of 40 measured invocations after five warm-ups per variant and workload. Prepared impacts and their setup were excluded from timing; each variant included its own collection and unchanged commit path. Representative median-of-run medians: at 64 records with six due, the instrumented direct-serial path took 1.1 µs versus the instrumented Jobs path at 6.0 µs; with all 64 due, 11.0 µs versus 16.6 µs. At 256 records with 25 due, it took 5.1 µs versus 20.4 µs; with all 256 due, 100.5 µs versus 125.6 µs. Raw results are in [UNITY_JOBS_DUE_MICROBENCHMARK.csv](UNITY_JOBS_DUE_MICROBENCHMARK.csv). The Jobs path has additional nested profiler scopes, so this comparison does not isolate algorithm cost. These are synthetic Editor measurements on the current Mac, not a matched battle capture or Player result.

Focused EditMode validation passed: 16 scheduled target-job cases, seven scheduled due-job cases, four coordinator target queue/capture cases, four coordinator due ordering/cancellation cases, and two capture report/export cases. The explicit synthetic benchmark test also passed. The earlier unfiltered test invocation timed out and was cancelled; it has no pass result. All open scenes were clean before each targeted run. Full battle movement, firing/impact frame, Burst Player, and frame-time equivalence gates remain open because no reproducible battle fixture or release target was supplied. Unrelated material, prefab, scene, settings, and Obsidian working-tree changes were not used as evidence.

## Reference pack applied

All three Markdown files in the supplied Drive folder were read in full. They are review guidance, not evidence that this project has passed their gates:

1. [01 — Unity threading and synchronization rules](https://drive.google.com/file/d/1Gsq8aLWwylJywv8zl48boRa7aMEuYBP7/view)
2. [02 — Unity Jobs audit playbook](https://drive.google.com/file/d/1U3prZcJH9i888TIx0ZPZh293adn8C7rH/view)
3. [03 — Agent prompts and report template](https://drive.google.com/file/d/1uXlmpuInp1Tua6PxbGzZIUbr9I7KSbpu/view)

The pack's generic suggestion to execute suitable tests does not override the repository's explicit prohibition on automated test execution without a user request. This pass inspected source and existing artifacts and provides a future validation matrix.

## Environment and behavior contract

| Item | Observed value / requirement | Evidence |
|---|---|---|
| Editor | Unity 6000.4.7f1 | [ProjectVersion.txt](/Users/golinsky/Projects/empire-at-war/ProjectSettings/ProjectVersion.txt:1) |
| Native packages | Burst 1.8.29; Collections 6.4.0; Mathematics 1.3.3; no declared Entities dependency | [manifest](/Users/golinsky/Projects/empire-at-war/Packages/manifest.json), [lock](/Users/golinsky/Projects/empire-at-war/Packages/packages-lock.json) |
| Test packages | Test Framework 1.6.0; Performance Test Framework 3.4.0 | Package lock; availability is not execution |
| DI / async | Extenject 9.2.0, Zenject namespaces. These two pipelines use Jobs and synchronous main-thread commits, not managed background tasks | [Extenject package](/Users/golinsky/Projects/empire-at-war/Assets/Plugins/Zenject/package.json), combat source |
| Current target / backend | Read-only Editor query: StandaloneOSX, Mono2x; current build flags non-development/non-profiler; macOS support installed | Official Unity CLI inspection by performance agent |
| Existing measured hardware | Apple M2, Unity Editor, High Fidelity | Existing capture metadata; minimum supported hardware and release targets remain unspecified |
| Target-selection rate | At most one round-robin hardpoint attempt per eligible weapon Tick; successful commit advances outer cooldown | [WeaponComponent.Tick](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/Weapon/WeaponComponent.cs:155) |
| Target ordering | Main group first, additional groups newest to oldest, hardpoint order preserved; choose first eligible candidate, not nearest | [RebuildCandidates](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/Weapon/WeaponComponent.cs:322), [job scan](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/Weapon/WeaponTargetSelectionJob.cs:41) |
| Aim contract | Range and yaw endpoints are inclusive; preserve final aim from the last in-range candidate when none passes the arc | Serial selector and TryFireWeapon; edge equivalence is not yet established |
| Target result deadline | Same frame, coordinator LateTick; no in-flight work persists to the next frame. Serial and Jobs currently use different capture moments | JOB-AUDIT-001 |
| Attack timing | Scaled Time.time; EarliestFrame prevents same-frame newly scheduled impacts/next shots; stable ordering by due time then event sequence; no catch-up salvo burst | [coordinator](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/Weapon/CombatAttackCoordinator.cs:114) |
| Result identity | Request owner generation, target-command version, target generation/alive state; impacts also validate target ID/membership | [selection commit](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/Weapon/WeaponComponent.cs:275), [impact commit](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/Weapon/CombatAttackCoordinator.cs:438) |
| Lifetime | Scene singleton coordinator; weapons register/unregister; release cancels their future shots and impacts; native allocations end on coordinator disposal | [installer](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Services/SceneContext/Skirmish/SkirmishServiceInstaller.cs:24), [Release](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/Weapon/WeaponComponent.cs:77) |
| Workload / goal | Actual queued-request, candidate, active-record, and due-event distributions are missing. Existing captures target 60 FPS, but this is not a user-approved Jobs budget | Capture limitations below |

The existing [battle optimization plan](/Users/golinsky/Projects/empire-at-war/EmpireAtWarDocumentation/TODOs/Battle_Attack_Optimization_Plan.md:36) provides advisory behavior requirements. Its Phase 4/5 parity and performance gates remain unchecked. In particular, damage uses live distance at impact time; firing-hardpoint destruction cancels remaining shots, while whole-owner release also cancels already scheduled damage. This audit does not authorize changing those rules.

## Per-feature verdicts

| Feature | Correctness | Approach verdict | Performance evidence | Next action |
|---|---|---|---|---|
| Weapon target selection | Native access looks correctly ordered; snapshot timing differs by threshold and vertical aim differs by execution path | **Keep with targeted fixes** | No matched complete-path comparison | Resolve JOB-AUDIT-001/002, then measure candidate-dependent crossover |
| Attack due-state evaluation | Independent result slots, completion before consume, stable event identities; no confirmed defect for finite valid timing inputs | **Insufficient evidence** for keeping the extra job path | No isolated due-pipeline timings | Compare serial collection/sort against current job under equivalent commit order |
| Registration / native lifetime | Normal release and disposal paths account for owned storage and reject stale registrations | **Keep** | Not a performance conclusion | Lifecycle regression coverage when test execution is requested |

## Implementation map

### Job inventory

| Job | Scheduler / rate | Inputs → outputs | Main-thread consumer |
|---|---|---|---|
| [WeaponTargetSelectionJob](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/Weapon/WeaponTargetSelectionJob.cs:8), Burst IJobParallelFor | [ProcessTargetSelections](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/Weapon/CombatAttackCoordinator.cs:246), once per nonempty qualifying LateTick; **R ≥ 8**, batch size **1** | R weapon inputs and C packed candidate positions → R result slots: first candidate index, visit count, selected/last in-range aim | Complete at line 299; ordered apply at 302; WeaponComponent validates and fires |
| [AttackDueJob](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/Weapon/AttackDueJob.cs:7), Burst IJobParallelFor | [ProcessDueEventsBatched](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/Weapon/CombatAttackCoordinator.cs:348), once per qualifying LateTick; **N = sequences + impacts ≥ 64**, batch size **32** | N due-time/frame pairs and captured now/frame → N byte flags | Complete at line 378; build/sort due identities; lookup and commit on main thread |

Both kernels use only numeric data and NativeArrays; they do not access presenters, Transform, physics, resolver services, mutable static state, managed collections, or worker callbacks. No first-party safety-disabling attributes, native slices, parallel append writers, deferred disposal, or cross-frame JobHandles were found in this scope. Discovery hits for unrelated methods named Complete or Block were excluded.

### Allocation and ownership inventory

All five allocations are private fields of CombatAttackCoordinator. Capacity starts at 8 and doubles until sufficient; storage is retained at its high-water mark. R, C, and N below are active counts, not allocation lengths.

| Storage ID | Field / allocator | Aliases and access | Terminal use | Reuse / disposal |
|---|---|---|---|---|
| T-in | _targetSelectionInputs / Persistent | Main thread writes [0,R); scheduled job's field copy reads the same allocation | Target Schedule(...).Complete() | Before subsequent gather; growth in EnsureTargetSelectionBuffer; scene Dispose |
| T-pos | _targetSelectionPositions / Persistent | Main writes [0,C); target iterations read frozen candidate spans | Same target completion | Same |
| T-out | _targetSelectionResults / Persistent | Each iteration i writes only slot i; main reads [0,R) after completion | Same target completion | Same |
| D-in | _dueInputs / Persistent | Main writes sequence prefix then impact suffix; due job reads [0,N) | Due Schedule(...).Complete() | Before subsequent due gather; growth; scene Dispose |
| D-out | _dueResults / Persistent | Each iteration i writes one byte i; main reads [0,N) after completion | Same due completion | Same |

References: [fields](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/Weapon/CombatAttackCoordinator.cs:79), [capacity growth](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/Weapon/CombatAttackCoordinator.cs:332), [Dispose](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/Weapon/CombatAttackCoordinator.cs:227). Distinct fields own distinct allocations; dispatch copies alias their corresponding owner's storage, not another input/output allocation. Unused capacity is not consumed. Every scheduled output slot is assigned, including no-match results.

Persistent reuse is appropriate to recurring work; it is not itself a leak. Peak capacity and allocation-failure behavior have not been stress-tested. There is no evidence supporting an arbitrary new capacity cap or extra synchronization framework.

### Execution / dependency graph

```text
Frame n: main-thread weapon Tick
  capture ordered candidate identities/positions + origin/rotation/version
  append managed request
       |
       | remaining Update work may change transforms (freshness concern)
       v
Main-thread coordinator LateTick (configured order -1000)
  if R < 8: live serial targeting -> commit
  else:
    grow/gather T-in, T-pos
      -- RAW: gather writes must precede job reads --> TargetSelectionJob
      -- job writes T-out --> Complete (terminal handle for all iterations)
      -- RAW: completion before output reads --> validate -> ordered commits
  clear request/candidate lists
       |
       | gameplay phase ordering; not shared native memory
       v
  capture now/frame, inspect sequence+impact records
  if N < 64: repeatedly find earliest due event -> commit
  else:
    grow/gather D-in --> AttackDueJob --> Complete
    D-out flags -> due event identities -> sort(time, sequence)
    lookup surviving event -> commit (may cancel other events)
       |
       v
Return from LateTick: no pending native users
Frame n+1: reuse buffers only after prior frame's completed access
```

There are no sibling native readers left to join and no incoming prior-frame handles. Complete covers every iteration in its batch. Main-thread program order plus completion supplies WAR/WAW protection before later overwrites, resize, or disposal. Target and due jobs use disjoint buffers, but this does not justify reordering their gameplay phases or moving commits to another frame.

### Resource-lifetime graph

```text
Scene DI creates coordinator (no native allocation yet)
  -> owner registration + monotonically assigned owner generation
  -> threshold reached -> allocate Persistent buffers
  -> main-thread gather -> job owns permitted access -> Complete
  -> main-thread read/apply -> retain storage
  -> repeat; completed storage may grow by dispose + replacement

Weapon release:
  unregister -> remove its sequences/impacts -> invalidate stale queued requests
  -> release hardpoint sequence/effect ownership

Scene coordinator Dispose:
  clear registrations/managed queues -> dispose each created native field
  (normal call path has no outstanding job, because scheduling is synchronous)
```

No general cancellation mechanism is needed to interrupt these short-lived jobs; they finish in the scheduling invocation. If completion is delayed in a future implementation, the owner must retain handles and establish a new shutdown protocol before this disposal code remains valid.

## Findings

### JOB-AUDIT-001 — Target snapshot timing changes at the eight-request threshold

- **Category / severity / confidence:** behavioral correctness; **Medium**; high confidence in the two different code paths, medium confidence in live-battle impact because update ordering was not measured.
- **Evidence:** observed in source; **not reproduced**.
- **Locations:** [WeaponComponent.QueueTargetSelection](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/Weapon/WeaponComponent.cs:185), [serial live reads](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/Weapon/WeaponComponent.cs:226), [threshold branch and snapshot gather](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/Weapon/CombatAttackCoordinator.cs:250), [commit validation](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/Weapon/WeaponComponent.cs:275).
- **Rules:** MT-03/04, JOB-05/10; playbook A1/D3.
- **Invariant:** selecting an execution strategy must not silently change the observation time used for target eligibility and aim.
- **Mechanism:** Tick captures candidate positions, weapon origin, and parent rotation. For fewer than eight requests, LateTick ignores those positions and recomputes from live transforms. At eight or more, it uses the earlier snapshot. Commit checks membership/version/generation, but movement alone does not invalidate any of these.
- **Ordering evidence:** Zenject dispatches regular ticks from [MonoKernel.Update](/Users/golinsky/Projects/empire-at-war/Assets/Plugins/Zenject/Source/Runtime/Kernels/MonoKernel.cs:59) and late ticks from [MonoKernel.LateUpdate](/Users/golinsky/Projects/empire-at-war/Assets/Plugins/Zenject/Source/Runtime/Kernels/MonoKernel.cs:91). Ship movement uses DOMove and rotation/route tweens in [ShipMovementTweenPlayer](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/Ship/Movement/ShipMovementTweenPlayer.cs:134). Those tweens use the default update mode; DOTween's implementation is compiled and its ordering relative to MonoKernel.Update was not established. This supports a concrete validation case, not a claim that a mismatch was observed in play. Station weapons use the same queued WeaponComponent path.
- **Trigger / impact:** if shooter or target moves or rotates between capture and LateTick, a range/arc boundary can be crossed. Adding unrelated weapon requests can then change whether the same weapon fires, which first candidate it selects, or its applied aim. A job can also report no candidate when one has since entered range. No runtime mismatch frequency was measured.
- **Minimal fix plan:** establish one main-thread capture phase for both paths. Recommended starting point is the existing LateTick selection phase: queue intent/identity during Tick, gather current numeric state once immediately before dispatch/serial evaluation, and have both branches consume that same snapshot. Preserve ordering and same-frame commit; stale membership must still trigger safe refresh/fallback. Do not introduce a cross-frame cache. Validate this against the intended pre-Jobs combat timing before accepting the change.
- **Behavior implication:** this deliberately removes the current threshold-dependent freshness difference; it requires agreement on the canonical capture boundary, not a promise of bitwise equivalence to both inconsistent branches.
- **Validation:** controlled movement between request and selection; 7/8/9 requests with identical subject weapon and dummy unrelated requests; entering/leaving range, arc crossing, rotating shooter, no-candidate output, version invalidation. Compare target ID, aim, shot count, and commit frame.
- **Remaining uncertainty:** precise movement/weapon update order and intended snapshot age; no deterministic runtime case executed.

### JOB-AUDIT-002 — The job returns identity aim for vertical targets where the serial path points at them

- **Category / severity / confidence:** numerical/behavioral correctness; **Medium / high**. The differing function contracts and installed Mathematics implementation establish the mechanism; live occurrence is unmeasured.
- **Evidence:** observed source plus version-matched API documentation; **not reproduced**.
- **Locations:** [job aim/yaw](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/Weapon/WeaponTargetSelectionJob.cs:47), [serial aim/yaw](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/Weapon/WeaponTargetSelector.cs:19), [installed LookRotationSafe](/Users/golinsky/Projects/empire-at-war/Library/PackageCache/com.unity.mathematics@19a9377c4ffa/Unity.Mathematics/quaternion.cs:405).
- **Rule:** JOB-10.
- **Invariant:** equivalent numeric inputs must produce equivalent target/aim outcomes regardless of request count.
- **Mechanism:** Unity Quaternion.LookRotation handles a nonzero direction collinear with world up by rotating +Z toward that direction. Mathematics quaternion.LookRotationSafe returns identity for collinear inputs. The job publishes that identity as selected aim or last in-range aim. Its yaw also uses atan2 of local forward, while the serial path decomposes the relative quaternion; singularities and angle wrapping therefore need explicit parity coverage. This report does not assert a general mismatch for ordinary nonsingular directions.
- **Concrete analytical case:** origin (0,0,0), candidate (0,10,0), identity parent, range >10, yaw interval containing zero. The two rotation APIs have different specified outputs: serial points upward, job returns identity. Even a rejected candidate can leave a different final aim through LastInRangeAim. This is a reasoning example, not an executed test.
- **Minimal fix plan:** preserve the existing serial selector as the behavior reference. Add an explicit per-request fallback result for unsupported/degenerate direction or yaw cases, and rerun that request through the serial calculation before applying any job result. A later shared numeric implementation is appropriate only after its boundaries and singularities are validated. Do not silently replace serial behavior with identity to make parity appear to pass.
- **Behavior implication:** ordinary first-match order and same-frame firing stay intact; exceptional requests use the established calculation. Define zero-distance and nonfinite-input behavior separately rather than masking them.
- **Validation:** above/below, collinear and near-collinear, zero distance, rotated parents, near ±180° yaw, min/max arc endpoints, exact range endpoint, and no-match final aim. Use angular quaternion comparison, not raw quaternion component equality.
- **Remaining uncertainty:** prevalence in actual battle geometry and exact acceptable angular tolerance.

The API difference is documented by [Unity 6.4 Quaternion.LookRotation](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Quaternion.LookRotation.html) and [Mathematics 1.3 LookRotationSafe](https://docs.unity3d.com/Packages/com.unity.mathematics@1.3/api/Unity.Mathematics.quaternion.LookRotationSafe.html), and the latter was checked against installed source.

### JOB-AUDIT-003 — Capture output omits the job pipelines needed to justify their thresholds

- **Category / severity / confidence:** validation/instrumentation; **Medium / high**. This is an evidence gap, not a demonstrated runtime slowdown.
- **Evidence:** observed in source and existing CSV headers.
- **Locations:** [job markers](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Services/Timing/BattleProfilerMarkers.cs:10), [capture metric list](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Services/Timing/BattlePerformanceCapture.cs:35), [recorder setup](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Services/Timing/BattlePerformanceCapture.cs:226), [thresholds](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/Weapon/CombatAttackCoordinator.cs:16).
- **Rules:** PERF-01/02/03.
- **Invariant:** a crossover threshold or speedup claim must use complete, equivalent feature-path measurements.
- **Mechanism:** TargetBatch, TargetBatch.Job, DueBatch, and DueBatch.Job exist, but the capture records older Ship/Weapon/TryFire/projectile/radar markers. TryFire only covers serial target scans after jobification. No inspected capture contains the new job metrics. The Job scopes also combine Schedule and Complete, so their duration cannot be called pure worker time or pure waiting. Selection Stopwatch counters have no discovered reporting consumer and omit early preparation costs.
- **Impact:** falling TryFire time cannot demonstrate that targeting got cheaper. Thresholds 8 and 64 and current batch sizes have no retained matched evidence establishing where they win.
- **Minimal fix plan:** extend the existing capture, not a new profiler framework. Record full target/due phase markers and nested gather/prepare, schedule, completion, apply/cleanup; record request/candidate/record/due counts, branch invocations, fallback counts, and capacity growth. Keep Tick-side capture work attributable until JOB-AUDIT-001 changes its location. Store revision and variant settings in capture metadata. Use CPU Timeline for actual worker execution.
- **Validation:** confirm exported headers and nonzero invocation counts for deliberately exercised branches; reconcile workload counters to the scenario; repeat matched runs. Measure instrumentation overhead separately.
- **Rollback:** remove or reduce instrumentation if it materially perturbs the workload; do not choose a Jobs threshold from incomplete captures.

## Approach observations and alternatives

These are candidates for experiments, not additional proven performance defects.

| Candidate | Expected benefit / question | Cost or semantic risk | Decision |
|---|---|---|---|
| Current immediate Schedule + Complete | Parallel/Burst execution may help sufficiently large scans | No useful main-thread overlap in these methods; gathering and scheduling may dominate | Keep as baseline. Immediate Complete is **not** a correctness bug |
| Serial target selection over the same snapshot | Establish actual parallel break-even without freshness differences | Must preserve ordering, aim, and exceptional inputs | Required comparison after parity fixes |
| Synchronous job Run path / single Burst-compatible job | Distinguish compilation/layout benefit from parallel dispatch | Do not assume the chosen Run API actually executes Burst code | Small secondary experiment only with compilation evidence |
| Serial due-flag collection + the same sort/commit | Remove dispatch and native marshalling for a two-comparison kernel | Must preserve stable event ordering and callback-driven cancellation | Most useful first alternative for due-state evaluation |
| Improve due-event lookup | Current sorted D due events each scan up to N live records via TryFindEvent: worst-case O(D×N), in addition to sort | Serial baseline also scans; direct saved indices become stale after swap-removal/callback cancellation | Only if apply/lookup is measured dominant; then consider a narrow event-ID index that is updated on swaps |
| Share candidate positions / change batching | Reduce repeated preparation or load imbalance | Must not share stale snapshots or change first-match priority | Measure candidate distributions before choosing |
| Delay Complete / double buffer | Potential overlap | Needs proven independent work; cross-frame results change timing and require new identity/lifetime contracts | No recommendation to implement now |
| ECS, global scheduler, managed Task pool, global worker-count changes | No demonstrated need in this feature | Broad architecture/platform costs | Out of scope |

The coordinator is about 500 lines and WeaponComponent about 410. Their ownership is still traceable, but both exceed the project's 200-line review threshold. A focused separation of targeting calculation from orchestration may become useful while fixing parity. Do not split unrelated combat systems or add a generic job abstraction merely to shorten files.

## Tests and inspection status

**Automated test commands executed: none.** No C# compilation or new runtime reproduction was performed. Existing unrelated tests and historical pass statements are not evidence that these Jobs paths pass. Targeted source searches found no direct tests for WeaponTargetSelectionJob, AttackDueJob, CombatAttackCoordinator, or WeaponTargetSelector in Assets/Scripts/Tests/Editor.

| Proposed validation | Inputs / expected result | Actual status |
|---|---|---|
| Target serial/job parity | 0/1 candidates; request counts 0/1/7/8/9; first accepted candidate, duplicates and last rejected in-range aim preserved | Not run |
| Numeric boundaries | Exact range/arc endpoints, ±180°, zero/vertical/near-vertical directions, rotated parents; define tolerances and valid input domain | Not run |
| Snapshot freshness | Move shooter/target between Tick and LateTick; strategy threshold does not change observation time | Not run |
| Due flag/order parity | 0/1/31/32/33/63/64/65 and nonmultiples; equal times sorted by stable sequence; all/none/few due | Not run |
| Timing semantics | Pause/resume, scaled time, long frame, delay zero; no new same-frame impact and no catch-up salvo burst | Not run |
| Mutating commits | A due impact releases an owner/removes another due event; remaining event identities still commit correctly once | Not run |
| Identity/lifecycle | Target destroyed/replaced, command version changed, owner released with queued requests/impacts, firing hardpoint destroyed | Not run |
| Native reuse | Cross capacity boundaries repeatedly; complete before resize/dispose; scene exit and reload modes; no leaked native allocation | Inspected normal path only; not run |
| Platform/compilation | Actual scheduled parallel path with safety checks; verify Burst compilation; equivalent supported Player behavior | Not run |

Future test execution requires an explicit user request. Before Unity tests, inspect registered command schemas and list open scenes; proceed only under the project's clean-scene rules. Do not save an untitled/dirty scene or change scenes to unblock testing. Use the installed framework and async test/status workflow when authorized. No test package installation is needed.

## Performance evidence and benchmark plan

Six existing CSV/summary pairs were located under `/Users/golinsky/Library/Application Support/DefaultCompany/EmpireAtWar/BattleCaptures/`: two dated September 16 and four dated September 17. Jobs were introduced in commit `1fc2f60f` on September 16 at 16:10; the September 16 captures precede that change. Later captures lack source-revision and branch-execution metadata, so their dates alone do not prove which job branch ran.

### Measurements actually available

Newest existing capture: [battle_20260917_104702_273_summary.txt](</Users/golinsky/Library/Application Support/DefaultCompany/EmpireAtWar/BattleCaptures/battle_20260917_104702_273_summary.txt>). Conditions: 458 frames over 10.006 seconds; Unity 6000.4.7f1 Editor, Apple M2, High Fidelity, 2940×1506, time scale 1, VSync 0, target 60 FPS.

| Metric | Average | p95 | p99 |
|---|---:|---:|---:|
| Frame duration | 21.786 ms | 40.501 ms | 52.803 ms |
| CPU main thread | 8.138 ms | 16.053 ms | 17.361 ms |
| GPU frame, 359/458 samples available | 28.711 ms | 41.918 ms | 45.654 ms |
| Managed allocation | 20,029 B/frame | 38,261 B/frame | 56,359 B/frame |
| Weapon Tick | 0.218 ms | 0.363 ms | 0.413 ms |

This capture has 16,030 Weapon Tick calls and only 35 TryFire calls. That imbalance is not a measured count of Jobs requests. Frame/GPU costs were substantial in this run, but neither their causality nor Jobs impact follows from the available markers. GPU and CPU durations are not additive frame-time components.

The older [battle_20260916_083653_628_summary.txt](</Users/golinsky/Library/Application Support/DefaultCompany/EmpireAtWar/BattleCaptures/battle_20260916_083653_628_summary.txt>) has 589 frames/10.017 seconds, frame p95 21.585 ms, main-thread p95 13.056 ms and GPU p95 22.099 ms. Fleet/pool state, code, and resolution differ. These two runs are **not** a controlled before/after comparison.

| Variant | Gather/prepare | Schedule | Worker execution | Completion | Apply/cleanup | End-to-end / frame delta | Native capacity/backlog |
|---|---|---|---|---|---|---|---|
| Current target pipeline | Not measured separately | Not measured | Not measured | Not measured | Not measured separately | No matched result | Not captured |
| Current due pipeline | Not measured separately | Not measured | Not measured | Not measured | Not measured separately | No matched result | Not captured |
| Equivalent serial alternatives | Not run | N/A | N/A | N/A | Not run | Not measured | Not measured |

### Small decisive experiment

1. First resolve targeting parity and capture gaps. Record an explicit build/revision, execution variant, seed or retained scenario, fleets/weapon mix, camera, resolution, quality, time scale, VSync, frame cap, Burst/safety settings, and warm-up interval.
2. Exercise target-request counts 1/7/8/9 plus observed typical/peak values, with bins for candidate counts and early versus late/no matches. Compare forced serial and forced Jobs on identical snapshots. Try target batch sizes 1/8/32 only after the baseline comparison.
3. Exercise active due-record counts 1/31/32/33/63/64/65 plus observed peak, with 0%, sparse, and 100% due. Compare current Jobs collection with a direct serial collector feeding the same sort/commit logic. Try due batch sizes 16/32/64 if parallel execution merits further tuning.
4. Collect at least three warmed steady-firing runs of roughly ten seconds per decisive variant; separate cold growth and post-death cleanup. Use sufficient samples for p95/p99, and retain raw data. Include a CPU Timeline capture for representative/peak cases; do not sum overlapping nested markers or worker durations.
5. Measure full feature and whole-frame p50/p95/p99, request-to-commit latency, GC, native capacity, fallbacks, outstanding records, and result equivalence. Confirm any substantial finding in a Development Player on the actual target hardware. Safety-enabled correctness runs and representative performance runs are separate.
6. Choose a meaningful CPU/frame regression and improvement threshold before selecting the winning variant. Until workload and target requirements are confirmed, do not hard-code a claimed winning threshold or promise an FPS gain.

## Prioritized remediation plan

| Order | Finding / goal | Concrete change scope | Acceptance / validation | Rollback condition |
|---|---|---|---|---|
| **1** | JOB-AUDIT-001: stable snapshot timing | WeaponComponent queues selection intent; CombatAttackCoordinator owns a documented same-frame capture/evaluation boundary for both branches. Keep owner/version validation and ordered commits | Moving-target/shooter case and 7/8/9 parity; no unintended frame delay or cooldown change | Different target/shot/impact frame outside the explicitly chosen capture contract |
| **2** | JOB-AUDIT-002: numerical equivalence | WeaponTargetSelectionJob returns an explicit serial-fallback signal for exceptional geometry; WeaponComponent honors it before aim/fire. Preserve WeaponTargetSelector as reference | Vertical/degenerate/yaw/range matrix and no-match final aim agree; verify actual scheduled execution | Any changed first-match order, invalid aim, or silent unsupported-input handling |
| **3** | JOB-AUDIT-003: trustworthy capture | Extend BattlePerformanceCapture, its report writer, BattleProfilerMarkers and the coordinator's scopes/counters as needed; include revision/variant/workload metadata | CSV records both branches and entire phases; Timeline separates completion from worker cost | Significant instrumentation perturbation or misleading aggregation |
| **4** | Decide due-job value | Benchmark an equivalent serial collector against AttackDueJob; retain the simpler winning path over supported workload range | Stable due ordering/cancellation/timing; repeatable complete-path result on target hardware | Tail CPU/frame regression or altered event sequence |
| **5** | Tune target batching only if justified | Change threshold/batch policy from measured request **and candidate** workloads; consider shared preparation only with coherent capture | Repeated full-feature benefit and lifecycle/behavior parity | Benefits exist only in synthetic extreme or regression at normal load |

Implementation was not requested in this pass. No fixes above have been applied. Each implementation phase should remain small enough to review separately; no new framework, ECS migration, global worker tuning, or rendering changes are required by these findings.

## Unverified requirements and remaining risks

| Gap | Why it matters | Resolution |
|---|---|---|
| Canonical capture phase / movement order | Current branches observe different times | Confirm intended phase, inspect exact player-loop order, run controlled movement case |
| Minimum hardware, supported release platforms, normal/peak fleets, CPU budget | Needed for portable crossover choices | Use current macOS/M2 setup as a provisional benchmark only; obtain product targets |
| Valid finite numeric domain and angle tolerance | Needed for meaningful parity assertions | Define zero/nonfinite/extreme inputs and angular error contract before tests |
| Real scheduled Burst execution | Attributes/source do not prove runtime compilation or speed | Burst inspection plus safety-enabled scheduled test and Player capture |
| Scene exit / reload-disabled / repeated initialization | Normal teardown source inspection is incomplete runtime coverage | Exercise explicit lifecycle matrix when authorized |
| Reinitializing a released WeaponComponent | Initialize does not reset _isReleased; no actual same-component pooling/reinitialization path was found | Confirm such reuse exists before treating this as a defect or adding reset logic |
| Capacity extremes / interrupted allocation | Normal completed access is safe; failure paths were not injected | Test realistic peak and growth; add capacity behavior only if a concrete requirement warrants it |
| Original coroutine versus centralized scheduler timing | Earlier plan already documents possible one-frame phase differences; this predates the present Jobs audit | Preserve current scheduler for serial/Jobs comparison; validate old timing separately if original-behavior equivalence remains required |

## Final acceptance gates

| Gate | Status | Evidence / blocker |
|---|---|---|
| Evidence and scope | Pass for this audit | Three guides, revision, inventory, storage/access graphs, callers and existing captures documented |
| Correctness and lifetime | Partial | Normal native lifecycle inspected; targeting findings open; no runtime tests |
| Behavioral equivalence | Not accepted | Snapshot and degenerate-direction differences; parity matrix not run |
| Approach suitability | Partial | Target scans plausible; due-state job value unresolved |
| Measured benefit | Not established | No matched complete-path Jobs capture |
| Maintainability | Partial | Coherent owner and small kernels; duplicate selection rules and large orchestration classes |
| Target-platform coverage | Partial | Current macOS Editor inspected; no Player or other target validation |

## Agents used

| Role / stable task name | Configured model / reasoning | Effective model | Task / result |
|---|---|---|---|
| code_explorer / combat_lifecycle | gpt-5.6-luna / medium | Unverified by runner metadata | Read-only callers, identity, teardown and movement-order trace; normal ownership accounted for, freshness risk identified, station path confirmed |
| unity_profiler / jobs_performance_evidence | gpt-5.6-terra / high | Unverified by runner metadata | Inspected six existing captures and read-only Editor configuration; no matched Jobs evidence, instrumentation gap and benchmark plan supplied |

The parent inspected kernels/native buffers and numeric semantics, reconciled findings with the source pack, and prepared this report. Children made no code or asset changes.

## API and repository references

- Context7 was consulted first for Unity Jobs and quaternion contracts; version-specific claims were then checked against Unity 6.4 documentation and installed package sources.
- [Unity 6.4: creating and completing jobs](https://docs.unity3d.com/6000.4/Documentation/Manual/job-system-creating-jobs.html) — completion is the ownership boundary before consuming native output.
- [Unity 6.4: IJobParallelFor](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Unity.Jobs.IJobParallelFor.html) — independent iterations and batch-size tradeoffs. No API migration is required for this audit.
- [Collections 6.4 allocator contracts](https://docs.unity3d.com/Packages/com.unity.collections@6.4/manual/allocator-overview.html) and [installed allocator documentation](/Users/golinsky/Projects/empire-at-war/Library/PackageCache/com.unity.collections@5b6ebd78ccc0/Documentation~/allocator-overview.md) — Persistent ownership/disposal and alias liveness.
- [Installed Burst aliasing contract](/Users/golinsky/Projects/empire-at-war/Library/PackageCache/com.unity.burst@6bb9aca3ef38/Documentation~/aliasing-job-system.md) — job field alias assumptions.
- [Earlier battle review](/Users/golinsky/Projects/empire-at-war/Docs/BATTLE_PERFORMANCE_REVIEW.md) and [ongoing optimization plan](/Users/golinsky/Projects/empire-at-war/EmpireAtWarDocumentation/TODOs/Battle_Attack_Optimization_Plan.md) — advisory historical context, not substitutes for current source or matched measurement.
