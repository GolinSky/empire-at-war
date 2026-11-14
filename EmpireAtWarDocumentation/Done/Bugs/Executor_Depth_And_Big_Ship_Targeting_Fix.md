---
category: Bugs
status: done
created: 2026-11-14
completed: 2026-11-14
acceptance: pending-user
---
# Executor Depth and Big-Ship Targeting Fix

## Goal
- Executor flies lower so it looks ~20% smaller on screen at mid zoom; clearly below every station.
- No ship hull intersects a planet on any map size.
- Clicking (select / right-click attack) an enemy station hits the station even when a deep capital sits under it.
- Attack order on a capital surrounded by own ships: every ordered ship engages, not only the inner ring.

## Phases
1. Executor depth (data only).
2. Planet surface ceiling (code + data).
3. Station-over-ship click ranking (code).
4. Attack-order stall recovery (code).
- Phases are independent; 1 → 2 order matters (ceiling derives from deepest hull).
- Test categories: `Ships`, `Map`, `BattleInput`, `Navigation`, `Combat`. Tests authored only; execution disabled by local policy.

---

## Phase 1 — Executor depth

### Findings (measured 2026-11-14, Unity eval)
- `ShipHeightTiersData.GetHeight(tier)` = `offset` (50) + tier value.
- Shared `ShipHeightTiersData.asset`: `deep` −391 → Executor and Malevolence `Height` −341.
- `MandatorHeightTiersData.asset`: `deep` −570 → Mandator `Height` −520.
- Baked hull spans (relative to `Height`):

| Ship | Height | HullBottom | HullTop | World span |
|---|---|---|---|---|
| Executor | −341 | −74.9 | +74.1 | −416 / −267 |
| Mandator | −520 | −55.6 | +55.7 | −575 / −464 |
| Malevolence | −341 | −55.0 | +55.0 | −396 / −286 |

- User saw "Executor deeper than Mandator in 3D" → data says opposite. Executor model is 1900 u long vs Mandator 957 u, so it reads closer/bigger.
- Stations: `MapGenerationSettings.StationHeight` −40. Prefab renderer bottoms (all levels): Empire −106, Rebellion −194, Republic −79, Separatist −110 → world bottoms −146 / −234 / −119 / −150.
- Today Executor top −267 is only 33 u under the Rebellion station bottom (−234) → visually "under the station".

### Depth math
- Apparent size ∝ 1 / view-ray distance; at fixed pitch ray distance ∝ (camera height − ship height).
- 0.8× size → gap × 1.25 → `h' = C − 1.25 × (C − h)`, `h` = −341.
- `CameraData.ZoomRange` = 60…1175.

| Camera height C | h' for −20% | Shrink at chosen h' −576 |
|---|---|---|
| 60 (min zoom) | −441 | −37% |
| 600 (mid) | −576 | **−20%** |
| 1175 (max zoom) | −720 | −13% |

### Decision
- **Chosen (user, 2026-11-14): mid zoom → Executor `Height` −576.** Hull span −651 / −502.
- New asset `ExecutorHeightTiersData` (copy of shared, `deep` −626). Same pattern as Mandator.
  - Why: only Executor moves; Malevolence stays on shared `deep`.
  - Avoid: changing shared `deep`; scaling the model (user wants depth, not size).
  - Rejected: −720 (full zoom-out target) → much larger planet move.
- Executor becomes deepest ship (Mandator −520).

### Clearance checks (all pass at −576)
- Stations: hull top −502 vs deepest station bottom −234 → 268 u clear.
- Asteroid fields: `FieldFloor` −150; Deep tier must have top ≤ floor → −502 ✓ (`AsteroidFieldHeightTests`).
- Decor rocks: `DepthShare` rocks in [−150, 110]; jitter rocks ±12 → no conflict.
- Planet decorations (junk, docks, containers): y = ±15 jitter → no conflict.
- Space dust: `MapSpaceDust.prefab` `heightRange` (−650, 180); test needs `x ≤ height − 50` = −626 ✓ (24 u spare). Executor hull bottom −651 sits 1 u under the dust volume — visual only.

### Steps
- [x] Create `Assets/Settings/Data/Ship/Empire/ExecutorHeightTiersData.asset` via Unity (duplicate `Shared/ShipHeightTiersData.asset`), set `deep` −626.
- [x] `ExecutorShipData.heightTiers` → new asset. Save + reserialize both assets; check console.
- [x] `ShipHeightTierTests`: Executor case → `ShipHeightTier.Deep, -576f`.
- [x] Source-check height-keyed systems at −576: hyperspace arrival (`ShipMoveComponent` start y = `Model.Height`), wreck/explosion spawn, cinematic camera framing (`CinematicShotSolver`), health/hardpoint overlays, selection ring, tactical markers, fog/radar reveal.
- [x] Source-check overlays/markers: selection anchor, hardpoint world position and entity marker transform follow the hull; visual acceptance queued.

