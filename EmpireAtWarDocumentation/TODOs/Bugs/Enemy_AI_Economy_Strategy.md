---
category: Bugs
status: in-progress
created: 2026-11-19
follows: "[[Done/Bugs/Enemy_AI_Active_Play_And_Ship_Combat]]"
---
# Enemy AI Economy Strategy

## Review 2026-11-19
- Source review (no tests, no playtest) → [[TODOs/Bugs/Enemy_AI_Economy_Strategy - Review]]. X1–X5 and E1–E7 match source for 1v1 Small.
- Open before team games / Medium / Large: R1 (assault gate counts own zones only), R2 (uncapped centre reinforcement blocks expansion), R6 (expansion has no distance/count limit). Fix proposals for R1–R9 are in the review; none implemented.
- User decision 2026-11-19: income is shared across allies → an allied AI may take mines beside a teammate's base, but those mines get lower priority (R4).

## Reopened 2026-11-19 — acceptance failed (user: "full failure")
- `2fa16bea3` played 2026-11-19 13:52 on UltraHard (Separatist AI, top-right). User: lots of corvettes again, one sabotage, idle instead of acting, no early centre take/retake, unsure it captured even 2 mining spots.
- Implementation history (E1–E7 in `2fa16bea3`) kept below. Fix the regressions first (X1–X5), then re-check E1–E7.

## Evidence — battle 2026-11-19 13:52 (UltraHard, code `2fa16bea3`, dirty)
Files: `BattleTelemetry/Loading_20261119_135239_ai.csv`, `_ai_orders.csv`, `_squadrons.csv`, `Battle_20261119_135243_map.csv`, `Battle_20261119_135406.csv`; Editor.log `Difficulty=UltraHard`.
- Log time 20.4 s = battle 0 s. AI station (1118, 1087) top-right; human Republic (−1118, −1087). Outer zones: 3 (−1545, 1453) top-left, 4 (1545, −1453) bottom-right.
- Main station died ≈ 580–600 s (hull 46,969 at 570 s). Backup station placed 606 s at (−1667, 1000) by patrol; dead by 776 s. Battle ≈ 810 s.
- Decisions: EngageLocal 74, RebuildFleet 56, CaptureZone 20, DefendBase 10, Raid 6, Hold 5, GuardSite 4. Top reasons: "Hostiles near the fleet outmatch it" 58, "No combat ships are available" 56.

### X1 — Finished ships never deployed (P0, regression)
- 7,262 `deploymentBlocked` (minutes 4–9: 354, 767, 1163, 1576, 1962, 1419); only 1 `deploymentForced`.
- AI combat ships on the map: **0 from 296 s to 634 s** while purchases continued.
- Human squadrons reached 246–304 from the AI station at minutes 3–4; human ships 324–551 at minutes 7–9.
- Cause (source): new rear deployment in `EnemyFactionController` — `hasRear` → `TryGetDefenseSpawnPosition` / `TryGetBestDefenseSpawnPosition(stationAnchor, _defenseDeploymentThreats)` rejects every point near known threats; the bounded-hold forced spawn (8 s) is not reached on this path.
- Effect: at 609–614 s, after the backup station appeared, 3 Providence + 3 Recusant + squadrons deployed at once — the held queue.

### X2 — Corvette spam (P0, regression)
- 32 C-9979 (22–209 s, ≈ 38k credits) + 6 Patrol Frigates; still C-9979 after level 2 (108 s) until 209 s; then Diamond chains (236–348 s, 609–746 s).
- Each bought the moment money reached the price (money at purchase 1,200–1,460; `counter=0`, `critical=False`).
- Cause: E5 "build queue never idles while money ≥ the cheapest useful item" → the cheapest ship wins every tick; the AI never saves for better ships.

### X3 — Unseen-enemy estimate makes the AI passive (P0, regression)
- `fleetAdvantage` 0.69 at 31 s with 1 own ship vs 0 seen; 0.06 at 142 s with 0 seen; ≤ 0.15 all battle.
- Cause: `EstimateUnseenFleetInvestment` (`EnemyStrategicContextBuilder.cs:1479`): before any hostile ship is seen, `lastSeen` = battle start → hostile income × min(elapsed, 90 s) × 0.5 + `UNSCOUTED_FLEET_INVESTMENT 1200`; plus `fleetRaw /= _hostileEconomyPressure`.
- Effect: "Hostiles near the fleet outmatch it" ×58, Hold/EngageLocal instead of capture.

### X4 — No centre (P0)
- Centre objectives: pad 8 at 62.6 s (groups 3/4) → `invalid:blockedApproach` 71.8 / 75.4 s → Hold; site 0 at 112.6 s → `blockedApproach` 137.7 s.
- `blockedApproach` = the centre form of `unsafe` (`EnemyStrategicContextBuilder.cs:1149`), driven by X3.
- No AI satellite at the centre the whole battle (satellites: home pad 13 at 9 s, top-left pads 16 / 15 at 75 / 132 s).

