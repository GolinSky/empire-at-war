---
category: Bugs
status: todo
agent_marker: AGY
created: 2026-11-18
---
# [AGY] Enemy AI Mid-Fight Base Defense at Level 3 and Difficulty Consistency

## Goal
- Eliminate the Level 3 mid-game fleet starvation and slow base-defense response that leave `UltraHard` AIs vulnerable to fast player base rushes.
- Remove snowball-vs-stall variance so the same AI difficulty produces reliable fleet strength and coordinated team defense across matches.

## Evidence (`Player.log` Host Battles 5–8)
- **Host Battle 8** (`Lobby 109775244899637107`, 3 humans vs 4 `UltraHard` Rebellion AIs, `08_host_battle4_...`):
  - `L43335`: `Player3:Rebellion` entered `DefendBase` with only `Committed=2/3` ships against attacking forces.
  - `L47867`: At `FactionLevel=3`, `State=HuntFleet`, `Player3` spent `2000/2428` credits on `SecondStationLevel1`; at `L48653` (`FactionLevel=4`), spent `3200/3532` on `SecondStationLevel2` (`Defense=5/3`, `Mining=5/7`) rather than ships.
- **Host Battles 5–8 (`10_enemy_ai_state_and_production_digest_AGY.log`)**:
  - Level-3 AI teammates (`Player4/5/6`) repeatedly prioritize `SecondStationLevel1/2/3` (`2000 + 3200 + 4000 = 9200` credits total) and defense platforms while another AI's station is under early pressure.
  - Peak ship counts diverge widely under the same `UltraHard` setting: when unpressured early, AIs reach `20–25` capitals (`Battle 7: 25+24+17+23+18 = 107` AI ships); when disrupted around Level 3, AIs stall with small fleets while hoarding for upgrades/backup stations.

## Root Causes (Source-Verified)
1. **Level 3 Fleet Starvation via `ShouldSaveForLevel` & `SecondStation` Saving (`EnemyProductionDecisionModel.cs:242-327`, `EnemyProductionStrategy.cs:251-254, 342-356`, `EnemyAiDifficultyProfile.cs:19`)**:
   - On `UltraHard`, `ShipOrdersPerLevel = 1`. Once cumulative `ShipsOrdered >= CurrentFactionLevel * 1` (`3` orders total by Level 3), `IsLevelDue` (`L312-320`) and `ShouldSaveForLevel` (`L323-326`) become `true` whenever `StrategicState != DefendBase`.
   - `EvaluateUltraHard` (`L259-295`) checks before buying ships: (1) `MiningFacility`, (2) `IncomeResearch`, (3) `Level` (`L272-276`), (4) `CombatResearch` (blocked if `ShouldSaveForLevel`), (5) `DefensePlatform`, (6) **`if (ShouldSaveForLevel) return EnemyProductionCategory.None;` (`L292-295`)** → completely stops ship purchases to save for Level 4!
   - `TryBuySecondStation` (`EnemyProductionStrategy.cs:342-356`) also runs before `Evaluate` unless `_stationPressure.IsStationCritical`, buying/upgrading `SecondStationLevel1/2/3` (`L226-236`) whenever money reaches the backup multiplier threshold.
   - Until `_stationPressure.IsStationCritical` (<50% HP or <120s TTL), `TrySelectCounterUnit` (`L427-465`) and `TrySelectShip` (`L514-560`) pick expensive top-tier units (`L525: Affordability is checked after selection so the AI saves for higher-tier ships`) → `EvaluateUltraHard` returns `None` while saving instead of buying immediately affordable combat ships.
2. **Late `HasPressure` / `DefendBase` Trigger (`EnemyStrategicContextBuilder.cs:1170-1219`, `EnemyStationPressureTracker.cs:26-27, 63-70`)**:
   - `BuildBaseThreat` (`L1190-1193`) ignores hostiles unless `CanThreatenStation(...)` AND (`combat.IsAttacking(ownBase)` OR `distance <= max(combat.WeaponRange, footprint + hostileRadius)`).
   - A player fleet closing within `1,500–2,500m` of the station or destroying nearby mining facilities / defense platforms does **not** set `visibleThreatId` (`L1193`), forcing `HasPressure` to wait for `SUSTAINED_DAMAGE_SECONDS = 3f` (`EnemyStationPressureTracker.cs:11, 63-65`) on the station itself.
   - Slow capital ships across the map take `60–120+s` to turn and return once station hull/shield damage is already underway.