---

## Phase 2 — Planet surface ceiling

### Findings
- `PlanetFactory.Create`: `position.y = prefab.y × PlanetScale`; `localScale = prefab.scale × PlanetScale`.
- Planet prefab surface+cloud top (prefab space): generic planets −65 (prefab y −1589); Kamino −283 (prefab y −1150).
- `PlanetScale`: Small 6, Medium 8.4, Large 10.5.

| Size | Generic planet top | Kamino top |
|---|---|---|
| Small | −390 | −1700 |
| Medium | −546 | −2380 |
| Large | −682 | −2975 |

- Planet has no nav obstacle outside the orbital-ring layout → ships fly over its footprint.
- Clipping today: Executor (−416) on Small; Mandator (−575) on Small and Medium.
- Orbital ring: `PlanetView.FitObstacle(center, radius)` rescales to obstacle radius and keeps scaled authored clearance; top unchecked.
- Only guard: `PlanetObstaclePresentationTests` → top < 0.
- `PlanetSuperWeaponOrigin` uses `planetTransform` + `surfaceRadius × scale` → follows the planet automatically; verify beam origin after move.

### Decision
- Add `PlanetSurfaceCeiling` to `MapGenerationSettings` (default −701 = deepest hull bottom −651 − 50 margin).
- `PlanetFactory.Create`: after placement/scale (and after `FitObstacle`), measure visual top; if above ceiling, shift down by the difference. One rule for all sizes and the orbital ring.
  - Why: scale-dependent; hand-editing prefab Y drifts as ships or scales change.
  - Avoid: per-prefab Y edits; nav obstacle for planets (changes gameplay routing).
- Resulting moves: Small −311, Medium −155, Large −19; Kamino unchanged.

### Sketch
```csharp
// PlanetView
public float VisualTop => /* max over planet+cloud renderers of center.y + sphere radius */;

// PlanetFactory.Create, after FitObstacle
float excess = planet.View.VisualTop - _settings.PlanetSurfaceCeiling;
if (excess > 0f) planet.transform.position += Vector3.down * excess;
```
- `VisualTop` reuses `GetVisualRadius` logic (renderer `localBounds` valid while inactive).

### Steps
- [x] `MapGenerationSettings.PlanetSurfaceCeiling` (serialized, `[field: SerializeField]`), set −701 in `MapGenerationSettings.asset`.
- [x] `PlanetView.VisualTop` + `PlanetFactory` clamp.
- [x] Test (`Map`): for every planet prefab × every `MapSize` (incl. orbital-ring fit), created top ≤ ceiling.
- [x] Test (`Map`/`Ships`): ceiling < min over all `ShipData` of `Height + HullBottom` (relationship, no pinned numbers).
- [x] Preserve existing proportional-depth `PlanetObstaclePresentationTests` source unchanged. Tests not run.
- User check queued: Small planet framing and superweapon beam surface origin.

---

## Phase 3 — Station not selectable / not attackable

### Findings
- `SelectionQuery.CollectAt` (`Services/Selection/SelectionQuery.cs`):
  - Squadron icons first.
  - `RaycastAll` → candidates; `ComparePointCandidates`: **units before structures**, then hit distance.
- Ray through the station continues to the Executor below → Executor ranks first.
- Callers that inherit the bug: `PlayerOrderInputHandler.HandleCommand` / `CanIssueAttackMove` (right-click attacks the Executor), `SelectionService` click select, `UnitHoverService`, `HealthOverlayPresenter`, `HardPointOverlayPresenter`, `MarkerInputController`.
- Lowering Executor does **not** fix it — the ray still reaches it.
- Trigger: [[Done/Bugs/Reinforcement_Spawn_Near_Station_Fix]] allows hulls under stations when spans clear → AI drops Executor beneath its station.
- `IStationHullFacade.HullBottom` (from the station `BoxCollider`) already exists.

### Decision
- A unit hit **below a structure's hull bottom, behind that structure's hit along the ray** ranks as a structure (then distance). Ships beside, above or in front of a station keep unit priority.
  - Why: keeps the original intent ("structures would hide ships under or behind them") for ships at gameplay height; the deep-capital case is the only one where the station is the obvious click.
  - Alt (not chosen): click-again cycling through stacked candidates — more UI work.
