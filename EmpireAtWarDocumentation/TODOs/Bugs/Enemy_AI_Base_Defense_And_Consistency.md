---
category: Bugs
status: todo
created: 2026-11-18
---
# Enemy AI: Weak Base Defense and Inconsistent Difficulty

## Goal
- UltraHard AI does not collapse when players hit its base mid-fight.
- Same difficulty gives similar challenge across battles.
- Parent: [[TODOs/Bugs/Enemy_AI_And_Faction_Balance]].

## Evidence (online 2026-11-17, all AI `UltraHard`)
- Summary: `Logs/OnlineBattles/20261117T1650Z/ai_summary.txt` (`scripts/ai_summary.py`).
- State mix varies a lot between AIs in one battle (battle 07): Separatist P3 `DefendBase` 48%, Republic P4 0% `DefendBase` and ship spend 105k vs 310k.
- Ship spend per AI: 105k–595k; mining builds 5–47 per AI → income differs ×3+.
- Superweapon purchases (log has no player id): 218k (05), 510k (06), 655k (07), 650k (08); fired mostly at Capital/BattleCruiser.
- With `BaseThreat ≥ 1`, AIs sometimes stay `RetreatValue` 1–3 decisions (P3 in 05, 06, 08).
- No battle time in `[EnemyAI]` lines → reaction delays cannot be measured from release logs.

## Causes (source)
1. **UltraHard has the highest bar to defend.** `EnemyAiDifficultyProfile.defenseThreatRatio`: Easy 0.75, Normal 1, Hard 1.25, **UltraHard 1.5**. `DefendBase` needs `BaseThreatRatio = 1 / Advantage(wholeOwnFleet, attackers) ≥ 1.5` unless `HasStationPressure`.
   - A strike group weaker than the AI's whole fleet is ignored until the station takes sustained damage (`EnemyStationPressureTracker`: 0.5% loss, 3 s sustained).
2. **All-or-nothing response.** UltraHard `committedFleetRatio` 1 → once `DefendBase` wins, the whole fleet leaves the mid-map fight → loses that fight and arrives piecemeal (slow capital turns, `BrakeForReplan` on re-orders).
3. **Threat only from visible attackers** (`BuildBaseThreat` skips `!_vision.IsVisible`); undetected attackers give ratio 0 until damage latches pressure.
4. **Comparison uses the whole fleet**, not the forces that can arrive in time.
5. Variance: faction economy + mining count + superweapon spending + team picture differ per battle; FleetAdvantage-driven `HuntFleet`/`RetreatValue` flips change tempo.
- "lvl 3" in the report: unclear whether this is station level 3 or a difficulty → ask user.
6. **Save-for-level stalls ship production (Argon, code path confirmed).** `EnemyProductionDecisionModel`: when `ShouldSaveForLevel` (`CanOrderShips && IsLevelDue`) the UltraHard path returns `None` instead of `Ship` (line ~292). UltraHard `shipOrdersPerLevel` = 1 → a level becomes due after very few ship orders → the AI saves while players attack. Impact on income/fleet not measured.
7. Argon (not yet verified): ally help only when `IsStationCritical` (< 50% HP or < 120 s to loss); `TeamAiCooperation` assigns at most 1 ally; `EnemyTaskForceExecutor` stops attacking fleets below 75% cohesion.

## Plan
- [x] Release telemetry (2026-11-18): `BattleTime=` (static `BattleClock.CurrentElapsed`) on `[EnemyAI]`, `[EnemyAI:Production]`, `[EnemyAI:SuperWeapon]`; superweapon lines also carry `Player=` and `SpentShare`. Shared with [[TODOs/Optimization/Online_Battle_Low_FPS]] Phase 0.
- [x] Invert/realign `defenseThreatRatio` (harder AI defends earlier): Easy 1.5, Normal 1.25, Hard 1, UltraHard 0.75 (2026-11-18). Local-force comparison not done.
- [x] Proportional response (2026-11-18): `DefendBase` commits `clamp(BaseThreatRatio × 1.5, 0.34, 1)` of ships; whole fleet only when `IsStationCritical`. Nearest ships defend; the rest keep their orders (no `StopRemaining`). `EnemyShipTaskGroup` no longer forces all ships for `DefendBase`.
- [x] Station damage reveals attackers (2026-11-18): within 5 s of station damage, hostiles with `IsAttacking(ownBase)` are reported to team intel and pass the vision check in `BuildBaseThreat`.
- [x] Superweapon spend cap (2026-11-18): `_spent + price ≤ 0.2 × EconomyModel.TotalEarned` (new property); tunable `MAX_SPEND_SHARE`.
- [ ] Replay (user plays): 2 humans attack fleet, 1 human raids base at minute ~8; compare AI losses before/after using the new `BattleTime=` logs. Plan stays active until then.

