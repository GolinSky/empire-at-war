# Unity Jobs, Burst & Threading Rules

Project rule set for any multithreaded / Jobs / Burst code in Empire At War. Condensed from the external pack *01 Threading & Synchronization Rules*, *02 Jobs Audit Playbook*, *03 Agent Prompts & Report Template* (2026-09-24), verified against this repository and adapted to its conventions.

**Installed baseline (verify before relying on version-specific API):** Unity `6000.4.7f1`, Burst `1.8.29`, Collections `6.4.0`, Mathematics `1.3.3`. DI is **Zenject** (not VContainer). No Entities/ECS.

> Prefer clear ownership, independent data and explicit dependencies. Jobs are a tool, not a goal: the target is a simpler, correct, measurable implementation.

---

## 1. Choose the execution model first

| Work | Start with |
|---|---|
| Small gameplay decisions, anything touching GameObjects/Transforms/UI | Main thread |
| Waiting (frames, I/O, Addressables) | Coroutine / UniTask / Awaitable — *async is not parallel* |
| Repeated numeric work over many items (targeting, pathfinding, due checks) | Burst job over plain data |
| Shared mutable managed state | Single owner / message passing; a small `lock` only if really shared |

- `async`/`await`/UniTask do **not** move work off the main thread by themselves.
- Unity object APIs are main-thread only unless the API explicitly says otherwise (`IJobParallelForTransform`, `RaycastCommand`). A lock does not make an engine call legal off-thread.

## 2. Ownership & data flow (MUST)

```text
Main thread: gather snapshot (+ stable ids / generation)
  -> job: compute on copied, Burst-compatible data only
  -> completion boundary: JobHandle.Complete()
  -> main thread: validate id/generation, then apply
```

- One **feature-level owner** (a Zenject service, e.g. `CombatAttackCoordinator`, `ShipNavigationService`) owns buffers, scheduling, completion, result publication and disposal. No global job manager / scheduler framework / EventBus.
- Jobs never receive presenters, components, ScriptableObjects, the DI container or live models. Copy values into structs (`float3`, `int`, blittable structs) first.
- A snapshot must be independent: copying a `NativeArray` struct **aliases** the same memory; `readonly` does not make referenced data immutable.
- Results computed for a request must carry a request id / generation; discard results whose owner was unregistered, pooled or re-requested meanwhile.

## 3. Job rules

| ID | Rule |
|---|---|
| JOB-01 | Inputs are job fields; outputs go to explicit native storage. Writing a scalar field in `Execute` is **not** a return channel (job structs are copied). No managed types, delegates, LINQ, statics with mutable state in Burst code — also check helper methods called from `Execute`. |
| JOB-02 | Mark genuine inputs `[ReadOnly]`; `[WriteOnly]` only if the job never reads the container. Attributes declare access, they do not lock. |
| JOB-03 | Pass producer handles to consumers; combine only where a consumer needs both. Don't chain unrelated work through one global handle; a missing edge is a bug, not an optimization. |
| JOB-04 | Always `Complete()` the covering handle before the main thread reads outputs or reuses/resizes/disposes buffers. `IsCompleted` is only a hint whether to wait — it is not the ownership handoff. |
| JOB-05 | `Schedule` + immediate `Complete` is valid (existing project pattern — Burst still helps). Delaying completion to a later frame is a **gameplay-latency decision** and must be stated explicitly. `Complete()` may run the job on the calling thread. |
| JOB-06 | `IJobParallelFor` iterations must be independent (iteration `i` writes output `i`), order is not guaranteed. `NativeList.ParallelWriter` cannot grow and append order is non-deterministic — reserve capacity first or use per-index output slots. |
| JOB-07 | `NativeDisableParallelForRestriction`, `NativeDisableContainerSafetyRestriction`, unsafe pointers: forbidden unless a written invariant + test justifies them. Never add them to silence a safety error. |
| JOB-08 | Allocators: `Temp` = scope-local, never passed to scheduled jobs; `TempJob` must be freed within 4 frames; `Persistent` for owner-lifetime buffers (preferred for recurring work, grow-by-reallocation only while no job is in flight). One disposer per allocation. |
| JOB-09 | Scheduled jobs cannot be cancelled. Shutdown order: stop accepting requests → stop scheduling → `Complete()` outstanding handles → drop stale results → `Dispose` buffers. Scene unload / Zenject dispose / pooling are **not** completion boundaries by themselves — wire them into this order (`IDisposable`/`ILateDisposable`). |
| JOB-10 | Race-free ≠ deterministic. Define tie-breaking explicitly (e.g. lowest index wins) when ordering matters. |
| JOB-11 | Validate numeric inputs (`math.isfinite`) inside kernels where bad data is possible; provide a serial fallback/flag rather than propagating NaN (see `WeaponTargetSelectionJob.RequiresSerialFallback`). |

