---
category: Bugs
status: done
created: 2026-11-13
updated: 2026-11-13
completed: 2026-11-13
acceptance: pending-user
---
# Ship Route Curvature — Research

Source-verified 2026-11-13 (read only; no Play Mode, no tests). Plan → [[Done/Bugs/Ship_Route_Curvature_Plan]].

## Pre-implementation Pipeline
- `ShipNavigationService.PlanRoute` → up to 17 destination candidates (`DESTINATION_CANDIDATE_RING_COUNT` 2 × 8 + centre) → `ShipRoutePlanner.Build` per candidate.
- `ShipNavMesh.TryFindPath` → `NavMeshRouteJob` (Burst A*, portal midpoints) → `NavMeshFunnel` (SSF) → waypoints.
- `ShipRoutePlanner.Build` tries 3 routes: (1) course-keeping Bezier, (2) turn-in-place + Bezier, (3) polyline. Each must pass `ContainsRoute` (map bounds), `PreservesTravelCost` (hazards) and `IsRouteClear` (obstacle sampling).
- `ShipMovementTweenPlayer.TickRoute` follows by normalized arc length; turn-limited stepping.

## Verified Claims From the Earlier Dialog
| Claim | Verdict | Evidence |
|---|---|---|
| Smoothing all-or-nothing, 3 candidates, fallback to straight legs | ✅ | `ShipRoutePlanner.cs:75-114` |
| One handle scale `0.5` | ✅ | `HANDLE_SCALE` |
| Curve passes through every waypoint, tangent = `next − prev` | ✅ | `ShipBezierPath.BuildWaypointRoute` |
| `minimumTurnRadius` affects only the first handle | ✅ | `startHandle` uses it only for `i == 0` |
| Hazard rule `routeExtra ≤ originalExtra × 1.05 + 0.01` | ✅ | `NavigationHazardRaster.PreservesTravelCost` |
| No arrival-per-node stop, but progress held at sharp bends | ✅ | `isTurnLimited` in `TickRoute` |

## Additional Findings
- **Waypoint Bezier corners are tighter than the ship can turn.** Handles = `0.5 × 0.3 = 0.15 × leg`. Corner radius ≈ handle length, unrelated to `v / ω`. Even a "successful" curve forces the corner speed limit → visible slowdown. Smoothing success ≠ no slowdown.
- **Polyline fallback = tangent discontinuity at every waypoint.** `GetCornerSpeedLimit` → `distance / (turn / rate)` → ~0 near the corner; floor `MINIMUM_SPEED_FACTOR` 0.1, then `isTurnLimited` holds position → stop-and-turn at each graph corner. This is the reported "slows between graph nodes".
- **Candidate (2) and (3) start with turn-in-place** (`TurnDuration > 0` → `_currentSpeed = 0`). Full stop at route start.
- **Hazard maps multiply corners.** `NavMeshFunnel.AppendLeg` / `UsePortalMidpoints` insert portal midpoints when a shortcut raises hazard cost → zig-zag waypoints → smoothing more likely rejected → polyline.
- **Clearance validated twice.** Graph disks are already `radius + clearance` (`ShipNavMesh.BuildMesh`); `IsRouteClear` re-samples the same `_mapObstacleContacts`. Rejection is the only result of the second check.
- **CPU waste per candidate:** up to 3 `ShipBezierRoute` builds (24 arc-length samples + 12 display samples per segment), contact sampling, hazard re-sampling. Follower per ship per tick: 8-step binary search + 8 corner samples.
- **Funnel corners lie on disks.** `NavMeshPointsJob` samples each inflated disk at `R/cos(π/n)`; walkable triangles never touch an inflated disk. Each funnel corner is therefore a sample of a known circle (or a map-edge / hazard-boundary point).

## Online Sync Facts
- Host-authoritative; clients do not plan. `HostShipRouteSender` sends `ShipRouteMessage { Vector3[] Route }` (display samples) only to the owner.
- Motion: `DeadReckoningSender` + `EntityMotion.Extrapolate` = **constant speed + constant turn rate (circular arc)**. Keyframe when error > `POSITION_ERROR_THRESHOLD` 2 or `HEADING_ERROR_THRESHOLD` 5°, else refresh 1 s moving / 3 s idle. Tick 20 Hz.
- Bezier curvature varies continuously → prediction drifts → extra keyframes. Turn-in-place / sharp corners → heading error bursts.
- Line + arc pieces have constant turn rate → extrapolation matches inside a piece up to quantization (`EntityKeyframeCodec`: `TurnRate` whole °/s `sbyte`, speed step 0.5); keyframes mainly at piece changes and during speed ramps.

## Alternatives Evaluated
| Option | Produces usable result first time | CPU | Sync fit | Verdict |
|---|---|---|---|---|
| Tune Bezier (more handle scales, per-corner repair) | ❌ still generate→reject | ↑ | Poor (varying curvature) | Reject |
| Safe-corridor Bezier (control points in convex free cells) | Containment yes; continuity needs solver | QP per route | Poor | Reject |
| State lattice / motion primitives | ✅ | Search states × headings × speeds; many ships replan several Hz | Good | Reject for RTS scale |
| Hybrid A* | ✅ mostly | Heavy, per-ship | Good | Reject |
| **Tangent-arc taut string around inflated disks + speed profile** | ✅ by construction | O(corners) closed form, Burst | **Exact match to dead-reckoning arcs** | **Chosen** |

## Sources
- Kallmann, "Navigation Queries from Triangular Meshes" / clearance-aware funnel (arcs around corners).
- Demyen & Buro, "Efficient Triangulation-Based Pathfinding" (TA*, radius funnel).
- Dubins paths (CSC: arc-line-arc tangent construction) for start heading.
- Mononen, Simple Stupid Funnel (current funnel basis).

## Implementation
- Implemented 2026-11-13. Outcome, corrected geometric assumptions and verification gates → [[Done/Bugs/Ship_Route_Curvature_Plan]].
- The original clearance argument was incomplete: non-corner disks may intersect tightened arcs; portal width alone cannot prove fillet clearance. Production now checks arcs and lines and limits fillet circles analytically.
- Compilation/source checks passed. Tests, archetype sweeps, runtime CPU comparison and keyframes per moving ship were not executed; no performance improvement claimed.
