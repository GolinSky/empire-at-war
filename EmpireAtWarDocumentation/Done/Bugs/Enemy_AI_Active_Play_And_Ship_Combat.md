---
category: Bugs
status: done
created: 2026-11-19
replaces: "[[TODOs/Bugs/Enemy_AI_UltraHard_Followup_And_Ship_Idle]]"
completed: 2026-11-19
acceptance: pending-user
---
# Enemy AI Active Play And Ship Combat

## Goal
- AI plays actively for the whole battle: expands, holds the centre, takes ≥ 2 mining zones, keeps reinforcing. The human must not win in 9 min against an idle AI.
- Ships keep the bow/main battery on the target while firing.
- Remove the "aggro when shot" pursuit and the AI second station.
- Source of truth = user rules + battle logs below. Current code is not a reference for intended behaviour.

## User rules (2026-11-19, final — no open decisions)
- Opening: buy all 5 spawnable mining facilities first → capture the neighbour zone, the centre and the top-left mining zone with separate groups → capture the close satellite pads → build satellites → build mining.
- Mining = economy; satellites protect mining and take/hold zones. Both outrank ships. Ships alone die fast → no money, no protection, no economy.
- AI must hold ≥ 2 mining zones besides home: the one near it and the top-left one. Top-left capture is **critical**.
- Centre: satellites then a steady flow of units; levels bought early.
- Ship hit while idle → no aggro/pursuit. Remove the logic completely.
- No AI station near the main zone. No second station without money and a mining zone. Remove the logic completely.
- Ship turning its stern to the target while shooting is a bug. Rework attack movement/facing if the current logic cannot hold the bow on target.
- Level 1: 1 corvette is enough (corvettes are expensive at level 1); build squadrons for capture and patrol.
- Hold reinforcements in reserve; spawn a small group or a big armada when a zone must be taken or defended.
- Sabotage: kill the human's mining structures and push non-central mining zones.
- Ships in range stay in place and shoot; no moving away to missile/artillery range (Victory).
- Retreats do not work for the AI: when a retreat would fail, stand to the end and make it count (focus a mining structure or a weak ship).
- Centre-zone goal is right; execution must hold it (01:11: taken, then lost).

## Evidence — battle 2026-11-19 00:49 (code `c43df90dc`, dirty)
Files: `BattleTelemetry/Loading_20261119_004939_ai.csv`, `_ai_orders.csv`, `Battle_20261119_004943_map.csv`, `Battle_20261119_005049.csv`; Editor.log.
- Map: Small Coruscant, seed `-1393780894`. Human station (−1349, −1015) bottom-left; AI station (1349, 1015) top-right. AI home zone 2 (578, 1015). Centre point 2 = 4 mines (sites 0–3) + 6 pads (4–9). Top-left zone 3 (−1533, 1488) = site 14 mine + pads 15, 16. Bottom-right zone 4 (1533, −1488) = site 17 + pads 18, 19.
- **AI difficulty was Medium** (Editor.log `Difficulty=Medium`, BattleTime 0–549). Every UltraHard-only rule (mining-first, mining target 5, ship reservation) never ran. Previous battle 22:12 was UltraHard; the 23:50 and 00:49 battles ran Medium.
- Battle end ≈ 562 s; human win. AI ships at 368 s: 1 own vs 18 enemy.

### Opening / economy
- AI bought ships first: PatrolFrigate 13.7 s, C9979 18.7, Vulture 23.7, PatrolFrigate 28.8. First miner 33.8 s; then 53.8, 123.8, 258.2 → max 4 miners, never 5.
- First satellite = home pad 13 at 22.6 s (before any miner), money 7,462.
- Centre: satellites on pads 6 (61.6 s) and 5 (70.6 s); no third, nothing more at the centre for the rest of the battle.
- Levels: L2 bought 48.8 s → done 128.8 s; L3 bought 143.9 → done 263.9; L4 bought 267.7 → done 447.7.
- Human: no ship until 47.7 s; first ship straight to the centre (49 s Move to centre mine site 3); human won the centre fights from 79 s.

