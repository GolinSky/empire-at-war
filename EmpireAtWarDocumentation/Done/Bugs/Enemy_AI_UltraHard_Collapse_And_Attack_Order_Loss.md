---
category: Bugs
status: done
created: 2026-11-18
parent: "[[TODOs/Bugs/Enemy_AI_And_Faction_Balance]]"
completed: 2026-11-18
acceptance: failed
followup: "[[TODOs/Bugs/Enemy_AI_UltraHard_Followup_And_Ship_Idle]]"
---
# Enemy AI UltraHard Collapse And Attack Order Loss

## Goal
- Fix code defects found in the 2026-11-18 20:43 UltraHard Separatist loss (seed `-1727569051`, Small Coruscant).
- Fix player ships dropping an Attack-station order after auto-power retargets them.
- Source-verified only. No new playtest steps; user verifies in-game.

## Evidence
- AI telemetry: `BattleTelemetry/Loading_20261118_204343_ai.csv`, `_ai_orders.csv`.
- Player0 = human Republic, Player1 = AI Separatist, AI station entity id `2`.
- Human ship 189: Attack `2` → replaced by Attack `200` at 573.72 (per-ship, no group order) → killed `200`, `201` → 579.57 `Target destroyed; no replacement order.` → Idle.
- Ships 159/174/166 switched Attack `2` → `61` on separate frames (565.06/565.07) → not a player click.
- AI capture objectives after 140 s: only `zone:0/1/2`, `site:2`, `site:17`; each dropped as `invalid:unsafe`.
- Home-defense deployments 414–613 s: spawn 600–977 units from the station rally (fallback zone).
- Spend: Ship 108.6k, Research 16.3k, Level 14.4k, Mining 6k, Defense 5k.

## Fixes

### 1. Player auto-power steals Attack orders (bug the user reported)
- Cause: `PlayerAutoPowerService.Choose` (`Services/ShipAbilities/PlayerAutoPowerService.cs:137`) → `AutoPowerCombatSnapshot.FindTarget` picks the nearest non-strikecraft in ability range, ignoring the ship's order target.
- Targeted abilities then call `IAttackFacade.Attack(target)` → `Issue(Attack)` replaces the station order: `AssaultAbility.Start`, `ConcentrateFireAbility.Start` (also retargets every ally in `CommandRadius`), `LuckyShotAbility.Start`.
- Then `ShipOrderRunner.CompleteOrder` (`Entities/Ship/Orders/ShipOrderRunner.cs:394-410`) chains to nearest radar contact or Idle; the original target is never restored.
- [x] `FindTarget` prefers the registered, visible and in-range Attack target.
- [x] Automatic Assault, Concentrate Fire and Lucky Shot skip the slot if the Attack target cannot be used. Authority rechecks the current order and range. Matching Assault/Lucky Shot attacks retain their order details.
- [x] Automatic Concentrate Fire redirects Idle/AttackMove/Hunt allies and preserves existing Attack orders. User confirmed manual casts can override allied Attack orders.
- Tests: `Abilities` (auto-power, ConcentrateFire), `Ships` (order runner). Not run.

### 2. AI never targets player mines (user finding: no mining captures)
- `EnemySiteObjectives.IsCapturable` (`Services/Enemy/EnemySiteObjectives.cs:74-79`) accepts only `Neutral`/`Owned`; `Constructing`/`Operational` hostile sites are never objectives.
- Raid path `FindRaidTarget` (`Services/Enemy/EnemyStrategicContextBuilder.cs:677`) requires the mine to be visible now → mines in human territory are ignored. 1 `RaidStructures` switch all battle.
- Main group scoring `GetCaptureScore` (`EnemyStrategicContextBuilder.cs:1006`) adds flat `+2000` to every non-relay → main force only chases relays (center).
- [x] Team intel retains observed stationary mines. Constructing and operational hostile mines are raid/capture objectives. The fleet attacks the blocking facility before capturing the site.
- [x] Capture ranking weighs travel and ion cost against relay and income value. Nearby mines can beat distant central relays.
- [x] A lone ship may detach for an unblocked mine when the route is safe. A ship engaged with a known local force retains its Attack order.
- Note: early game the AI did capture `site:12` (ownedOrRemoved at 52.5 s). The failure is post-140 s expansion/recapture.
- Tests: `EnemyAi`, `Capture`. Not run.

### 3. Capture push stalls outside the circle
- `ExecuteCapture` diversion check (`Services/Enemy/EnemyTaskForceExecutor.cs:371`): `distance > closest + leash` only fires when a ship moves away. A ship stopping to fight outside the circle never trips it.
- `AttackMoveState` pursues every contact in the shared engagement set with no leash to the destination.
- [x] After 5 s without meaningful progress outside the circle, issue Move to a clear interior sample. Resume AttackMove inside the circle. Keep the distance leash for excursions.
- Tests: `EnemyAi`, `Navigation` if AttackMoveState changes. Not run.