## Review 2026-11-18 (commits `026a1a48f`, `136268bb2`)
- Good: harder AI defends earlier (relationship test), detachment sized to the raid with a full response when critical, battle time + player id in AI logs, superweapon spend share.
- [x] (fixed 2026-11-18: `ReleaseUncommittedDefenders` — ships outside the detachment keep an Attack/AttackMove/Hunt fight; former defenders and stale non-combat orders are stopped) **Uncommitted remainder is unmanaged:** `EnemyTaskForceExecutor` no longer calls `StopRemaining`; ships beyond `CommittedShipCount` get no orders while the group is in `DefendBase`. When their current target dies they idle mid-map until the state changes. Split the defenders into their own task group (or keep running the previous state for the rest).
- [x] (fixed: `IsCloaked()` skip) **Cloaked attackers revealed:** `RevealStationAttackers` reports attackers to team intel without an `IsCloaked()` check; other intel consumers can then see cloaked units. Skip cloaked units.
- [x] (fixed: `EconomyModel.Refund` returns money without counting it as earnings; used by `EconomyService.Refund` and the superweapon refund. Save/load reset of `TotalEarned`/`_spent` remains, accepted.) Superweapon share: refunds go through `EconomyModel.AddMoney` → `TotalEarned` grows **and** `_spent` drops (double credit); production-cancel refunds also count as earnings. `TotalEarned`/`_spent` are not saved (reset on load). Count only income, not refunds.
- [x] (reused list + cached comparison) Minor: the defense path allocates and sorts a new ship list every tick; nearest ships are chosen regardless of strength.
- Note: `BattleClock.CurrentElapsed` is static global state, acceptable for log lines only.

## Playtest 2026-11-18 (Editor, 4 players, 32 min; `Logs/Playtests/20261118T1013Z/`)
- **Order churn:** AI issued 13,991 orders and cancelled 13,064 (Player1 197/min, Player3 153/min, Player2 75/min; human 11/min). Every re-issue cancels the order and rebuilds the ship's route.
- **Patrol corvette flip-flop (the "ships go in random directions" report):**
  - Patrol frigate 209 (Player1) got 3,292 AttackMove orders, 3,139 of them < 0.6 s apart. Destination alternated between its patrol point and the fleet's point (e.g. (3944, 964) → (1210, −3102) → back).
  - `EnemyPatrolShipController.Select` issued `IssueAttackMove` on every decision with a new `AttackMoveEngagement`, so `ShipOrderRunner.AttackMoveTo`'s repeat check (same engagement instance) never matched.
  - Patrol log: 470 `assigned` / 465 `released;reason=noSafeTarget` → after each release the fleet ordered the ship elsewhere, and the next decision took it back.
  - [x] Fixed 2026-11-18: no repeat AttackMove while the ship already moves to the same patrol target; after `noSafeTarget` the corvette stays with the fleet for `NO_TARGET_RETRY_SECONDS` (10 s). Recompile clean; tests not run.
  - [ ] Test gap: `EnemyPatrolShipController` re-issue/back-off (needs a patrol-service fake; `TryGetTarget` is on the concrete `EnemyPatrolService`).
- Human ships (Player0): wrong-way travel in 1.4% of moving samples (normal route turns); AI 1.1–2.2%.
- [ ] Remaining churn: other AI paths also re-issue identical AttackMoves with new engagements (`EnemyTaskForceExecutor` capture/marker/screen calls). Consider an `IssueAttackMove` overload that keeps the ship's current engagement when the destination is unchanged.

## Agent handoff
- Read this plan, its linked plans and the live source first; source wins over this note.
- Follow `AGENTS.md`: no Play Mode, no tests (local override). Update/add the tests listed here and report them as not run.
- Unity assets: change them through Unity tooling, re-serialize the changed paths, check the console.
- Hand back: changed files, checked-off steps, deviations and open questions, then ask the user for review.

## Tests (not run — local override)
- `EnemyAi`: `EnemyStrategicDecisionModel` tests (defense rule thresholds), defense strategy tests. Assert relationships (harder ≥ earlier defense), not tuned numbers.
- Added/updated (not run): `EnemyStrategicDecisionModelTests` (detachment vs critical, larger raid → larger detachment, harder defends earlier); `PlayerSaveTests.Enemy_DoesNotSpendMoreThanItsShareOfEarningsOnSuperWeapons`; save test economy now earns extra so the cap allows its purchase.
- Compile check: Unity recompile clean, no console errors.
- No unit test for the attacker reveal (needs a full world snapshot).

## Files
- `Entities/EnemyFaction/Models/EnemyAiDifficultyProfile.cs`, `EnemyStrategicDecisionModel.cs`, `EnemyDefenseStrategy.cs`
- `Services/Enemy/EnemyStrategicContextBuilder.cs` (`BuildBaseThreat`), `EnemyStationPressureTracker.cs`, `EnemySuperWeaponController.cs`, `EnemyAiTelemetry.cs`
