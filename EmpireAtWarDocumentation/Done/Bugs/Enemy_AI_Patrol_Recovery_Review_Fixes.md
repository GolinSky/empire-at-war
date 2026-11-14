---
category: Bugs
status: done
created: 2026-11-14
completed: 2026-11-14
acceptance: pending-user
---
# Enemy AI Patrol and Recovery Review Fixes

## Goal

- Restore attacked-site priority; separate backup construction from abandoning a defendable station.
- Restrict pressure bypass to entering station defense; standardize recovery telemetry.

## Decision

- User confirmed 2026-11-14: defend a living station while rebuilding unless smoothed local advantage falls below difficulty RetreatAdvantage (0.7 Medium/Hard; 0.8 Easy/UltraHard). Retain retreat exit hysteresis (0.875 Medium/Hard; 1.0 Easy/UltraHard) after withdrawal.
- Threatened owned site preempts a normal capture immediately; safety, reservation and stall filters still apply.
- Placement search performance is a source-confirmed risk, not a measured regression; profile a large blocked map before choosing an optimization.

## TODO

- [x] Implement and source-review fixes; author EnemyAi regressions.
- [x] Verify Unity compilation; do not run tests under local override.
- [x] Update canonical guidance and acceptance queue.

## Files

- [[Done/Features/Enemy_AI_Patrol_And_Recovery_Relocation]]
- [[Architecture/Enemy_AI_Objectives]]
- [[Tooling/Battle_Telemetry]]

## Implementation

- Immediate threatened-site preemption; recovery withdrawal gated by actual local disadvantage with exit hysteresis; reinforcements follow actual station defense.
- Station pressure bypass applies only to entering DefendBase. Recovery event fields use invariant culture and semicolon separators.
- Regression coverage authored for threatened-site distance/age/reservation/stall, parity defense, weak withdrawal/exit, pressure-held capture and recovery reinforcement rally.
- Verification: Unity compilation completed without script errors; error console empty; scoped diff and source review passed. Tests and Play Mode not run.
- Guidance updated: [[Architecture/Enemy_AI_Objectives]], [[Tooling/Battle_Telemetry]], parent [[Done/Features/Enemy_AI_Patrol_And_Recovery_Relocation]].
- Remaining user gates: battle behavior/log acceptance, EnemyAi tests on explicit request, and large blocked-map placement/scouting profiling. No measured performance improvement claimed.