### 4. Late retreat and order flip-flop
- `EnemyAdvantageSmoother` (`Entities/EnemyFaction/Models/EnemyAdvantageSmoother.cs:19`): symmetric 4 s EMA → collapse reacts late (163.6 s: raw 0.18, smoothed 3.04 → EngageLocal).
- `EnemyStrategicDecisionModel.Evaluate` `held` (`EnemyStrategicDecisionModel.cs:319`, `MIN_STATE_SECONDS = 8`) blocks EngageLocal/CaptureZone → RetreatValue (169.7 s: raw 0.14 held).
- `Bypass = "cornered"` skips the hold entirely → main group CaptureZone ↔ EngageLocal/Hold flips in <1 s.
- [x] Apply advantage drops immediately and smooth gains with a 4 s exponential average.
- [x] Entry into viable retreat bypasses offensive commitment.
- [x] Use a 2.5 s cornered commitment while the previous objective stays viable.
- Tests: `EnemyAi` (decision model). Not run.

### 5. Home-defense reinforcements spawn in the enemy lane
- `EnemyFactionController.Build` (`Entities/EnemyFaction/Controllers/EnemyFactionController.cs:305`): defend-home → `TryGetSpawnPositionAround(rally)` else `TryGetDefaultZoneSpawnPosition`.
- `ShipSpawnPoints` samples 100 random points in `HomeAreaRadius`; hostile spawn-block bubbles reject points near the station, so the fixed home zone (771 units toward center) wins.
- `ReinforcementSpawnRule.CanSpawnShip` has no hostile weapon-range check.
- [x] Prefer rear positions and reject known hostile weapon reach with a safety margin.
- [x] Hold paid deployments and retry every 1 s without blocking production lanes or falling back to the forward home zone.
- [x] Gather ready defenders for up to 20 s. Release ready waves immediately under critical pressure.
- Tests: `Economy`/`EnemyAi` (`EnemyFactionControllerLifecycleTests`), `Navigation` (ShipSpawning). Not run.

### 6. Critical-pressure purchases that arrive too late
- `EnemyCounterProductionModel` (`Entities/EnemyFaction/Models/Combat/EnemyCounterProductionModel.cs:80-92`): with no timely candidate it still buys late ones; critical score `gain*pref/arrival` ignores price.
- 36/63 critical purchases had ETA > time-to-loss; final bomber ETA 63.9 s vs 1.2 s.
- [x] Skip combat purchases with ETA beyond station loss and save credits. Rank timely units by gain, resource cost and arrival time.
- [x] Reviewed bomber mix: 50/63 critical purchases and 9/35 noncritical purchases were bombers. Bombers accounted for 28/36 late critical purchases. Keep ordinary fleet mix and strikecraft weighting. Apply the critical timing and cost fixes before evaluating any balance changes.
- Tests: `EnemyAi` (counter production). Not run.

## Order
1. Fix 1 (player-visible, isolated).
2. Fix 4 + 3 (fleet survival).
3. Fix 5 + 6 (endgame).
4. Fix 2 (expansion; largest).

## Files
- `Services/ShipAbilities/PlayerAutoPowerService.cs`, `AutoPowerCombatSnapshot.cs`, `Abilities/ConcentrateFire/ConcentrateFireAbility.cs`, `Abilities/Assault/AssaultAbility.cs`, `Entities/Ship/Orders/ShipOrderRunner.cs`
- `Services/Enemy/EnemySiteObjectives.cs`, `EnemyStrategicContextBuilder.cs`, `EnemyShipTaskGroupPlanner.cs`, `EnemyTaskForceExecutor.cs`
- `Entities/EnemyFaction/Models/EnemyStrategicDecisionModel.cs`, `EnemyAdvantageSmoother.cs`, `Combat/EnemyCounterProductionModel.cs`
- `Entities/EnemyFaction/Controllers/EnemyFactionController.cs`, `Services/ShipSpawning/ShipSpawnPoints.cs`, `Services/Reinforcement/ReinforcementSpawnRule.cs`

## Verification
- Unity script compilation completed with zero errors. Reviewed and updated regressions in EnemyAi, Abilities, Ships, BattleInput and Combat. Reviewed related Capture, Economy, GameFlow, Networking and Core fixtures.
- Unity tests were not run under the local execution override. In-game acceptance remains with the user.
- Reusable rules → [[Architecture/Enemy_AI_Objectives]] and [[GameDesign/Abilities_And_Superweapons#Automatic player ship powers]].

## Acceptance (2026-11-18 22:12 UltraHard battle)
- Fix 1 (auto-power keeps Attack orders) held: no station attack replaced by an ability.
- Failed: defense-spawn hold never times out (447 `deploymentBlocked`); capture stall → `Move` locks ships out of combat; AI still loses map control.
- Follow-up → [[TODOs/Bugs/Enemy_AI_UltraHard_Followup_And_Ship_Idle]].
