---
category: Bugs
status: done
created: 2026-11-15
updated: 2026-11-15
finding_status: resolved
tags:
  - Ship
completed: 2026-11-15
acceptance: pending-user
---
# Ship Rotation Snaps and Tangent Failures

## Goal
- Remove sudden battle ship yaw changes, especially during rerouting.
- Resolve tangent repair failures without hiding errors or weakening route-clearance checks.
- Implementation and related tests authorized and completed on 2026-11-15; battle/online visual acceptance remains pending.

## Important Values
- Historical investigation below describes pre-fix HEAD `3d084c176`; implementation outcome below supersedes its proposed fixes.
- User report: ships sometimes rotate instantly during battle, especially rerouting; four tangent errors and a related message now reported as a warning.
- Current `C:/Users/golin/AppData/Local/Unity/Editor/Editor.log`: four code-3 errors at lines `39791`, `39814`, `39853`, `39908`; three code-1 warnings at `35831`, `35870`, `36179` (rg line numbering, matching supplied excerpt).
- Message: `[ShipNavigation] Tangent route build failed (code 3); following the funnel polyline with turns in place.`
- Source inspected at HEAD `3d084c176a63bac6de6c7bc0e730295f679f9414`, with existing unrelated working-tree changes. No gameplay code/assets modified.

## Implementation

