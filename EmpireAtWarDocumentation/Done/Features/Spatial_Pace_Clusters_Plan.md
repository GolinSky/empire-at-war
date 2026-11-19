---
category: Features
status: done
created: 2026-11-19
acceptance: pending-user
completed: 2026-11-19
---
# Spatial Pace Clusters (Formation Speed by Cluster)

## Goal
- Replace "whole selection → one `GroupPace` → slowest speed" with **spatial pace clusters** built once per order.
- Right-click = formation move (cluster pace). Double right-click = loose move (own speeds) + **green** wave feedback.
- Cheap: O(n²) clustering only at order issue; per-tick cost O(1) per ship; merge check ≤ 1 Hz.

## Current State (verified 2026-11-19)
- `GroupPace` (`Entities/BaseEntity/Orders/GroupPace.cs`): one per `IssueAttack`, all moving receivers, live min `BaseSpeed`. → [[Done/Features/Group_Attack_Speed_Matching_Plan]]
- Used only by `Attack` / hard-point attack. `Move`, `AttackMove`, waypoint, guard, retreat: **no pace**.
- `ShipOrderRunner.JoinPace/LeavePace/UpdatePace` → `IShipMovement.SetSpeedCap`; release at `AttackTargetState.HasReachedRange` or any new order.
- `Squadron` mirrors it → `ISquadronFlightComponent.SetSpeedCap`.
- `ShipMoveComponent.PaceSpeed` = `+∞` while Arriving / ion-disabled / anchored.
- `GroupPace.SharedReplacement`: whole group retargets onto the first replacement found (`ShipOrderRunner.CompleteOrder`).
- Move feedback: `UnitOrderFeedbackUi.PlayMovement` (blue `movementWave` + `movementEcho`), driven by `UnitOrderService.OrderIssued` → `UnitOrderFeedbackUiController.HandleOrder`.
- Double click available: `IPointerInput.ClickCount` (used by `SelectionService`).
- No route-remaining-length API on `IShipMovement`.

## Debate: speed unification ≠ cohesion
- Claim: same max speed does not keep a formation; turn rate, acceleration, detours and spacing pushes still spread ships.
- Agree partly. Full progress sync (shared timeline, leader/slot steering, route-length ETA) needs per-ship route length and a cluster controller every tick → cost + complexity the game doesn't need.
- Confirmed by user 2026-11-19: partial agreement; cheap middle ground accepted.
- Cheap middle ground (chosen): **cap + lag correction**. Each member reports straight-line distance to its slot; ships ahead of the cluster's mean progress by > `LEAD_TOLERANCE` get cap × `AHEAD_SLOWDOWN`; ships behind run uncapped at own `BaseSpeed` to catch up. One float per ship, no routes.
- Straight-line distance is wrong across obstacles → only valid because clustering already rejects members whose link segment is blocked (similar routes). Accepted error.
- Avoid: slot steering, formation-shape keeping in flight, ETA from route planner, per-frame reclustering.

## Decision (confirmed by user 2026-11-19)
- Orders using clusters: `Move`, `Attack`, hard-point attack, `AttackMove`. Unchanged (own speed): waypoint, guard, retreat, hunt, stop.
- Double right-click → `MovePace.Loose` for `Move` only. Second click re-issues the move; no click delay on first click.
- AI uses the same `UnitOrderService` → task-force moves get clusters automatically; single-ship AI moves = singletons (no pace).
- Membership is fixed at issue. No re-clustering; only **detach** (straggler/stuck/engaged) and **merge** (same order only).
- Detached ships never rejoin within that order (no flicker).
- Squadrons: join cluster only if linked at issue; leave on engagement (existing `IsInAttackZone`) → interceptors break away freely.
- Not saved; not run on online clients (host-only, as `GroupPace` today).

## Implementation

