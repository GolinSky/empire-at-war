---
category: Bugs
status: done
created: 2026-11-15
updated: 2026-11-15
completed: 2026-11-15
acceptance: pending-user
---
# Ship Tangent Arc Repair Loop

## Goal
- Fix the captured normal-battle code-2 repair failure without changing working rotation or weakening route-clearance checks.
- Preserve regression evidence and correct failure counters under Burst execution.

## Important Values
- User confirms rotation works; normal battle still logs `FAILURE_ARC_REPAIR = 2`.
- Captured UTC `2026-11-15T13:25:24.1906491Z`: build `1696`, frame `78862`, service registration `77`, clearance `66`, height `-22`, speed `0`, turn radius `131.78028869628906`.
- Capture: 20 obstacle disks, 47 waypoints, 227 final circle records; incorrect `RepairCount/RepairLimit = 0/0` requires separate diagnostic correction.
- Preserved input: `Logs/ShipTangentRepairVerification/Battle1696/tangent-failure-0.json`; rolling source: `Application.persistentDataPath/ShipNavigationFailures/tangent-failure-0.json`.
- Prior small job-only loop and implemented rotation/winding fixes → [[Done/Bugs/Ship_Rotation_Snaps_And_Tangent_Failures]]. Do not assume the capture has the same mechanism until replayed.

## TODO
- [x] Preserve battle capture and read prior plan/backlog/acceptance.
- [x] Replay captured builder input and confirm baseline code-2 failure.
- [x] Correct demonstrated repair defects; retain clearance, bounds, continuity and bounded work.
- [x] Correct repair counter capture and verify actual job execution path.
- [x] Add permanent regression coverage; EditMode `Navigation` 270/270 passed, explicit tests disabled.
- [x] Update [[Rules/BATTLE_PERFORMANCE_GUIDELINES#Ship pathfinding]] and close implementation; live battle acceptance → [[TODOs/User_Acceptance_Queue]].

## Implementation
- Captured obstacle-18 principal tangent sweep `-3.003401 rad`; intended sweep `3.279784 rad` (`187.92°`), waypoint hint `3.102846 rad` (`177.78°`). The old `SweepHint > π` test wrongly removed the intended arc and initiated repeated repairs on the other side.
- Choose the negative sweep's winding nearest its hint: `sweep + π < SweepHint` → add `2π`. Legitimate 270° arcs remain supported.
- `RepairArc` keeps original-circle prefixes/suffixes only when their tangent contacts lie within the repaired arc. Preserve their sweep hints; replace obsolete segments instead of copying both sides unconditionally and recreating the same blocked arc.
- Failure counts return through `NativeReference<int2>` (repairs, final-pass limit). `RunByRef` does not guarantee scalar-field writeback from Unity's native runner; builder uses `.Run()` with native output. Captured replay now reports `113/112` before the repair fix rather than `0/0`.
- Repair budget, analytic blocker checks, fallback/error severity and Editor/Development-only snapshot guard remain. No movement/rotation code changed. These fixes cover demonstrated patterns; they do not claim completeness for arbitrary obstacle layouts.

### Verification
- Baseline quick EditMode `ShipTangent*`: 17 total, 14 passed, 3 failed (1.16 s). Exact battle replay fell back to the polyline; both mirrored two-disk removal cases returned code 2. Original tests and 270° case passed.
- Final EditMode `Navigation`: **270/270 passed**, 4.60 s, `include_explicit=false`. No exhaustive sweep or live battle run. Unity compilation completed without errors; independent source review found no actionable issues.
- Updated `ShipTangentRouteBuilderTests`: scheduled-job metrics, mirrored arc-removal loops, preserved 270° arc. New `ShipTangentCapturedGeometryTests`: full captured builder input; curves required, exact endpoints, tangent continuity, analytic bounds/clearance and sampled clearance.
- Full replay fixture: `Assets/Scripts/Tests/Editor/Navigation/ShipTangentBattle1696.json` retains original input, omitting only redundant final-circle output. Test results: `Logs/ShipTangentRepairVerification/Battle1696/{baseline-results.json,navigation-results.json}`.
- Diagnostics changes were included in concurrent workspace commit `256d621e0`; the subsequent repair/test changes are reviewed separately. Source and test diff checked together; unrelated changes excluded.

## Files
- `Assets/Scripts/Services/ShipNavigation/{ShipTangentRouteJob,ShipTangentRouteBuilder,ShipTangentFailureSnapshot}.cs`.
- `Assets/Scripts/Tests/Editor/Navigation/{ShipTangentRouteBuilderTests,ShipTangentCapturedGeometryTests}.cs`.