### Completed implementation · 2026-11-15
- `ShipMoveComponent.Plan` supplies actual hull forward to route geometry and tests actual initial-heading mismatch before braking, independently of reported `TurnDuration`.
- `ShipMovementTweenPlayer` retains bounded yaw recovery beyond speed-cap crossings. Recovery brakes on checked geometry, aligns at rest when necessary, rebuilds the remaining profile and resumes exact tangent following.
- Braking/recovery stops at the next sharp corner. Insufficient stopping distance → clamp at that checked corner; do not cross it with momentum or snap to outgoing yaw. Turning consumes its own tick.
- Deterministic code-3 reproducer: XZ start `(-10,0)`, goal `(10,0)`, disks `(-2,-1.65,r1.66)` and `(-1.5,2.25,r2.49)`, plus mirrored Z. Preferred opposite windings between overlapping disks caused alternating insertion.
- `ShipTangentRouteJob` tries the opposite insertion winding only when the preferred winding lacks adjoining tangents; accepts it only if both tangents exist. Clearance checks, shared repair budget, fallback and log severity remain unchanged.
- Failure capture: four rotating JSON files in `Application.persistentDataPath/ShipNavigationFailures`; full disks/bounds, waypoint corner tags, actual forward, turn radius, escape/collapse options, final circles, frame/build/service-registration ID, current speed/clearance/height and final-pass repair count/limit. Capture allocation/serialization occurs only on failure; failed collapsed attempts are captured too.
- Capture decision: retain full replay inputs and final links instead of recording every repair operation on successful jobs. Ordered insertion/removal trace requires replay/debugging; no replay-loader UI. Registration ID is service-local, not network identity; prior route tangent is not recorded. Four files bound retention count, not geometry-dependent file size.
- Baseline Unity regression: 10 existing tangent cases passed; both new mirrored cases failed with code 3. Post-fix EditMode `Navigation`: **260/260 passed**, 3.70 s; `Ships`: **209/209 passed**, 33.01 s. Both used `include_explicit=false`; no exhaustive or PlayMode run.
- Added/updated `ShipRotationKinematicsTests`, `ShipNavigationServiceTests`, `ShipTangentRouteBuilderTests`, `ShipTangentFailureSnapshotTests`. Reviewed unchanged `ShipRouteTests`, `ShipArchetypeRouteSweepTests`, `ShipEngineHardpointTests`, `TitanShipTests`: geometry/speed/interface contracts remain valid.
- Compilation completed with zero errors; loaded diagnostic/job fields verified. Initial stale-assembly run was invalidated after correcting a new test namespace collision. Service test setup corrected for map-edge clamping and actual transform-vector equality; production behavior was not weakened.
- Reusable guidance → [[Rules/BATTLE_PERFORMANCE_GUIDELINES#Ship pathfinding]]. Original four battle errors are not attributed to the deterministic geometry; correlate any recurrence with captures during user acceptance.
- Existing movement classes exceed 200 lines; focused playback-state extraction may help later. No unrelated refactor performed.

### Historical investigation

### Verified failure meaning
- `ShipTangentRouteJob.cs:16,118–125`: code 3 = `FAILURE_LINE_REPAIR`; a tangent line still intersects an obstacle disk when the shared repair counter exceeds its limit.
- Repair limit at line 50: `(Disks.Length + Circles.Length) * 4 + 16`, calculated on entry to `RepairTangents`; arc and line repairs share the counter. Code 3 identifies the branch that exhausted it, not necessarily a sequence containing only line repairs.
- `ShipTangentRouteJob.cs:68–80`: code 1 = `FAILURE_NO_TANGENT`. A failed heading-constrained start circle first becomes a zero-radius start; unresolved tangent failure then returns code 1.
- `ShipRouteGeometry.cs:17–28`: rejects coincident centres (`distance <= 0.0001`) or impossible signed-radius separation (`distance < abs(a.Sign*a.Radius - b.Sign*b.Radius) - 0.0001`).
- `ShipTangentRouteBuilder.cs:123–132,164–178`: failures return the funnel polyline. Its straight legs have heading discontinuities; successful fallback still needs correct stop/turn playback.
- Warning change is intentional: commit `88a4abe35ef3e8bb719fe1e28231dac8e161446b` (2026-11-14) changed code 1 to `Debug.LogWarning`; code 3 remains `Debug.LogError`. Lower severity did not remove the fallback.

### Source-level root cause: heading continuity is conditional
- `ShipMovementTweenPlayer.TickRoute`, lines 255–267: above the current piece speed limit → `StepRotation`; otherwise → immediate `Quaternion.LookRotation(routeTangent)`.
- The switch tests speed only. Residual hull-to-tangent heading error is discarded when speed reaches the limit, or when entering a straight piece with a higher limit. This permits a one-frame catch-up rotation after an overspeed reroute.
- `ShipMoveComponent.Plan`, lines 399–404: planning forward is `_motion.CurrentPathTangent ?? transform.forward`. During gradual overspeed turning, route tangent and actual hull forward intentionally differ.
- `ShipRoutePlanner.Build`, lines 65–68: `TurnDuration` is measured from that supplied forward. `ShipMovementTweenPlayer.PlayPath`, lines 160–161, requires both positive `TurnDuration` and actual heading mismatch before turning in place.
- Consequence: a route starting along the old route tangent can have `TurnDuration == 0` despite a misaligned hull. If its first piece is no longer overspeed, playback directly assigns the new tangent.
- Root contract gap: exact-tangent playback assumes heading continuity, but overspeed recovery and route replacement do not enforce it. This can cause snaps even without a tangent-builder error.

### Connection to fallback routes
1. Queued planning: `ShipNavigationService.LateTick` → `ShipMoveComponent.ExecuteQueuedPlan` → `Plan` → `ShipRoutePlanner.Build` → tangent builder.
2. Tangent failure → straight funnel legs with sharp corners; `ShipRoute.RequiresTurnAtPiece` detects breaks greater than `1°`.
3. Normal playback checks `_nextTurnPiece`, stops at its start distance and turns gradually; fallback alone does not imply an instant rotation.
4. `TickRoute`, lines 224–229: `BrakeForReplan` instead integrates distance directly, clamping only to route end. It bypasses the normal next-corner handling at lines 235–243.
5. If braking distance crosses a sharp corner, `ShipRoute.EvaluateDistance` advances to the next leg. The direct heading assignment can then snap to that leg. This is an additional source-level gap, conditional on reaching the corner with momentum; a feasible profile with enough stopping distance may stop before it.
- Shared mechanism: discontinuous or lagged heading enters playback that assumes exact alignment. Tangent failures increase exposure to corner transitions; rerouting exposes braking and route-handoff transitions.

### Evidence limits
- Confirmed: failure counts/severity, exhausted repair budget, fallback construction, and the source paths that permit yaw discontinuity.
- Not established: which logged request belongs to a visible snap, the affected ship/map seed, or the exact obstacle geometry that exhausted repairs.
- Log messages lack ship ID, frame, route/request ID, actual heading, speed, waypoints, disks and repair sequence. Four errors are four build failures, not proof of four distinct ships or orders; destination candidate planning can build multiple routes.
- Repeated obstacle insertion/removal or turn-side oscillation is a hypothesis. `RepairTangents` removes nonpositive-sweep corners and inserts blocking disks; no captured trace proves a particular cycle here.
- Serena launcher reported `AlreadyRunning`, but HTTP connection failed; service log contains socket `WinError 64`. Investigation used Graphify and direct live-source reads. No service configuration changed.

### Verification pass (2026-11-15)
- Re-read every cited line at HEAD `3d084c176`; all line numbers, constants and behaviour claims match. Log still has 4× code 3, 3× code 1. Commit `88a4abe35` touched builder/job/tests as stated.
- Refinement — handoff gap is wider: the stale `CurrentPathTangent` also feeds `ShipTangentRouteBuilder.Build` (`ShipRoutePlanner.cs:64`), so the start circle's geometry is built from the old tangent, not just `TurnDuration`. `TurnDuration == 0` also skips the brake-then-replan branch (`ShipMoveComponent.cs:424–430`).
- Refinement — repair budget: `RepairTangents` can run twice (`ShipTangentRouteJob.cs:39–40`), counter resets each pass. `InsertAfter` appends and removal only unlinks, so `Circles.Length` includes dead circles and the second-pass limit is larger.
- Refinement — oscillation is structurally possible: nonpositive-sweep removal (`:88–96`) is not counted and does not mark the disk; nothing stops line repair (`:131`) re-inserting the same blocker. The comment "A removed corner is never revisited" is not enforced. Still unproven for these logs.
- Builder retries without link collapse (`ShipTangentRouteBuilder.cs:22–23`); a failure in the collapsed attempt returns `null` silently, so one logged error can represent two job runs.
- Minor: `_isTurningToRoute` exits within `ROUTE_HEADING_TOLERANCE = 1°`, then exact assignment snaps ≤1° — negligible.
- Fix 2 must pass actual hull forward to the tangent builder's start circle as well as the turn-duration check.

## Decision
- **Implemented:** preserve exact tangent following for aligned, feasible continuous routes; explicitly handle recovery and handoff heading errors.
- **Why:** current design deliberately uses exact tangent heading for predictable motion. Changing every route tick to gradual chasing would alter that design and can produce persistent heading lag.
- **Avoid:** merely downgrading code 3, increasing repair limits without a reproducer, or claiming the fallback itself proves the visual snap.

### Original proposed solutions, in priority order
1. Fix overspeed recovery: retain a bounded heading-recovery state until the hull aligns. Do not switch to exact heading based only on speed. Coordinate speed/progress if needed to let the hull catch the tangent.
2. Fix route handoff: distinguish geometric continuation tangent from actual hull forward. Decide initial alignment using the actual heading independently of `TurnDuration`; preserve controlled braking before an incompatible turn.
3. Fix braking at tangent breaks: respect the next discontinuity and stop before crossing it, then turn or replan from the actual stopped pose. Define the short-stopping-distance case explicitly; do not snap yaw or leave checked geometry.
4. Capture tangent failure inputs: request/ship/frame IDs, start/goal, actual forward and route tangent, speed, clearance/layer, waypoints/tags, disks/circles, repair count/limit and inserted/removed blocker sequence. Keep failure output bounded and reproducible.
5. Reproduce code 3 from captured geometry; correct the demonstrated insertion/removal, winding or tolerance defect. Add progress/cycle detection if the trace supports it; keep bounded work and a safe fallback.

## TODO
- [x] Investigate supplied symptoms, current log, failure codes, rotation path and severity history.
- [x] Review relevant existing test source; document missing coverage.
- [x] Implement heading recovery, handoff and corner-safe braking corrections.
- [x] Reproduce code-3 geometry, fix its specific repair defect and capture future failures for replay; historical-log attribution remains an acceptance check.
- [x] Add regression coverage; user-authorized EditMode `Navigation` 260/260 and `Ships` 209/209 passed.
- [ ] User acceptance → [[TODOs/User_Acceptance_Queue]]: repeated reroutes near obstacles, overspeed tight arcs, fallback corners and online client behavior; correlate captures with any remaining snaps.

### Historical verification proposal and reviewed coverage
- `ShipRotationKinematicsTests.ReplanOntoTightArc_BrakesFromCurrentSpeedWithoutSnappingHeading` checks only the first overspeed tick. Extend through speed-limit crossing and arc→line transition; assert per-frame yaw bounds throughout.
- `BrakeForReplan_ContinuesAlongRouteUntilMomentumIsGone` uses one straight leg. Add sharp fallback corners, short stopping distances and braking during a turn.
- Add a replan with hull heading different from `CurrentPathTangent`, including a planner-reported zero turn duration.
- Preserve `ArcRoute_FollowsTangentWithoutCornerStops`: feasible aligned arcs should retain exact tangent behavior.
- Reviewed `ShipTangentRouteBuilderTests`: duplicate-circle signs, overlapping walls, arc blocker, map edge, turnaround and start-turn direction. Add captured failure geometry rather than guessing a seed.
- `ShipArchetypeRouteSweepTests.cs:135–170` accepts fallback log messages and checks zero profile speed at tangent breaks. It does not verify frame-by-frame hull yaw; retain its geometry assertions and add separate playback coverage.
- Initial investigation did not run tests or Play Mode. The completed implementation and authorized test results are recorded above; runtime visual acceptance remains pending.

## Files
- Movement files: `Assets/Scripts/Components/Ship/Movement/{ShipMoveComponent,ShipMovementTweenPlayer,ShipRoute,ShipRouteSpeedProfile}.cs`.
- Planner files: `Assets/Scripts/Services/ShipNavigation/{ShipRoutePlanner,ShipNavigationService,ShipTangentRouteBuilder,ShipTangentRouteJob,ShipRouteGeometry}.cs`.
- Tests: `Assets/Scripts/Tests/Editor/Navigation/{ShipRotationKinematicsTests,ShipTangentRouteBuilderTests,ShipArchetypeRouteSweepTests,ShipRouteTests,ShipNavigationServiceTests}.cs`.
- Prior implementation/accepted exact-tangent design → [[Done/Bugs/Ship_Route_Curvature_Plan]]; historical research → [[Done/Bugs/Ship_Route_Curvature_Plan - Research]].
- This narrow bug follow-up owns the new failures; the historical implementation record remains unchanged.