### 1. `PaceCluster` (rename/extend `GroupPace`)
- Keep `Join/Leave/Report/Pace` + `SharedReplacement` (now per cluster).
- Add per member: `Remaining` (distance to slot), `JoinPace` speed at issue.
- `Pace` = min `BaseSpeed` of members with speed ≥ `STRAGGLER_SPEED_RATIO × issuePace`.
- `MeanRemaining` (lazy, dirty flag) for lag correction.
- `Centroid`, `Radius`, `Heading` computed lazily only for merge checks.

### 2. `PaceClusterBuilder` (pure C#, `Entities/BaseEntity/Orders/`)
- Input: receiver positions (XZ), nav radii, own speeds. Output: cluster index per receiver.
- Single-link union-find, O(n²), n = selection size (≤ ~50).
- Link A–B when all hold:
  - `dist ≤ max(LINK_DISTANCE, LINK_RADIUS_FACTOR × (rA + rB))`
  - route compatible: `ShipNavMesh.IsUnpenalizedSegment(A, B)` (or obstacle segment test) → asteroid between = no link
  - speed compatible: `max/min speed ≤ LINK_SPEED_RATIO` **or** one is a squadron (fighters escort; release on engagement)
- Clusters of 1 → no `PaceCluster` (null, own speed).

### 3. `UnitOrderService`
- `CreatePace(receivers)` → `CreateClusters(receivers)` → `PaceCluster[]` per receiver.
- `IssueMove(receivers, point, MovePace pace = Formation)`; `Loose` → no clusters.
- `IssueAttackMove` → clusters passed to `AttackMoveTo(..., engagement, cluster)`.
- `Publish` → `UnitOrder` gains `bool IsLoose`.

### 4. Ship / squadron runtime (`ShipOrderRunner`, `Squadron`)
- `MoveTo(world, PaceCluster cluster)`: on `_orders.Matches(Move, destination)` still call `JoinPace(cluster)` (double-click to same point must drop the pace).
- `UpdatePace` per tick (O(1)):
  1. Report `PaceSpeed`, `Remaining`.
  2. Lag correction: ahead by > `LEAD_TOLERANCE` → cap = `Pace × AHEAD_SLOWDOWN`; behind → cap = `+∞`; else cap = `Pace`.
  3. Straggler: behind mean by > `max(STRAGGLER_DISTANCE, 3 × cluster radius)` for > `STRAGGLER_TIME` → `LeavePace` (own speed).
  4. Stuck: `NavigationCurrentSpeed < STUCK_SPEED` while moving → report `+∞` (releases sync, navigation does local recovery); still stuck after `STUCK_DETACH_TIME` → `LeavePace`.
  5. Release triggers (existing + new): Arriving at slot, `HasReachedRange`, AttackMove engagement starts, any new order.
- Already-engaged ship receiving `Attack` on its current target in range → no join (existing `TryRefreshAttackSlot` keeps hold).

### 5. `PaceClusterMergeService` (`Services/UnitOrders`, `ITickable`)
- Holds active clusters (registered by `UnitOrderService`, removed when member count = 0).
- Every `MERGE_INTERVAL` (1 s): for pairs of clusters **from the same order id**:
  - `centroidDist ≤ MERGE_FACTOR × (radiusA + radiusB) + MERGE_PADDING`
  - `dot(headingA, headingB) ≥ cos(MERGE_ANGLE)`
  - centroid segment route-compatible
  - speed compatible (`LINK_SPEED_RATIO`)
  - holds for `MERGE_HOLD_TIME` consecutive checks → merge (smaller into larger; members re-point via cluster forwarding, no ship callback).
- Hysteresis: merge threshold < link threshold < straggler threshold → no oscillation; no split besides detach.

