---
category: Bugs
status: done
created: 2026-10-31
completed: 2026-10-31
acceptance: pending-user
system:
  - ai
  - capture
  - navigation
---
# Enemy AI Recovery Review Fixes

## Goal

- Fix two source-confirmed recovery defects from the 2026-10-31 review of [[Done/Bugs/Enemy_AI_Defense_And_Zone_Recovery_Plan]].

## Implementation

### Retreat anchor rejects its own fleet reservations

- Cause: generic navigation clearance includes destination reservations. Compact retreat places a ship at the anchor; the next strategic evaluation rejected that reservation, including all radius `0` home samples.
- [x] Retreat strategic safety passes `includeReservations: false` to generic `IsPositionClear`; map obstacles, ion cost and known route/local hostile safety remain enforced.
- [x] Add `RetreatAnchor_RemainsSafeAfterRetreatReservesItsDestination`: real `UnitOrderService.IssueRetreat` → real navigation plan/reservation → next allied-relay anchor evaluation. Also asserts obstacle and ion-risk rejection.
- Formation placement still resolves reservations. Capture-area clearance keeps its existing reservation policy.

### Inactive capture objective consumes its stall timer

- Cause: candidate selection measured wall time before strategy evaluation, including defense, retreat, hold and rebuilding.
- [x] `EnemyUnitCommander` reports the executed capture objective after every decision; only a nonempty executed capture body enables tracking.
- [x] Accumulate active capture time; pause across other strategies. Approach/capture progress or objective replacement resets the stall budget.
- [x] Add elapsed-time regressions: 60 s inactive before capture; 8 s active + 62 s paused + 12 s active; genuine 20 s stall and 20 s retry cooldown.

## Rules

- Affected categories: `EnemyAi`, `Navigation`. Tests and Play Mode not run under the checkout override.
- Source inspection and scoped `git diff --check` passed; only a line-ending warning.
- Unity 6000.4.7f1: `recompile_status` reports `up_to_date`, `failed: false`, no errors. Reflection confirms the updated feedback method, reservation-policy signature and new regression are loaded; `isCompiling: false`, `scriptCompilationFailed: false`.
- No new captured Console errors after cursor `2480`. Serena still reports unresolved pre-existing new types; verified Unity compilation is the authoritative result.
- Reusable rules extracted → [[Architecture/Enemy_AI_Objectives]].
- User-only gates → [[TODOs/User_Acceptance_Queue]]: authored regressions on explicit request and battle recovery acceptance.

## Files

- `Assets/Scripts/Services/Enemy/EnemyStrategicContextBuilder.cs`; `EnemyUnitCommander.cs`.
- `Assets/Scripts/Services/ShipNavigation/ShipNavigationService.cs`.
- `Assets/Scripts/Tests/Editor/EnemyUnitCommanderTests.cs`; `ShipNavigationServiceTests.cs`.