3. **Allied AIs Ignore Base Rushes Until `< 50%` Station HP & Reject Unsafe Approaches (`EnemyStrategicContextBuilder.cs:317-338, 349-352`, `TeamAiCooperation.cs:85-98, 107-125`)**:
   - `AssessStation` (`L349-352`) only calls `Cooperation.RequestDefense` when `_stationPressure.IsStationCritical` (`DurabilityFraction < 0.5f` or `EstimatedTimeToLossSeconds < 120f`).
   - `TeamAiCooperation.TryGetDefenseAssignment` (`L85-98`) dispatches **at most one** nearest available AI (`FindNearestResponder`).
   - `SelectAllyDefenseRequest` (`EnemyStrategicContextBuilder.cs:317-322`) rejects the assignment if `!IsMarkerApproachSafe(station)` (`!IsObjectiveSafe(..., REQUEST_RADIUS, false)` using that single AI's force against the attackers). Against a 3-player rush on one station, every single AI ally fails the solo safety check and abandons the besieged station.
4. **75% Main-Body Cohesion Gate Stalls Fleets (`EnemyTaskForceExecutor.cs:297-302, 494-498, 528-535`)**:
   - `ExecuteCapture` (`L297`) and `IssueAttackOrHunt` (`L494`) require `_body.Count * 4 >= count * 3` (75% of assigned ships within body cohesion) and `_attackForce.IsReady`.
   - When 26%+ of the fleet are slow dreadnoughts or freshly reinforced ships trailing behind, line 531–532 calls `IssueStop(anchor)` and halts the entire forward fleet in place.

## Important Values
- `EnemyAiDifficultyProfile` (`UltraHard`): `ShipOrdersPerLevel = 1`
- `EnemyStationPressureTracker`: `SUSTAINED_DAMAGE_SECONDS = 3f`, `DAMAGE_WINDOW_SECONDS = 5f`, `DAMAGE_LATCH_FRACTION = 0.005f`, `CRITICAL_FRACTION = 0.5f`, `QUIET_SECONDS = 30f`
- `EnemyTaskForceExecutor` body cohesion threshold: `_body.Count * 4 >= count * 3` (`75%`)

## Implementation
1. **Enforce active-ship floor per faction level & emergency affordable production (`EnemyProductionDecisionModel.cs`, `EnemyProductionStrategy.cs`)**:
   - Gate `IsLevelDue` / `ShouldSaveForLevel`, `SecondStation`, and non-critical `DefensePlatform` spend behind an active combat-ship minimum per level (e.g., `ActiveShipCount >= CurrentFactionLevel * minShipsPerLevel`, ~`9` ships at Level 3).
   - Bypass `ShouldSaveForLevel` and second-station saving whenever `HasPressure` or `StrategicState == DefendBase`; fall back to the strongest **immediately affordable** combat ship in `TrySelectCounterUnit`/`TrySelectShip` when under base threat below the fleet floor.
2. **Add station perimeter early-warning detection (`EnemyStrategicContextBuilder.cs:1170-1219`)**:
   - Include hostiles within an extended station warning perimeter (`footprint + weaponRange + PERIMETER_MARGIN`) or actively attacking friendly mining/defense structures near home base so `HasPressure` and `DefendBase` trigger before direct station bombardment begins.
3. **Coordinate multi-ally defense before 50% station loss (`EnemyStrategicContextBuilder.cs`, `TeamAiCooperation.cs`)**:
   - Publish `RequestDefense` as soon as `HasPressure && baseThreatRatio > 1.0f` instead of waiting for `IsStationCritical`.
   - Allow `TeamAiCooperation` to recruit multiple allies up to sufficient combined strength and evaluate `IsMarkerApproachSafe` using combined defending + responding allied strength.
4. **Decouple forward fleet advance from trailing reinforcements (`EnemyTaskForceExecutor.cs`)**:
   - Evaluate cohesion on ships not freshly reinforced / already converged above minimum combat ratio so trailing slow ships do not `IssueStop` an engaged main fleet.

## Edge Cases
- `ProductionHost` != `_owner.Id` (hosted AI economy) → keep `TryBuySecondStation` recovery path intact when host station is destroyed.
- Avoid all AIs abandoning every capture point simultaneously for a single frigate raid → scale number of requested allied responders to attacker combat value (`baseThreatForce`).

## Files
- `Assets/Scripts/Entities/EnemyFaction/Models/EnemyProductionDecisionModel.cs`
- `Assets/Scripts/Entities/EnemyFaction/Models/EnemyAiDifficultyProfile.cs`
- `Assets/Scripts/Services/Enemy/EnemyProductionStrategy.cs`
- `Assets/Scripts/Services/Enemy/EnemyStrategicContextBuilder.cs`
- `Assets/Scripts/Services/Enemy/EnemyStationPressureTracker.cs`
- `Assets/Scripts/Services/Enemy/TeamAiCooperation.cs`
- `Assets/Scripts/Services/Enemy/EnemyTaskForceExecutor.cs`
- Affected test suites to review (do not run): `Assets/Scripts/Tests/Editor/EnemyAi/`

## TODO
- [ ] Add active-ship floor per level & affordable-fallback under pressure in `EnemyProductionDecisionModel` / `EnemyProductionStrategy`
- [ ] Expand `BuildBaseThreat` perimeter and nearby-structure pressure detection
- [ ] Enable multi-ally `TeamAiCooperation` defense scaled to attacker strength + combined approach safety check
- [ ] Prevent trailing reinforcements from stalling engaged `EnemyTaskForceExecutor` main body
- [ ] Review/update `EnemyAi` EditMode tests
