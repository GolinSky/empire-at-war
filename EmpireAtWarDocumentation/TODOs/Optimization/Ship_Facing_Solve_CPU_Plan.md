---
category: Optimization
status: in-progress
created: 2026-11-14
updated: 2026-11-14
---
# Ship facing solve CPU plan

## Progress (2026-11-14)

- Implemented fixed 2° sweep reuse, parent-frame yaw edge tests, and a Burst sweep for dense groups. Ambiguous headings that can beat the current score are checked with the existing aim selector. Battery priority and heading tie order are preserved by design. Seeded parity fixtures compiled and remain unrun.
- Added deterministic 0.8–1.2 s refresh intervals and a shared FIFO. It processes up to two refresh callbacks per frame and stops starting work after roughly 1 ms. First and changed targets solve immediately. Retained the 2° threshold until matched trigger data is available.
- Added production group and trigger metadata. Original frame 599 contains four solves at 8.63–9.14 ms without context metadata. That capture cannot identify the ships, targets, or trigger reasons.
- Removed the runtime health fallback array. First use or buffer growth can still allocate. The latest in-range capital probe observed managed allocations during the probe and does not isolate steady-state allocation call stacks.
- Unity compilation and console checks passed. Automated tests disabled under the local override. Same-map checkpoint was later than the baseline and combat was too light for a matched FPS comparison.
- Natural capture `Logs/RenderAudit/20261114T152009525Z-profiler`: one pre-queue solve, C9979 → structure, timer refresh, 3 mounts × 5 targets, 13 headings / 135 aim checks, 0.062 ms and zero GC.Alloc children. GC call-stack recording restored to None.
- In-range diagnostic `Logs/RenderAudit/20261114T153113515Z-profiler`: 50 capital solves, 49 ≤0.681 ms, max/p99 1.111 ms (14 mounts × 41 targets, 180 headings, 2,433 aim checks). One solve per probe frame.
- Dense 40×30 diagnostic `Logs/RenderAudit/20261114T153001520Z-profiler`: 30 solves, roughly 2.4 ms typical, p99/max 4.163 ms. Full 180 headings and 180,969 aim checks. Warmed synthetic kernel recorded zero GC.Alloc children in the standard capture.
- Post-change 10 s CSV `battle_20261114_153206_897.csv`: 2,025 frames, 0 ship-order frames above 6 ms, zero target requests. Use it only as an idle bound.
- Remaining: implement exact turn intervals and verify score/turn parity before claiming the dense target. Profile a matched crowded fight with allocation call stacks, confirm trigger mix and aggregate facing budget, and check capital behavior in game.

## Goal
- Remove the recurring ship-facing spikes: **35.2 ms of a 60.5 ms frame** (Profiler frame 599, four solves) and **5.9–10.2 ms** single solves.
- Keep facing behavior: main batteries are tried first, then secondary batteries. Pick the heading with the most weighted mounts able to fire, and the smallest turn among ties.
- Targets: `ShipFacingSolve` **≤ 1.0 ms per solve** (p99) and **≤ 1.5 ms of facing per frame**. No once-a-second ship-order spikes above 6 ms.

## Evidence (2026-11-14 capture)
- Profiler: `Logs/RenderAudit/20261114T145948597Z-profiler/profiler.data`. The render thread mostly waited during the spikes, so they are main-thread CPU cost.
- CSV: `%LOCALAPPDATA%Low/GolinSkyCompany/EmpireAtWar/BattleCaptures/battle_20261114_145930_348.csv` (786 frames, 10 s). Ship orders took more than 6 ms on 33 frames. On six of them, ship orders took about 26 ms, roughly once per second.
- Excluding those 33 frames gives an average of **82 FPS**, against **79 FPS** overall. Facing explains the dips, not the whole FPS drop.
- The CSV recorded **≈205 MB** of managed allocations in 10 s, in bursts, with callers unknown. This is out of scope here except for the facing path (see Phase 4).
- Exclude the **14.65 s** hitch right after the Profiler window. It is the capture export.
- History: an earlier facing sweep fix (`72c9d8312`, 2026-10-12) brought `FacingSolve` max from 9.14 to 1.55 ms. Possible regression source: `417b02029`, "Implement capital siege damage and forward main batteries", which added main-battery groups and limited-yaw forward mounts. Bisect only if it is cheap.