### X5 — Expansion and sabotage thin (P1)
- Mining: home site 12 mine 57 s; top-left pads 16/15 (71/82 s), mine 14 later dropped `unsafe` (151.8 s); bottom-right sites 19/17 targeted 86–104 s. Second site mine started 90 s. No centre mines.
- Sabotage: RaidStructures 6 decisions ("raids an undefended structure" ×3).

## Fixes (reopened)
- Detailed fix plan → [[Done/Bugs/Enemy_AI_Economy_Strategy_Regression_Fixes]] (causes with source lines, changes, tests, verification; implemented 2026-11-19, pending next UltraHard battle).
- [x] **X1** Rear deployment must always resolve: try rear → station sides → home zone → forced spawn after the bounded hold (8 s) on every path; never keep finished ships in the queue for minutes. Add a test: threats around the station still deploy within the hold limit.
- [x] **X2** Replace "never idle" with "never idle **after** saving for the chosen unit": pick the best affordable-soon ship for the level/role and save for it; cheapest-ship buys only for the opening corvette and real emergencies. Cap level-1 corvettes (C-9979, Patrol Frigate) to ≤ 1–2 alive at faction level ≥ 2.
- [x] **X3** Unseen estimate: no unscouted penalty in the opening (before the first hostile contact use 0, or only scouted mines); cap the unseen share (e.g. ≤ the AI's own fleet value); do not divide by economy pressure twice. Opening captures/centre never blocked by an estimate alone.
- [x] **X4** Centre is the opening priority: `blockedApproach` only for seen hostile ships/defenses stronger than the group (R10 strength), not for the unseen estimate.
- [x] **X5** Re-check E1 (every safe mining zone) and sabotage after X1–X4; record per-zone ownership timeline in the next battle.
- [x] Tests: deployment under station threat (X1), production saving (X2), unseen estimate in the opening (X3), centre not blocked by estimate (X4). Categories `EnemyAi`, `Ships`, `Economy`.


## Goal
- AI plays the economy game: owns the centre plus every safe mining zone, keeps building (mines, satellites, defenses, levels, research, ships) and only assaults the human base when it is decisive.
- Follows [[Done/Bugs/Enemy_AI_Active_Play_And_Ship_Combat]] (user 2026-11-19: huge improvement; sabotage perfect; centre retake amazing).

## User rules (2026-11-19, final)
- Capture **all** mining on the map except mining near hostile structures (near the enemy base). One empty outer zone left for 1–2 min is a bug.
- After the centre is captured/retaken: **do not push the enemy station**. Heavy cruisers barely hurt a station; the human still has money (3 mining spots) and ship production → the AI fleet dies there.
- Instead: hold the centre; rebuild all mining and satellites; level up; research; build ships; build defenses (different defend platforms, staggered, not all at once); sabotage; retake remaining zones. Keep the build queue running all the time.
- Zone + mining on the same spot = reinforcement deployment + mining + defenses = money = the main advantage.
- Second station: allowed again at **low priority**, not early. Build it when the main station is dying/about to die, or late with surplus money. Place it on an empty safe spot found by patrol, not next to the main zone.
- Station income research (`IncreasedStationIncome`): high priority.

### Battle flow (user 2026-11-19, final)
1. Early: take as many zones as possible; build mining, satellites (rules above).
2. Take the centre and keep it.
3. Build defenses and level up.
4. Retake or take all remaining zones. If that is not possible → sabotage: kill hostile mining facilities.
5. Base attack only when the assault gate below is met.

### Assault gate (user 2026-11-19, final)
- AI station at level 4 and the attack fleet is level-4 ships (capital ships).
- Reinforcement plan: more ships keep arriving during the attack (production + held reserve sent in waves).
- Zone control: AI holds more than 50 % of the zones (not all needed — the human retaking zones will interrupt the attack).
- Money override: a lot of money + a big fleet can start the attack even when the human has more mining facilities.
- Otherwise: stay in the flow above (hold, build, retake, sabotage).

## Evidence — battle 2026-11-19 12:39 (UltraHard, Separatist AI, code `529094e87`)
Files: `BattleTelemetry/Loading_20261119_123927_ai.csv`, `_ai_orders.csv`, `_squadrons.csv`, `Battle_20261119_123930_map.csv`, `Battle_20261119_124037.csv`; Editor.log `Difficulty=UltraHard`.
- Log time 20.7 s = battle 0 s. Battle ≈ 1,108 s (longest so far). AI station (−1214, −1262) bottom-left; human Republic (1214, 1262) top-right; AI home zone 2 (−443, −1262); human home zone 1 (308, 1262).
- Outer mining zones: zone 3 (1590, −1466) = mine 14 + pads 15/16, 2,804 from the AI; zone 4 (−1590, 1466) = mine 17 + pads 18/19, 2,729 from the AI.

### Worked (keep)
- `IncreasedProduction` I at 0 s, II at 86 s. Combat research from 40 s (ImprovedDefenses, ImprovedWeapons, EnhancedEngines, ReinforcedStructures).
- Ships during the level-2 upgrade: Patrol Frigate 10 s, C-9979 15 s, Patrol Frigate 26 s.
- Centre: satellites 45/48/57 s, retaken later (satellites 451–457 s, 4 centre mines 478–517 s).
- Decisions: EngageLocal 107, CaptureZone 90, RebuildFleet 42, HuntFleet 16, Raid 3, AssaultBase 3. No retreat collapse.
- `IncreasedStationIncome` is bought right when it unlocks: I (data `AvailableLevel` 4) at 423 s ≈ 15 s after level 4; II (level 5) at 643 s ≈ level 5 done.

### Mining zones (E1)
- Zone 3: AI created a new capture group for pads 15/16 **8 times** (58–146 s: groups 5, 6, 8, 9, 11, 13, 14, 16); each dropped `invalid:unsafe` (103.6, 135.1, 248 s). Zone was empty until 120 s (then 2 human squadrons); first AI unit there at 240 s. Mine 14 targeted once (900 s). Never owned.
- Zone 4: groups 64–128 s (sites 19/18/17); human had 1 unit there from 60 s. AI satellites on pad 19 only at 592–790 s; mine 17 retargeted 584–917 s.
- By ≈ 4 min the AI held home + centre only.

### Post-centre push (E2)
- 420–499 s: group 23 flips CaptureZone ↔ HuntFleet (8 switches); 523 s AssaultBase (`localRaw=100`, nothing visible).
- HuntFleet at 430 s: own 10 vs enemy 1 seen, `fleetAdvantage 2.64`; 455 s: own 9 vs 0 seen, advantage 22.68 (R11 economy term not enough).
- 552–767 s: ≈ 27 AI ships lost, mostly 150–900 from the human home zone and 600–1,250 from the human station. Own ships 12 (607 s) → 2 (785 s).

### Defenses (E4)
- One defend platform (Xq6 at 33 s) for the whole 1,108 s battle.

### Purchases / spawns (E5)
- Level-5 counter picks of level-1 corvettes: Patrol Frigate 1,062.7 s + C-9979 1,063.7 s (`counter=1`, `critical=False`, money 7.5k).
- Normal deployments spawn on the home-zone side, 650–990 from the station (866–1,028 s); 298 `deploymentBlocked` at minutes 17–18.
- Max money at a purchase 20,641 (median 3,730).

## Implementation

### E1 — Expansion

- [x] Assign one persistent detachment to each eligible outer mining zone. Rank open objectives by travel ETA and remember the zone while its group waits for reinforcements.
- [x] Size contested capture groups from sampled hostile strength plus one ship-equivalent. Check known route threats, static defenses and clearance before moving. Reserve assigned zones across groups.

### E2 — Consolidate and assault

- [x] Guard the secured centre, support expansion and use wings or a small ship group to raid exposed hostile income.
- [x] Require a level 4+ station, at least three level 4+ capital ships, a held reserve and capital production, and a strict majority of uncontested capturable zones. The surplus exception also requires clear fleet advantage. Reevaluate each decision.
- [x] Send paid reserves in waves during the assault. Keep ships outside the strike group on centre duty.

### E3 — Unseen fleet

- [x] Add estimated unobserved reinvestment to remembered combat strength. Use known mine claims and last observed effective mining income, including production research. Use the estimate for hunting and assault.

### E4–E7 — Production

- [x] Place one defend platform per safe held zone where space permits, centre and captured relays first. Prefer a type with fewer copies and space purchases by 20 s.
- [x] Look for an affordable useful purchase when the preferred category is blocked. Protect higher-priority spending while production is active. Reject level-1 corvettes at faction level 3+ and launch normal reinforcements behind the station.
- [x] Allow one backup station after the opening for imminent station loss, or from 480 s at twice its price after upgrades and research. Place it at a safe patrol-known point at least 1,400 units from home and its relay. Prefer owned zones.
- [x] Prioritize `IncreasedStationIncome` on unlock at levels 4 and 5. Keep the existing unlock data.

## Verification

- Runtime and Editor assemblies compiled with the Unity Bee response files and standalone Roslyn. Reviewed EnemyAi, Capture, Economy, Ships and the affected Core composition fixture. Tests and playtests were not run.
- User acceptance → [[TODOs/User_Acceptance_Queue#Enemy AI Economy Strategy]].
- Reusable rules → [[Architecture/Enemy_AI_Objectives]].

## Order
1. E1, E3 (economy picture), E2 (strategy posture).
2. E5, E4.
3. E6, E7.

## Files
- `Services/Enemy/EnemyShipTaskGroupPlanner.cs`, `EnemyStrategicContextBuilder.cs`, `EnemySiteObjectives.cs`, `EnemySquadronCaptureAssignments.cs`, `EnemyProductionStrategy.cs`, `EnemySiteConstructionController.cs`
- `Entities/EnemyFaction/Models/EnemyStrategicDecisionModel.cs`, `EnemyProductionDecisionModel.cs`
- `Entities/EnemyFaction/Controllers/EnemyFactionController.cs`, `Services/ShipSpawning/ShipSpawnPoints.cs`
- `Services/CaptureSites/ICaptureSitePresence.cs` (strength API)

## Tests
- Categories: `EnemyAi`, `Capture`, `Economy`, `Ships`. No stat-pinning asserts. Not run by agents (local override).