### Captures and idling
- AI capture objectives: own home mine site 12 (28.7 s), centre pads 5/4 (44–86 s), centre mines 1/2 (92–164 s, dropped `invalid:unsafe`), bottom-right zone 4 sites 17–19 (46–168 s).
- **Top-left zone 3 (sites 14/15/16): targeted 13 times 111–268 s; every time dropped `invalid:unsafe` or `reserved` → RetreatValue. Never captured.**
- Decision states: DefendBase 53, RetreatValue 51, EngageLocal 19, CaptureZone 15, Raid 3.
- RetreatValue reasons: "Withdraw from the outmatched forward station…" ×28 (`forwardStationFallback`), "Cover the safe recovery anchor while rebuilding" ×15, "Hostiles near the fleet outmatch it" ×8.
- AI ship samples: no order 42 %, Retreat 16 %, Attack 24 %, AttackMove 16 %. 240–300 s: 88 of 98 samples with no order. Human: no order 25 %.
- Recovery mode on at 170.7 s (`fleetWeak`, fleet 0.46) with own 6 vs enemy 2 ships at 123 s already rated 0.45.

### Second station
- `SecondStation` bought 180.2 s in recovery mode, placed 270.2 s at (753.9, 1015.2) = 176 from the AI home zone (`source=anchor`). Then `SecondStationLevel1` 277.7 s and `SecondStationLevel2` 358.8 s.
- Cause: `EnemyProductionStrategy.TryBuySecondStation` (`:349`) skips the level and money gates while `_recovery.IsActive`; placement uses the recovery anchor (home zone).
- Effect: 28 `forwardStationFallback` retreats.

### Aggro when shot
- 29 `pursuit` events ("Attacked while idle; pursuing …"), both players; 8 "Pursuit target destroyed; returning to the post".
- Source: `ShipOrderRunner.UpdatePursuit` / `ObserveHealth` / `PURSUIT_*` (added `7c3dfd19b`, return added `2955d807a`), fed by `Ship.HandleHealthChanged`.

### Stern to target while firing
- In-range Attack/AttackMove samples (`preferredInRange=1`): human 82 of 555 with `facingDot < 0`, 56 below −0.5; AI 31 of 82, 18 below −0.5.
- Most cases: `hold=inReach move=approach`, movement `Idle` (ship stopped in reach, not turned to the target); also `hold=moving`, `pursuitStop`.
- Worst human ships: 139 (12 samples), 153 (10). AI: 141, 143.
- Related: [[TODOs/Bugs/Attack_Order_Hold_In_Range]] (`001ab9e5b` hold in reach), [[TODOs/Bugs/Capital_Movement_And_Class_Rework_Fixes]] (stern-first retreat).

## Verification of `c43df90dc`
- R1 pad lock removed (`EnemySiteObjectives`): pads capturable without owning point mines. Matches rules.
- R2 `IsZonePoint` = no mines or more mine sites than `SITES_PER_MINING_POINT` → centre needs 3. Matches rules; in battle only 2 built (money went to ships on Medium).
- R3 opening focus: `Neighbour` only adds +10,000 to centre objectives; it does not aim at the neighbour zone. Groups 1/2 went to the AI's own home mine (site 12). No top-left focus exists. Centre = capturable zone with the largest radius; disabled if another capturable zone has the same radius.
- R4 failed satellite start falls through to mining. Correct.
- R5 4 of 6 tests added; missing: pressured-station recovery anchor, construction order.
- Mining-first and mining target 5 are gated to UltraHard → not active in this Medium battle.

## Evidence — battle 2026-11-19 01:11 (UltraHard, code `c43df90dc`, dirty)
Files: `BattleTelemetry/Loading_20261119_011116_ai.csv`, `_ai_orders.csv`, `Battle_20261119_011119_map.csv`, `Battle_20261119_011246.csv`; Editor.log `Difficulty=UltraHard`.
- Map: Small Coruscant, seed `-319851211`. Human station (−1358, −1363) bottom-left; AI station (1358, 1363) top-right; AI home zone 2 (587, 1363). Top-left zone 3 (−1471, 1407): site 14 + pads 15/16. Centre point 2: mines 0–3, pads 4–9.
- End ≈ 677 s, human win. AI station dead at 659 s.

### What worked
- Mining first: 5 `CommonMiner` at 12.1–16.1 s.
- Top-left: pads 16 → 15 → mine 14 targeted 46–107 s; satellites on pad 16 (99.2 s) and pad 15 (123.2 s).
- Centre: pads 8, 9 captured; satellites 81.2 s (pad 8) and 117.2 s (pad 9). User: good direction, lucky, then lost.