## 4. Managed threading (outside Jobs)

- `lock` a private dedicated object; every reader and writer uses it; no `await`, Unity calls, event invocation or job completion under a lock.
- `Interlocked` protects one operation, not an object. `volatile` gives visibility only — never use it as a cancellation or publication protocol.
- `ConcurrentQueue` makes queue operations safe, not the messages inside. Bound admission and drain with a per-frame budget.
- No `Task.Wait()`, `.Result`, `Thread.Join` or busy-wait on the main thread.

## 5. Performance claims

- Measure the whole feature path: gather → prepare → schedule → execute → complete → apply → cleanup. Use `ProfilerMarker`s (see `BattleProfilerMarkers`) around schedule and complete.
- Never report "faster", "thread-safe" or "deterministic" because code compiles, has `[BurstCompile]`, or showed no safety error in one Editor run. Say what was measured and what remains unverified.
- Tune batch sizes only after correctness; don't sum overlapping worker time into frame time.

## 6. Project-specific adaptations (differences from the external pack)

- The pack's example code uses constructor `ArgumentNullException` guards — **not allowed here** (see [[Rules/AGENTS]]: no constructor null guards). Assign injected dependencies directly.
- The pack mentions VContainer — this project uses **Zenject**; the owner service is bound in the scene installer and disposes buffers via `IDisposable`/`ILateDisposable`.
- Collections here is `6.4.0` (Unity 6 versioning), not `2.5`; allocator semantics referenced above are unchanged, but check the package docs for API details.
- Keep job structs `internal`, one type per file, named after the file; `const` fields in `UPPER_SNAKE_CASE`.
- Tests that exercise jobs run through the Unity Test Framework only when the user explicitly asks (see Unity Test Safety in [[Rules/AGENTS]]).

## 7. Audit checklist (for reviewing an existing Jobs feature)

1. Record revision, Unity/package versions, target platform.
2. Write the behavior contract: work, scale, expected output/order, deadline (same frame vs later), lifetime owner, goal.
3. Inventory jobs (scheduler, rate, inputs/outputs, consumer) and allocations (allocator, aliases, readers/writers, final handle, disposal site).
4. Check: thread affinity, copied data, iteration independence, access attributes, `Complete()` before consumption, capacity/overflow, allocator lifetime, disposal on every exit path, stale-result rejection.
5. Decide approach: keep / keep with fixes / simplify to synchronous / rework batching / different model / insufficient evidence.
6. Report findings with severity + confidence + evidence label (*observed in source*, *reproduced*, *measured*, *hypothesis*, *not verified*). Correctness first, design second, measured optimization third.

## Existing project Jobs usage

- `Components/Weapon/WeaponTargetSelectionJob.cs` and `AttackDueJob.cs` — `IJobParallelFor`, Burst, scheduled and completed in the same frame by `CombatAttackCoordinator` with `Allocator.Persistent` buffers grown on demand and disposed in `Dispose()`. Use this as the reference pattern.

## References

- Unity 6 manual: job system (creating jobs, dependencies, NativeContainer access, copying containers).
- Burst 1.8 manual: C# type support, aliasing, safety settings.
- Collections manual: allocators, parallel readers/writers.