- Implement as a post-collect pass so the sort stays transitive.

### Sketch
```csharp
// PointCandidate gains HitY; after collecting hits:
for each unit candidate u:
    for each structure candidate s with IStationHullFacade hull:
        if (s.Distance < u.Distance && u.HitY < hull.HullBottom) { u.IsUnit = false; break; }
_pointCandidates.Sort(COMPARE_POINT_CANDIDATES);   // unchanged comparer
```
- `PlayerOrderInputHandler` friendly-hull fallback (`_attackCandidates`) unchanged: an enemy station still wins over own ships on top.

### Steps
- [x] `SelectionQuery`: store hit y in `PointCandidate`; demote units under a nearer structure's hull bottom.
- [x] Tests (`BattleInput`, `SelectionQueryTests`):
  - Enemy Executor under enemy station → station first.
  - Ship beside/behind station at gameplay height → ship first (existing behaviour).
  - Squadron icon still first.
- [x] Test (`PlayerOrderInputHandlerTests`): right-click station above enemy capital → `IssueAttack(station)`.
- [x] Exposed hull behavior covered by authored source regression; interaction acceptance queued.

---

## Phase 4 — Attack order stalls around big ships

### Findings
- `AttackTargetState` moves with `MoveToPosition(..., preserveCourse: true)` both for the formation leg (`MovementTargetPosition`) and standoff (`StandoffPosition`).
- Standoff = target + (ship − target).normalized × `max(range × 0.65, ContactDistance)` → for the outer ring it lies inside own ships.
- `ShipNavigationService.PlanRoute`: no clear candidate in rings or fallback → `IsStationary`.
- `ShipMoveComponent.Plan`: stationary → `_failedDestination = destination`.
- `ShipMoveComponent.SetTargetPosition`: `preserveCourse` request within `NavigationRadius` of `_failedDestination` → **returns silently**. Stationary target → same standoff → ship idles forever, out of range.
- Attack-move works: `ShipEngagement.Pursue(newTarget: true)` → `preserveCourse: false` clears the lock; search re-runs every 0.25 s.
- `HasEnoughRange(distance, contactDistance, class)` = `distance ≤ max(engage × ENGAGE_RANGE_FACTOR, contact × CONTACT_ENGAGE_FACTOR)`, centre-to-centre.
- Existing pattern: `AbilityApproachState` retries a failed path (`RETRY_INTERVAL` 0.5 s, `MAX_STALLED_ATTEMPTS` 3) — covered by `AbilityApproach_FailedPathRetriesThenCompletesWithoutCasting`.

### Decision
- When not moving, out of range and the last request did not move the ship (stalled), retry on a **rotated bearing** around the target with `preserveCourse: false`, every 0.5 s.
  - Rotate the approach angle by ± steps (e.g. 20°, −20°, 40°, −40° …) so the ship slides around the ring to an open slot.
  - Never complete the order on stall (unlike ability approach) — keep trying while the target lives.
  - Why: matches the working attack-move path (non-preserved course clears `_failedDestination`) and the existing retry pattern.
  - Avoid: changing `ShipMoveComponent` lock semantics globally (protects autonomous pursuit from replanning every frame).
- Keep current hysteresis/closing-range logic for the normal case.

### Sketch
```csharp
private const float STALL_RETRY_INTERVAL = 0.5f;
private const float STALL_BEARING_STEP_DEGREES = 20f;
private float _stallRetryTimer;
private int _stallAttempt;
private Vector3 _lastAttemptPosition;

// in UpdateMoveState / UpdateFormationMove, when !IsMoving && !inRange:
if (stalled && (_stallRetryTimer -= dt) <= 0f) {
    float angle = STALL_BEARING_STEP_DEGREES * ((_stallAttempt / 2) + 1) * (_stallAttempt % 2 == 0 ? 1 : -1);
    Vector3 bearing = Quaternion.AngleAxis(angle, Vector3.up) * (ship - target).normalized;
    _shipMoveComponent.MoveToPosition(target + bearing * standoffDistance, preserveCourse: false);
    _stallAttempt++; _stallRetryTimer = STALL_RETRY_INTERVAL;
}
```
- Reset `_stallAttempt` on target change, on a successful move, and in `Exit`.

### Steps
- [x] `AttackTargetState`: stall detection + rotated-bearing retry (both formation and single paths). File is ~200 lines → keep the retry in a small private method; flag refactor if it grows.
- [x] Tests (`Navigation`, `ShipPursuitTests`):
  - Stationary target, movement fails (stays put) → next retry issues a different destination with `preserveCourse: false`.
  - Successful move resets attempts.
  - In-range ship never retries.