### Level-1 overspend on corvettes
- Before level 2 finished (173.4 s): 9 Patrol Frigates (1,500 each = 13,500) + 7 C-9979 (1,200 each = 8,400) + 3 Xq6 platforms. Level 2 bought only at 93.4 s.
- Levels: L2 done 173.4 s, L3 302.9 s, L4 487.2 s.

### Reinforcements spawn one by one
- Every finished ship deploys immediately; 63 `deploymentBlocked`, 7 `deploymentForced` (579–642 s, station besieged). No held reserve, no grouped spawn.

### No sabotage
- AI units vs human structures, whole battle: ≈ 10k hull, 3 kills (Recusant 4.5k, satellite 3.5k, squadrons/others rest).
- Human units vs AI structures: ≈ 290k hull, 17+ kills (HeavyDreadnought 73k/7, Arquitens 71k, Acclamator 59k/5, Resolute 43k, Victory 35k).
- AI `RaidStructures` (25 decisions) targeted only centre mine site 3 (92–162 s) and late raids (468–530 s); never the human home mines, spawned miners or the bottom-left zone.

### Fleet relocation and retreat collapse
- 230–330 s: main group 6 flips RetreatValue ↔ EngageLocal every 2–20 s ("Cover the safe recovery anchor while rebuilding" / "Hostiles near the fleet do not outmatch it").
- Recovery mode on 236.3 s (`fleetWeak`, fleet 0.61) with 11–12 own vs 3–7 enemy ships.
- `SecondStation` bought 245.2 s in recovery; recovery anchor moved 267.2 s from home to the top-left zone (−1471, 1407), 2,829 from the main station.
- 280–320 s: the 12-ship fleet left home and crossed the top of the map to the top-left (centroid 524,1173 → −1034,1473); human fleet followed (272,484 → −380,1320).
- 335.2 s: second station placed at (−1223, 1777) next to the top-left zone.
- 333–367 s: fight at top-left, "No safe retreat anchor… fights back"; 12 → 5 ships.
- 373.6 s: "Withdraw from the outmatched forward station" → `Retreat` to the main station (≈1,700, 1,300), 2,600 away, path through the human fleet (centroid −497, 1304). 6 → 1 ship in 15 s; 0 ships at 393 s.
- "Withdraw from the outmatched forward station" is the most common decision reason: 44 of 206.

### Victory (human) moves away instead of firing
- Ships 208, 209, 220 (hull 7,150, weapon range 715) run with `artillery=1`, `artReach=502`, `engage=518`; approach legs end at `destDist=452`.
- Target close and in range → `contactRetreat` leg out to 452: 209 at 584 s (dist 107 → stern, face −0.77 / −0.96); 220 at 658 s (dist 52 → face −0.61 / −0.87 / −0.98).
- Target already in reach (dist 489) → another `artilleryApproach` leg to 452 (550.6 s).
- Held in `artilleryBand` on the station, 208 swung from face 0.96 to −1.0 (647–652 s) without a new order.

## Fixes

### F1 — Rules for every difficulty (P0)
- [x] Opening/economy rules apply to all difficulties. Difficulty changes only reaction speed, ability use and attack thresholds, not build order.
- [x] Remove the `UltraHard` gate from mining-first and mining target = catalog `MaxCount` (5).
- [x] Lobby: the 00:49 battle ran Medium after an UltraHard battle → check offline lobby memory restores the last AI difficulty; fix if not.

### F2 — Opening build order (P0)
- [x] Mining facilities to 5 before any ship, level, research or squadron (`EnemyProductionDecisionModel`, `NeedsMinimumFleet` after mining).
- [x] Satellite on the home pad waits until the 5 miners are bought (22.6 s satellite came before any miner).
- [x] Level 2 bought right after the 5 miners; levels keep coming early (no long ship-first stretch).

### F3 — Expansion: neighbour, centre, top-left (P0, critical)
- [x] Explicit opening targets, not score nudges: group A → nearest relay mining zone (here zone 4); group B → centre; group C → the other relay mining zone (top-left, here zone 3). With fewer ships, fill in order A → B → C.
- [x] Home mine site is captured by the first ship on its way, not as a group objective.
- [x] A zone objective is not dropped as `unsafe` because of a weak defender: compare local strength; drop only when the defenders are clearly stronger, then come back with more ships.
- Battle acceptance: AI holds ≥ 2 mining zones besides home by ~4 min.

