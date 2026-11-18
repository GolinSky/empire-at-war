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
- [ ] Release telemetry: battle time + player id on `[EnemyAI]`, `[EnemyAI:Production]`, `[EnemyAI:SuperWeapon]` lines (shared with [[TODOs/Optimization/Online_Battle_Low_FPS]] Phase 0).
- [ ] Invert/realign `defenseThreatRatio` (harder AI defends earlier), or compare attackers against home defences + forces within N seconds of the base.
- [ ] Proportional response: send a detachment sized to the base threat (task force) instead of `committedFleetRatio` 1; keep the main fight if local advantage holds.
- [ ] Treat station damage source as a sighting (reveal attacker) for threat.
- [ ] Cap superweapon spend share per AI (e.g., ≤ 20% of income) and log per player.
- [ ] Replay: 2 humans attack fleet, 1 human raids base at minute ~8; compare AI losses before/after.

## Agent handoff
- Read this plan, its linked plans and the live source first; source wins over this note.
- Follow `AGENTS.md`: no Play Mode, no tests (local override). Update/add the tests listed here and report them as not run.
- Unity assets: change them through Unity tooling, re-serialize the changed paths, check the console.
- Hand back: changed files, checked-off steps, deviations and open questions, then ask the user for review.

## Tests (not run — local override)
- `EnemyAi`: `EnemyStrategicDecisionModel` tests (defense rule thresholds), defense strategy tests. Assert relationships (harder ≥ earlier defense), not tuned numbers.

## Files
- `Entities/EnemyFaction/Models/EnemyAiDifficultyProfile.cs`, `EnemyStrategicDecisionModel.cs`, `EnemyDefenseStrategy.cs`
- `Services/Enemy/EnemyStrategicContextBuilder.cs` (`BuildBaseThreat`), `EnemyStationPressureTracker.cs`, `EnemySuperWeaponController.cs`, `EnemyAiTelemetry.cs`
