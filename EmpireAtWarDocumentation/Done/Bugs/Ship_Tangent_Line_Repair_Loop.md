---
category: Bugs
status: done
created: 2026-11-15
updated: 2026-11-15
completed: 2026-11-15
acceptance: pending-user
---
# Ship Tangent Line Repair Loop

## Goal
- Fix the verified code-3 insertion/removal loop without weakening clearance, bounds, winding or repair-budget checks.
- Add permanent regressions, run related Navigation tests and commit.

## Important Values
- XZ start `(-10,0)`, goal `(10,0)`; disks `(0.72,1.49,r2.81)` and `(-1.45,1.66,r1.74)`, plus mirrored Z.
- Current Unity job inserts small disk 1, inserts disk 0 before it, removes disk 0 for negative sweep (`-2.581336895 rad`), then repeats.
- Verified failure: code 3, `33/32` repairs, 34 stored circles; both mirrored direct-job inputs fail. Eight normal-builder checks (four headings × two mirrors) pass; gameplay reachability is not established by this probe.
- Prior arc fixes → [[Done/Bugs/Ship_Tangent_Arc_Repair_Loop]]. Verification artifacts: `Logs/ShipTangentRepairVerification/AfterArcFix/unity-results.json` and `remaining-line-loop.json` in its parent folder.

## TODO
- [x] Read current implementation and prior verification.
- [x] Add permanent mirrored and preseeded-corner regression coverage.
- [x] Correct obsolete-corner topology while retaining analytic clearance validation.
- [x] Run standard EditMode Navigation with explicit tests disabled; source-review changed code/tests together.
- [x] Extract reusable guidance and close implementation; task-only commit follows.

## Implementation
- `ShipTangentRouteJob` chooses the blocker winding against the original neighbors, then unlinks the next positive-radius internal corner if its tangent contact is covered by the blocker. Insert the blocker immediately and restart the normal tangent/clearance checks.
- Merely unlinking and restarting rediscovers the smaller blocker; selecting winding after unlinking can change the intended side. Keep that ordering.
- Use the same penetration tolerance as `FindBlocker`: radius minus `max(0.0001, radius × 0.00001)`. Endpoints and zero-radius corners remain fixed; budget, fallback severity and analytic collision checks are unchanged.
- A separate previous-corner removal branch is unnecessary: its outgoing contact belongs to the arc checked immediately before line repair. Production patch adds 12 lines; no movement/rotation changes or unrelated refactor.
- Reusable rule → [[Rules/BATTLE_PERFORMANCE_GUIDELINES#Ship pathfinding]]. Live battle follow-up remains in [[TODOs/User_Acceptance_Queue]] with the prior arc repair.

### Verification
- Baseline EditMode `ShipTangentRouteBuilderTests`: 25 total, 19 passed, 6 new cases failed with code 3. Empty/small-corner/both-corners inputs exceeded repair limits `32`, `36`, `40` respectively; both mirrors reproduced.
- Final standard EditMode `Navigation`: **280/280 passed**, 4.52 s; `include_explicit=false`. No exhaustive sweep or PlayMode battle run. Unity compilation completed without errors; source/test diff check and independent source review passed.
- New coverage: six mirrored direct jobs with 0/1/2 obstacle corners preseeded, fixed ≤4-repair bound, analytic disk clearance/bounds, sampled geometry/endpoints/continuity; four normal-builder controls across two headings and mirrors.
- Reviewed existing captured-battle, arc-repair, 270° winding, start-inside-clearance escape and rotation coverage; their contracts remain unchanged and all passed in Navigation.
- Evidence: `Logs/ShipTangentRepairVerification/AfterArcFix/{permanent-baseline-results.json,navigation-fixed-results.json}`. Direct-job failure is resolved; this does not establish an exhaustive proof for all obstacle layouts.

## Files
- `Assets/Scripts/Services/ShipNavigation/ShipTangentRouteJob.cs`.
- `Assets/Scripts/Tests/Editor/Navigation/ShipTangentRouteBuilderTests.cs`.