## Cost model (source verified at `17597d5df`)
- Call chain: `ShipMoveComponent.LookAtTarget` (`ShipMoveComponent.cs:246`) → `WeaponComponent.GetFiringTurnAngle` (`:390`) → `WeaponFacingSolver.FindTurn` (`WeaponFacingSolver.cs:48`).
- A solve is triggered when the ship is not moving and any of these hold: no previous solve, a target change, more than `FACING_RESOLVE_INTERVAL` = **1 s** since the last solve, or a bearing change over `FACING_RESOLVE_ANGLE` = **2°**. There is no stagger and no shared budget.
- `FindTurn(group)` checks up to **180 samples** (`TURN_STEP` = 2°, alternating −/+). Each sample:
  - rotates every target with `Quaternion.AngleAxis` and `rotation * target`;
  - for each mount and each reachable target, runs a planar range check, then for limited-yaw mounts calls `WeaponTargetSelector.TryCalculateAim`. That call repeats the range check (`PlanarGeometry.Distance`, with a sqrt) and runs `LookRotation`, `Quaternion.Inverse`, multiply and `eulerAngles`. That is the most expensive part.
- An early exit happens only when `score == maximumScore`, the sum of all eligible mounts' weights. If any mount can't hit at any heading (for example a rear mount against a distant target), the solver **always runs the full 180 samples**. This is likely the common case.
- When main batteries score 0, a full secondary sweep follows, doubling the cost.
- Worst case: 180 × mounts × targets. For example, 180 × 40 × 30 gives about 216k aim checks per solve.

## Rules
- Turn behavior must match the current solver for the same inputs: the same best score and a turn within ±`TURN_STEP` / 2. Check this with a reference-comparison test, not by pinning stat values.
- No new allocations on the solve path. Keep the buffers already in `WeaponFacingSolver`.
- The ship entity owns the solve request. Components report through interfaces (`IWeaponFacing`). A scheduler must not command `ShipOrderRunner` states.
- Tests are not run locally (disabled by the local override). Test files may be written. Categories when they are run: `Combat` (solver) and `Ships` / `Navigation` (move component).

## Phase 0 — Attribution (no behavior change)
- [ ] Add dev-only markers inside the solve: `FacingSolve.Main`, `FacingSolve.Secondary`. Counters: samples evaluated, aim checks, mounts, targets, and whether it exited early.
- [ ] Add a dev-only count of solve triggers by reason: first, target change, 1 s timer, bearing over 2°. This settles whether the 2° rule fires every frame against close or fast targets.
- [ ] Check frame 599 in the existing `profiler.data` for which ships and target types (station vs. capital) caused it before making a new capture.
- Done gate: a ranked list of (ship type, mounts × targets, samples, trigger reason) for the top spikes.

## Phase 1 — Make each sample cheap (same results)
- [ ] Replace `TryCalculateAim` in `Score` with planar math:
  - Precompute, for each mount, the inverse parent yaw as `sin`/`cos`, plus `RangeSquared`. Parent rotation is constant within the counter-rotated frame.
  - Each check: `dx, dz` → squared-range test (already done, so drop the second `Distance`) → rotate into the parent frame → `atan2` local yaw → compare with `MinYaw`/`MaxYaw`.
  - Keep `WeaponTargetSelector.TryCalculateAim` unchanged for real weapon aiming.
- [ ] Replace `Quaternion.AngleAxis` per sample with a static 180-entry `sin`/`cos` table and 2D target rotation into `Vector2`/float buffers.
- [ ] Skip the secondary sweep's target rotation when the main sweep already rotated the same targets. Cache turned targets per step across the two groups if memory stays small (180 × targets).
- Expected: about 5–10× less work per aim check, with identical scores. Verify with Phase 0 counters plus marker times.