### F4 — Centre hold (P0)
- [x] Centre gets 3 satellites (rule R2) before more optional spending; then steady reinforcement: new ships rally to the centre while it is held/contested.
- [x] Centre mines built once the pads have satellites.
- [x] Centre is not dropped as `unsafe` (01:11: dropped at 158–163 s after 2 satellites, then lost). Held reserve (F11) reinforces it.

### F5 — Stop idling / over-retreating (P0)
- [x] Fleet strength rating: own 6 vs enemy 2 rated 0.45 at 123 s → audit the advantage calculation (composition weighting) and recovery trigger `fleetWeak`.
- [x] Recovery mode must not park the fleet ("Cover the safe recovery anchor" ×15, 88 idle samples 240–300 s): recovering ships defend owned mining zones/centre or capture, never stand idle.
- [x] Remove `forwardStationFallback` withdrawal (goes with F6).
- [x] Idle ship with no order for > a few seconds gets a task (capture, guard zone, reinforce centre).

### F6 — Remove AI second station (P0)
- [x] Remove `TryBuySecondStation` from AI production and the recovery-mode bypass; AI never buys `SecondStation` or its levels.
- [x] Remove `forwardStationFallback` in `EnemyStrategicDecisionModel` and other second-station AI branches.
- 01:11 battle: bought again in recovery (245 s), placed at the top-left zone (335 s); caused the fleet relocation and the 44 forward-station withdrawals (see F13).

### F7 — Remove aggro when shot (P0)
- [x] Delete `ShipOrderRunner.UpdatePursuit`, `ObserveHealth`, `PURSUIT_*`, `_isPursuing`, `_pursuitOrigin`, pursuit-return in `CompleteOrder`, the `Ship.HandleHealthChanged` call, `pursuit` telemetry.
- [x] Delete/adjust tests: `ShipPursuitTests` pursuit/return cases (`Navigation`).

### F8 — Bow on target while firing (P0)
- [x] Reproduce from telemetry: in-reach hold with `movement=Idle` and `facingDot < 0` (ships 139, 153, 141, 143). Find why the held ship does not turn to the target (`AttackTargetState` hold path, facing solver).
- [x] A held ship in reach turns toward the target (main battery side) while firing; no stern-first holds or retreats.
- Existing approach and hold paths were corrected. Child rewrite was conditional.
- [x] Add telemetry check: share of in-range samples with `facingDot < 0` → target ≈ 0 for ships whose main battery faces forward.

