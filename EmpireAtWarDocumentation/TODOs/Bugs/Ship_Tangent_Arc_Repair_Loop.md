---
category: Bugs
status: in-progress
created: 2026-11-15
updated: 2026-11-15
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
- [ ] Replay captured builder input and confirm baseline code-2 failure.
- [ ] Correct demonstrated repair defect; retain clearance, bounds, continuity and bounded work.
- [ ] Correct repair counter capture and verify actual job execution path.
- [ ] Add permanent regression coverage and run related `Navigation` tests (existing task authorization).
- [ ] Update canonical guidance and close implementation with battle acceptance queued.
