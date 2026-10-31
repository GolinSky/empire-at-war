---
category: Bugs
status: done
created: 2026-10-31
completed: 2026-10-31
acceptance: pending-user
system:
  - ai
  - capture
---
# Enemy AI Independent Task Groups

## Goal

- Retreat only threatened ship groups; keep a small safe capture detachment expanding elsewhere.
- Distribute available squadrons across capture targets while preserving scout, escort and carrier guard roles.

## Implementation

- [x] Stable fastest eligible one-ship capture detachment when more than one ship exists and the route is safe; release when no target remains and recovery is complete.
- [x] Spatial combat groups with independent local force, decisions, retreat/recovery and capture progress; per-pass capture objective reservations.
- [x] Squadron assignments cover threatened sites, retain valid targets and spread remaining squadrons by coverage then distance; special roles remain excluded.
- [x] Preserve Hold/Rebuild state-entry order gating and existing uncommitted AI work.
- [x] Extract reusable behavior to [[Architecture/Enemy_AI_Objectives]].

## Important Values

- Combat group join radius: 800 units; split hysteresis: 1600 units from anchor.
- Existing difficulty retreat thresholds unchanged; checks use the group's origin and ships.
- Telemetry: primary combat strategy/context; aggregate active/body/rallying counts; all-group action summary.

## Verification

- Unity 6000.4.7f1 recompile: completed, failed=false, errors=[]; new scripts imported and four .meta files generated.
- Unity Console: no new errors after baseline cursor 2704 (post-compile cursor 2707).
- Source review: group ownership, force isolation, capture reservations, recovery, release/merge and default-zone movement.
- git diff --check passed for changed code (existing line-ending conversion warnings only).
- Authored 11 EnemyAi regression cases: 4 ship/model cases and 7 squadron allocation cases. Tests not run under the local override; no Play Mode.
- User-only runtime/test gates → [[TODOs/User_Acceptance_Queue]].

## Files

- `Assets/Scripts/Services/Enemy/EnemyShipTaskGroup.cs`; `EnemyShipTaskGroupPlanner.cs`; `EnemyUnitCommander.cs`; `EnemyStrategicContextBuilder.cs`.
- `Assets/Scripts/Services/Enemy/EnemySquadronCommander.cs`; `EnemySquadronCaptureAssignments.cs`.
- `Assets/Scripts/Entities/EnemyFaction/Models/EnemyStrategicDecisionModel.cs`.
- `Assets/Scripts/Tests/Editor/EnemyUnitCommanderTests.cs`; `EnemyStrategicDecisionModelTests.cs`; `EnemySquadronCaptureAssignmentsTests.cs`.