- [x] Source-trace `Attack_CommitsLegDespiteTargetMotionAndOwnDetour` and `Attack_TargetClosesRange_StopsWithoutAnotherPlan`; existing moving-leg commitment and range-stop branches retained. Tests not run.
- Deferred optional follow-up: count range to the target hull edge for huge targets (contact distance) — only if outer ring still sits just out of range after the retry fix.

---

## Edge Cases
- Hardpoint hover already wins over hulls → unaffected by Phase 3.
- Marquee select uses screen positions, not rays → unaffected.
- AI orders (`EnemyTaskForceExecutor`) also use `AttackTargetState` → Phase 4 helps AI fleets around big targets too.
- Many outer ships retrying on the same tick → 0.5 s interval + nav plan queue; check profiler on large battles (read `Rules/BATTLE_PERFORMANCE_GUIDELINES` first).
- Save/load source-verified: `UnitSaveService.Restore` → `SpawnRestoredShip` → `ShipMoveComponent.Construct` replaces saved Y with `Model.Height`; restored initialization applies −576 immediately.

## Files
- Phase 1: `Assets/Settings/Data/Ship/Empire/ExecutorShipData.asset`, new `ExecutorHeightTiersData.asset`, `Tests/Editor/Navigation/ShipHeightTierTests.cs`.
- Phase 2: `Entities/Map/MapGenerationSettings.cs`, `Settings/Data/Models/Map/MapGenerationSettings.asset`, `Services/Factory/PlanetFactory.cs`, `Entities/Planet/PlanetView.cs`, `Tests/Editor/Map/PlanetObstaclePresentationTests.cs`.
- Phase 3: `Services/Selection/SelectionQuery.cs`, `Tests/Editor/BattleInput/SelectionQueryTests.cs`, `PlayerOrderInputHandlerTests.cs`.
- Phase 4: `Entities/Ship/StateMachine/AttackTargetState.cs`, `Tests/Editor/Navigation/ShipPursuitTests.cs`; reference `AbilityApproachState.cs`, `ShipMoveComponent.cs`, `ShipNavigationService.cs`.

## User Acceptance
- Gates recorded in [[TODOs/User_Acceptance_Queue]]; none were run during implementation.
- [ ] Executor looks ~20% smaller at mid zoom; still reads as the biggest ship.
- [ ] Small map: no ship clips the planet; planet framing acceptable.
- [ ] Enemy station with Executor under it: click selects station; right-click attacks station.
- [ ] Select all → attack surrounded Executor: outer ships move to open slots and fire.

## Reusable Guidance
- [[Architecture/Map_Archetypes#Planet surface ceiling]] — ceiling ownership, hull margin, inactive renderer bounds and superweapon origin.
- [[Architecture/SELECTION_SYSTEM]] — post-collect depth demotion and unchanged boundary/front-hit priority.

## Execution Evidence — 2026-11-14
- Executor profile duplicated/saved/reserialized through Unity. Loaded height −576, hull span −650.876…−501.935. Malevolence remains −341; Mandator remains −520. No asset import/serialization errors observed.
- Code and regression sources implemented for planet ceiling, station hit ranking and 0.5 s attack stall retries; targeted `git diff --check` clean. Checked test boxes mean authored source, not passing execution.
- Unity script compilation completed with `failed: false`, `errors: []`. Added the missing `EmpireAtWar.Entities.BaseEntity` import in concurrently edited `SquadronIconComponent` to unblock compilation; marker implementation left intact.
- Map settings saved/reserialized through Unity with ceiling −701 and dirty=false. Loaded ship-data inventory reports deepest hull Executor −650.876 → minimum ceiling gap 50.124 units. Console had no current errors after reload/save. No tests run under local policy.
- Height source audit: hyperspace/restoration uses `Model.Height`; wreck/explosion anchors, cinematic target, selection/health/hardpoint overlays and entity tactical markers use current world transforms. Radar uses XZ. Visual behavior remains user acceptance.
- `AttackTargetState` is now 259 lines (previously 222); consider a focused pursuit extraction if expanded again. No unrelated refactor included.
- Serena failed MCP/direct HTTP connection despite launcher reporting running; Graphify freshness failed on busy update lock. Live source used.
- Reusable rules extracted to [[Architecture/Map_Archetypes#Planet surface ceiling]] and [[Architecture/SELECTION_SYSTEM]].