## Phase 2 — Reduce samples (pick one, after Phase 1 is measured)
- **Chosen if Phase 1 isn't enough: exact turn intervals.**
  - For each (mount, target) pair, compute the hull-turn intervals where the target is in range and inside the yaw arc:
    - Range: closed form from the law of cosines on the target radius, mount radius and range.
    - Yaw-arc edges: `a·cosθ + b·sinθ + c = 0`, solved per arc edge.
  - Per mount, merge these into one set of turn intervals (the mount can fire at any target).
  - Sweep the interval endpoints and find the maximum weighted coverage. Choose the covered turn closest to 0. Cost is O(M·T + E log E), with no 180 factor.
  - Why: exact, and removes both the sample count and the "can't reach max score" full-sweep problem.
  - Avoid: coarse-to-fine sampling (10° then 2°). It can miss narrow firing windows and change behavior.
- Fallback, simpler: tighten `maximumScore` to the sum of weights of mounts whose (radius ± range) can reach any target radius at all. `AddMount` already filters by this, so first confirm in Phase 0 whether the full-sweep cases come from yaw limits rather than range.

## Phase 3 — Spread solves across frames
- [ ] Desynchronize the 1 s timer. Give each ship a phase offset (from entity id) and add ±20% jitter to `FACING_RESOLVE_INTERVAL`. This breaks the once-a-second lineup in the CSV.
- [ ] Add a shared facing budget, modeled on `ShipNavigationService.LateTick`:
  - `MAX_QUEUED_PLANS_PER_FRAME` = 4 and `PLAN_BUDGET_MILLISECONDS` = 2.0, using `Stopwatch` after the first item.
  - Facing would use at most N solves per frame and about 1.0 ms after the first solve.
  - Timer and bearing refreshes are queued. The ship keeps rotating toward `_solvedFacing` until its turn comes.
  - **First solve and target change run immediately.** Players should see an instant reaction to an order.
- [ ] Re-check the 2° bearing rule using Phase 0 trigger counts. If it dominates, scale the threshold with target distance (a fixed lateral displacement) or raise it to about 5°.
- Placement: a focused `ShipFacingSolveScheduler` service under `Services/Ship`, or reuse the navigation queue pattern. Do not grow `ShipMoveComponent` (already over 200 lines).

## Phase 4 — Facing-path allocations
- [ ] `CollectFacingTargets` fallback calls `IHealthModelObserver.GetShipUnits(HardPointType.Any)`. When the target has no hardpoint facade and has hardpoint adapters, this allocates an array on every solve (`HealthComponent.cs:207`). Add a non-allocating overload that fills a list, or cache it per health version.
- [ ] Confirm there is no `GC.Alloc` under `ShipFacingSolve` in a capture with allocation call stacks on. Broader 205 MB/10 s attribution belongs to [[TODOs/Optimization/Battle_CPU_Performance_Plan]].

## Verification
- Source: compile, and check the console for errors.
- Tests (written, not run locally): an equivalence test comparing the new solver with the current sweep, kept as a test-only reference, over seeded random mount/target layouts (full-circle, limited-yaw, out-of-range, main + secondary). Add a scheduler test: N requests → at most budget per frame, and first/target-change solves are immediate.
- User acceptance: the same skirmish setup, 10 s CSV + Profiler. Results must meet the Goal targets, ignoring the export hitch. Facing must look the same in game: stationary capitals bring their main batteries onto the target.

## Files
- `Assets/Scripts/Components/Weapon/WeaponFacingSolver.cs`
- `Assets/Scripts/Components/Weapon/WeaponTargetSelector.cs` (unchanged; reference only)
- `Assets/Scripts/Components/Weapon/WeaponComponent.cs:390` (`GetFiringTurnAngle`, `CollectFacingTargets`)
- `Assets/Scripts/Components/Ship/Movement/ShipMoveComponent.cs:246` (`LookAtTarget`, resolve constants `:32-33`)
- `Assets/Scripts/Services/ShipNavigation/ShipNavigationService.cs:133` (budget pattern)
- `Assets/Scripts/Components/Health/HealthComponent.cs:207`
- `Assets/Scripts/Tests/Editor/Combat/WeaponFacingSolverTests.cs`

## TODO order
1. Phase 0 → 2. Phase 1 + Phase 3 timer jitter (cheap, independent) → 3. measure → 4. Phase 3 budget or Phase 2 intervals, only if targets are still missed → 5. Phase 4 → 6. user capture.