### F9 — Remaining from the blocked plan
- [x] Tests: pressured-station recovery anchor; satellite → mine → optional construction order.
- User test run → [[TODOs/User_Acceptance_Queue#Enemy AI Active Play and Ship Combat]].

### F10 — Level-1 production (P0)
- [x] At level 1: at most 1 corvette (Patrol Frigate or C-9979). Spend the rest on squadrons for capture and patrol.
- [x] Level 2 right after the 5 miners and the first corvette (it came at 93 s / done 173 s, after 21,900 on corvettes).

### F11 — Held reinforcements (P0)
- [x] Finished units stay in a reserve instead of deploying one by one.
- [x] The AI spawns from the reserve as a small group or a full armada when a task needs it: take a zone, defend the station/centre, raid.
- [x] Spawn point = station or held zone nearest the task (living stations first, as already implemented).

### F12 — Sabotage (P0)
- [x] Raid targets include the human's home mines, spawned mining facilities and non-central mining zones, not only the centre.
- [x] Use squadrons or small fast groups for raids; keep raiding while the main force holds the centre.
- Battle acceptance: AI damage to human structures is a real share of the battle (was ≈ 10k hull vs the human's ≈ 290k).

### F13 — Retreat discipline (P0)
- [x] No cross-map retreat: never retreat toward an anchor whose path crosses the enemy fleet or that is far (here 2,600). Fight where you are instead.
- [x] When a fight is lost or retreat is unsafe, stand and make it count: focus the weakest enemy ship or the nearest enemy mining structure.
- [x] No recovery-anchor relocation to a far zone (267 s home → top-left); the anchor stays at the main station/held zone near the fleet.
- [x] No RetreatValue ↔ EngageLocal flip-flop: once committed to a fight, hold it for a minimum time unless the local strength drops sharply.
- [x] `fleetWeak` recovery trigger re-check: 11–12 own vs 3–7 enemy rated 0.61 (with F5).

### F14 — Ships fire from where they are (P0, with F8)
- [x] A ship whose weapons reach the target holds and fires; no `contactRetreat` or `artilleryApproach` leg while the target is in range.
- [x] Artillery stand-off only while approaching from outside range; never back away from a target that closed in.
- [x] A held ship keeps the bow/main battery on the target (Victory swung to face −1.0 while held).
- Existing approach and hold paths were corrected. Child rewrite was conditional.

## Order
1. F7, F6 (removals).
2. F13, F14/F8 (stop the collapses; ships fire in place).
3. F1, F2, F10, F11 (economy, level 1, reserve).
4. F3, F4, F12, F5 (expansion, centre, sabotage, activity).
5. F9.

## Outcome and Verification

- F1–F14 implementation complete. Existing offline lobby persistence restores the saved per-slot difficulty before rendering the menu and writing it into the battle setup. Reviewed `LobbySettingsDataTests`; no route or settings changes needed.
- Strategic AI uses the same mining-first order on all difficulties. One scouting wing may precede the last miners when no valid placement is visible. Level 2 can start without the optional corvette when that corvette is unavailable or unaffordable.
- Paid finished units form a deployable wave. The first group, station defense and a full-population task can release a lone waiting unit after 12 s. Critical station pressure releases ready defenders immediately. Saved pending second-station purchases and upgrades refund; existing constructed stations remain loadable.
- The 01:11 log at 236.3 s shows 12 friendly ships and 7 known hostile ships, team recovery strength 0.61, and local raw strength 1.279. At 237.8 s local raw strength was 3.377. Recovery now requires sustained weakness in the local fight as well as team weakness and affects placement only. Health, weapons, ship classes and strikecraft inform the strength estimate.
- No combat rewrite child plan needed. `AttackTargetState` now enters hold at usable reach, and combat facing selects forward valid mounts or points the bow at the target. The telemetry audit measures actual target bearing by ship type.
- Unity 6000.4.7f1 compiled project and Editor fixture assemblies with zero errors. `git diff --check` and Python AST parsing for `battle_audit.py` passed.
- Updated EnemyAi production, lifecycle, strategy, stability, recovery, scheduling, capture/raid, construction, and squadron assignment fixtures. Updated `ShipPursuitTests`, `WeaponFacingSolverTests`, `ShipSpawnPointsTests`, `RealEntityCompositionTests`, and `PlayerSaveTests`. Reviewed dependent capture, order, movement, ranged-weapon and lobby contracts.
- Tests were not run under the checkout override. Battle and test-run gates → [[TODOs/User_Acceptance_Queue#Enemy AI Active Play and Ship Combat]].
- Reusable rules → [[Architecture/Enemy_AI_Objectives]], [[GameDesign/Combat#Artillery standoff]], [[Rules/WEAPON_EFFECTIVE_DPS_LIMITATIONS#Artillery engagement]].

## Files
- `Entities/EnemyFaction/Models/EnemyProductionDecisionModel.cs`, `EnemyStrategicDecisionModel.cs`, `EnemyAiDifficultyProfile.cs`
- `Services/Enemy/EnemyProductionStrategy.cs`, `EnemyShipTaskGroupPlanner.cs`, `EnemyStrategicContextBuilder.cs`, `EnemySiteObjectives.cs`, `EnemySiteConstructionController.cs`, `EnemyRecoveryAnchor.cs`
- `Entities/Ship/Orders/ShipOrderRunner.cs`, `Entities/Ship/Ship.cs`, `Entities/Ship/StateMachine/AttackTargetState.cs`
- `Entities/MainMenu/Skirmish/SkirmishRouteUiController.cs`, `Services/Settings/LobbySettingsData.cs`

## Tests
- Categories: `EnemyAi`, `Ships`, `Navigation`, `Capture`, `Ui`. No stat-pinning asserts. Not run by agents (local override).
