---
category: Bugs
status: done
created: 2026-11-14
completed: 2026-11-14
acceptance: pending-user
---
# Enemy AI Battle Log Follow-up Fixes

## Goal

- Fix the failed log checks from the first battle after [[Done/Features/Enemy_AI_Patrol_And_Recovery_Relocation]] and [[Done/Bugs/Enemy_AI_Patrol_Recovery_Review_Fixes]].

## Evidence — `Loading_20261114_125624` (AI Player1 Rebellion, 3514 s, AI lost)

| # | Check | Result |
|---|---|---|
| 1 | Engage ↔ retreat flips | Pass: max 2 in 30 s (was 4 in 25 s); 31 `stateSwitchHeld` |
| 2 | Smoothed advantage | Pass: median raw−smoothed gap at switches 0.045 |
| 3 | Capture target flips | **Fail**: 0 fast `better` switches, but 178 target → none → target bounces < 5 s (176 `invalid:unsafe`); peak 151 switches/min |
| 4 | Patrol coverage | **Fail**: 32/36 sectors by 864 s mostly via fleet vision; `site:10` never seen |
| 5 | Patrol running | **Fail**: all 17 `patrolTarget` rows before 261 s; patrol ship released 7× in 190 s (`taskForce`) |
| 6 | Recovery order | Pass: 362 on → 413 anchor placement → 460 SecondStation → off |
| 7 | No old-home rebuild | Pass: replacement 391 from destroyed station, hostile 1468 away |
| 8–9 | Near/far, ally zones | Not testable (one AI, no allies) |
| 10 | Destroyed-spot avoidance | Pass: min 391 |

- Recovery latched 719 → 3244 s (`fleetWeak`) while the backup station stood; no free-placement purchases 847 → 3154 s (cause unconfirmed: latch vs build limits).
- Patrol/production-wait numbers used the system locale (`132,9`, `бесконечность`).
- DefendBase = 2080 s (60% of match).
- Analysis script: scratch `ai_checks.py` (not in repo).

## Decision

- **Capture unsafe grace:** current objective survives a `hostile:` safety failure for `UNSAFE_GRACE_SECONDS = 3`; obstacle/ion failures still drop at once. Grace resets when safe or on target change.
  - Why: raw route matchup dips for one tick as squadrons launch/die; commitment could not hold an "invalid" target.
  - Cost: real danger drops the target ≤ 3 s later; decision model then retreats.
- **Patrol ship:** `CanPatrol(difficulty, shipCount, stationCritical)` → Normal+, > 1 ship, station not critical. Fights, sieges, recovery no longer release it. Release reasons: `stationCritical`, `lastShip`.
  - Why: one corvette barely changes a fight; any-combat release disabled patrol for 92% of the match.
- **Recovery latch:** `canRebuild = !SecondStationLevelModel.IsBuilt`. Standing backup → recovery neither starts nor stays on (`trigger=backupStanding`).
  - Why: relocation has nothing to build; the latch only blocked patrol and free placement.
- **Logs:** `EnemyPatrolService.Log` takes `FormattableString` → invariant culture; both `production-wait` rows invariant (`money` now `F1`).

## Files

- `Assets/Scripts/Services/Enemy/EnemyStrategicContextBuilder.cs` — `UNSAFE_GRACE_SECONDS`, `_captureUnsafeSince`, `StationCritical`.
- `Assets/Scripts/Services/Enemy/EnemyPatrolShipController.cs`, `EnemyUnitCommander.cs` — patrol release rule.
- `Assets/Scripts/Entities/EnemyFaction/Models/EnemyRecoveryLatch.cs`, `Services/Enemy/EnemyRecoveryAnchor.cs` — `canRebuild`.
- `Assets/Scripts/Services/Enemy/EnemyPatrolService.cs`, `EnemyProductionStrategy.cs` — invariant logs.
- Tests: `EnemyRecoveryLatchTests` (+2), `EnemyPatrolMemoryTests` (CanPatrol cases), constructor updates in `EnemyRecoveryAnchorTests`, `EnemyUnitCommanderTests`.

## Verification

- Unity recompile 2026-11-14 12:00 UTC: completed, 0 errors (one unrelated existing CS0105 warning in `BattleSaveServiceTests`).
- Tests not run (local override). Battle not replayed.

## Known Gaps (not fixed)

- Smoothing lag: t3366 engaged at smoothed 1.38 vs raw 0.25. Option: faster drop time constant.
- Main station lost while backup stands: non-recovery placement still searches around `GetStationPosition(ProductionHost)` (old home).
- Other `EnemyAiTelemetry.RecordEvent` call sites (controller, counter-selection) still use current culture.
- Placement/scout threat-scan cost unmeasured → existing queue line in [[Done/Bugs/Enemy_AI_Patrol_Recovery_Review_Fixes]].

## Reusable Guidance

- AI telemetry details must be formatted with `FormattableString.Invariant` → added to [[Tooling/Battle_Telemetry]] Rules.