### 6. Attack specifics
- Superseded post-kill direct Attack rule → [[Done/Bugs/Enemy_AI_Active_Play_And_Ship_Combat#Reopened scope outcome]]. The cluster behavior below records the original implementation.
- Target dies → each cluster uses its own `SharedReplacement` (search around cluster centroid, `REPLACEMENT_RADIUS`); `Attack` without replacement → attack-move to last position (existing); `AttackMove` → continue to original destination (existing).
- Different combat ranges: keep per-ship hold at own firing range (`HasReachedRange`); cluster pace only governs the approach. No shared final position.
- Cluster arriving first fights/holds immediately; no cross-cluster waiting (clusters are independent by construction).

### 7. Double right-click input + green feedback
- `PlayerOrderInputHandler.HandleCommand`: plain move branch → `_pointerInput.ClickCount >= 2` → `IssueMove(receivers, point, MovePace.Loose)`.
- `IPlayerUnitOrders.IssueMove(..., MovePace)` → `UnitOrderCommand.Pace` → `PlayerCommandSerialization` (network) → `AuthorityCommandSink` → `UnitOrderService`.
- `UnitOrder.IsLoose` → `OrderFeedbackMessage` (clients) → `UnitOrderFeedbackUiController` → `UnitOrderFeedbackUi.PlayMovement(screen, isLoose, completed)`.
- `UnitOrderFeedbackUi`: `[SerializeField] Color formationColor` (current blue), `looseColor` (green); tint `movementWave` + `movementEcho` before the sequence.
- Minimap move: formation only.

## Important Values (starting points; tune in `UnitOrderSettings`)
| Const | Value |
|---|---|
| `LINK_DISTANCE` | 600 |
| `LINK_RADIUS_FACTOR` | 4 |
| `LINK_SPEED_RATIO` | 2.5 |
| `LEAD_TOLERANCE` | 150 |
| `AHEAD_SLOWDOWN` | 0.8 |
| `STRAGGLER_SPEED_RATIO` | 0.6 |
| `STRAGGLER_DISTANCE` / `STRAGGLER_TIME` | 800 / 8 s |
| `STUCK_SPEED` / `STUCK_DETACH_TIME` | 1 / 5 s |
| `MERGE_INTERVAL` / `MERGE_HOLD_TIME` | 1 s / 2 checks |
| `MERGE_FACTOR` / `MERGE_PADDING` / `MERGE_ANGLE` | 1.0 / 100 / 30° |
- Reference: Venator `30`, Venator II `25`, ARC-170 `81`; `FleetCommandService.COMMAND_RADIUS = 400`; `REPLACEMENT_RADIUS = 1500`.

## Edge Cases
- Ships very far apart → separate clusters; never a global min.
- Fighters + dreadnought → linked only if close at issue; break away on engagement.
- Damaged engine below `STRAGGLER_SPEED_RATIO × issuePace` → excluded from pace → detached.
- Asteroid between nearby ships → blocked segment → no link.
- Selection change → no effect; clusters belong to the order.
- Stations → never join (no `IAttackFacade`).
- Ion-disabled / anchored → `PaceSpeed = +∞` (existing).
- Double-click on same point → `Matches` early-return must still drop pace.
- Online client → no pace; feedback colour still replicated.
- Load save → orders resume at own speed (unchanged).

## Tests (review/update; not run — local override)
- `GroupPaceTests` → `PaceClusterTests` (`Navigation`): straggler exclusion, mean remaining, merge forwarding.
- New `PaceClusterBuilderTests` (`Navigation`): far apart → 2 clusters; blocked segment → split; speed ratio; singleton → null.
- `UnitOrderServiceTests` (`BattleInput`): move/attack/attack-move clusters; loose move → no pace; selection change no effect.
- `ShipPursuitTests` / `NavigateStateTests` (`Navigation`): lag correction, stuck release, double-click drop.
- `UnitActionsPresenterTests`, `ShipAbilityServiceTests`, `ShipAbilitySymmetryTests` fakes: new signatures.
- Networking: `PlayerCommandSerialization` round-trip of `Pace` (`Networking`).
- UI: feedback colour wiring (`Ui`).
- No stat-pinning: assert relationships, not tuned values.

## Files
- `Assets/Scripts/Entities/BaseEntity/Orders/GroupPace.cs` → `PaceCluster.cs`, new `PaceClusterBuilder.cs`, `MovePace.cs`
- `Assets/Scripts/Services/UnitOrders/UnitOrderService.cs`, `UnitOrder.cs`, `PlayerUnitOrders.cs`, `IPlayerUnitOrders.cs`, `PlayerOrderInputHandler.cs`, new `PaceClusterMergeService.cs`, `UnitOrderSettings`
- `Assets/Scripts/Services/PlayerCommands/UnitOrderCommand.cs`, `AuthorityCommandSink.cs`
- `Assets/Scripts/Services/Networking/Messages/PlayerCommandSerialization.cs`, `OrderFeedbackMessage.cs`
- `Assets/Scripts/Entities/Ship/Orders/ShipOrderRunner.cs`, `Entities/Squadron/Squadron.cs`, move/attack-move facades
- `Assets/Scripts/Entities/UnitOrderFeedback/UnitOrderFeedbackUi.cs`, `UnitOrderFeedbackUiController.cs` + prefab colour fields

## TODO
- [x] Confirm decisions (2026-11-19: Move + Attack + AttackMove all use clusters; AI uses clusters; merge same-order only; squadrons join if near at issue, break away on engagement; lag correction accepted)
- [x] 1 `PaceCluster` + builder
- [x] 2 `UnitOrderService` clusters for Move / Attack / AttackMove
- [x] 3 Runner + squadron runtime (lag, straggler, stuck)
- [x] 4 Merge service
- [x] 5 Double-click loose move end-to-end (incl. network)
- [x] 6 Green feedback colour + prefab
- [x] 7 Test review/update
- [ ] User: in-game check (formation, loose, asteroids, damaged engine) → [[TODOs/User_Acceptance_Queue]]

## Outcome & Verification (2026-11-19)
- Replaced `GroupPace` with `PaceCluster`, `PaceClusterBuilder`, `PaceClusterTuning`, `PaceMemberCandidate`, and `MovePace`.
- Wired `UnitOrderService` spatial clustering across `Move` (formation), `Attack`, hard-point attack, and `AttackMove`, using `IMapObstacleContactProvider` + `ShipAvoidancePlanner.IsSegmentClear` for obstacle-split links and registering created clusters with `PaceClusterMergeService` (`ITickable`, registered in `SkirmishScope`).
- Updated `ShipOrderRunner` and `Squadron` runtime pacing with lag correction, straggler speed/distance detach, stuck release/detach, arriving/engagement release, and per-cluster `SharedReplacement` centroid search.
- Wired double right-click loose move (`IPointerInput.ClickCount >= 2`) through `PlayerOrderInputHandler` → `IPlayerUnitOrders` → `UnitOrderCommand.Pace` → `PlayerCommandSerialization` → `AuthorityCommandSink` → `UnitOrderService` → `UnitOrder.IsLoose` → `OrderFeedbackMessage.IsLoose` → `UnitOrderFeedbackUiController` → `UnitOrderFeedbackUi` (blue `formationColor` / green `looseColor` saved in `UnitOrderFeedbackUi.prefab`), with tuning defaults saved in `UnitOrderSettings.asset`.
- Verification: Unity full recompile succeeded (`0` errors across runtime, Editor, and test assemblies). Automated tests reviewed/updated and new cases authored (`PaceClusterTests`, `PaceClusterBuilderTests`, `UnitOrderFeedbackUiTests`, plus additions in `UnitOrderServiceTests`, `ShipPursuitTests`, `PlayerOrderInputHandlerTests`, `NetworkTickReplicationTests`); tests not run under local override.
- Guidance: no new canonical guidance needed; cluster parameters live in `UnitOrderSettings` and `PaceClusterTuning`.
