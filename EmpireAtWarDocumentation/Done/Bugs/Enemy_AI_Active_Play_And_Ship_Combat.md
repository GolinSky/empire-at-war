---
category: Bugs
status: done
created: 2026-11-19
replaces: "[[TODOs/Bugs/Enemy_AI_UltraHard_Followup_And_Ship_Idle]]"
after: "[[Done/Features/Spatial_Pace_Clusters_Plan]]"
completed: 2026-11-19
acceptance: pending-user
---
# Enemy AI Active Play And Ship Combat

## Reopened 2026-11-19 (acceptance failed)
- `a945072cc` (F1–F14) played 2026-11-19 10:45 on UltraHard: human win ≈ 551 s. User: AI slow, comes too late, bad corvette purchases, one-by-one spawns in front of the station, no held reserve, own fleet pushes on its own.
- [[Done/Features/Spatial_Pace_Clusters_Plan]] landed in `4cc4c5a3e`. The later R-items supersede the original level-1 purchase limits and shared post-kill retargeting.
- Completion history: F1–F14 implemented in `a945072cc` (outcome section kept below).

## Evidence — battle 2026-11-19 10:45 (UltraHard, code `a945072cc`, dirty)
Files: `BattleTelemetry/Loading_20261119_104527_ai.csv`, `_ai_orders.csv`, `_squadrons.csv`, `Battle_20261119_104532_map.csv`, `Battle_20261119_104702.csv`; Editor.log `Difficulty=UltraHard`.
- Log time 26.8 s = battle 0 s (first human squadron `start` and AI station both at 26.8). Times below are **battle** seconds.
- Map: Small Coruscant, seed `2004124536`. Human station (−1238, −1311), AI station (1238, 1311), AI home zone 2 (467, 1311), top-left zone 3 (−1585, 1556), centre point 2 (mines 0–3, pads 4–9).

### What worked
- 5 `CommonMiner` at 0.1–4.1 s, all within ~300 of the station. Level 2 bought 6.1 s. 1 Patrol Frigate + 6 Vultures at level 1.
- Home site 12: ship 91 arrived 35.6 s → owned 46.3 s → mine started 48.1 s. Top-left: Vultures arrived 75.8 s → pads 15/16 owned 78.6/80.6 s, mine 14 owned 92.7 s → mine started ≈ 96 s. **Captures are fast once units arrive (≈ 3–11 s); the delay is travel and target choice.**
- Removals confirmed in source: no `TryBuySecondStation`, `UpdatePursuit`/`ObserveHealth`, `forwardStationFallback`. No retreat collapse: decisions EngageLocal 138, CaptureZone 30, DefendBase 23, RebuildFleet 14, Raid 7.
- Sabotage up: AI → human structures 21.9k hull / 8 kills (01:11: ≈ 10k / 3).

### AI too slow to the centre
- Human squadrons at the centre from 30 s; human ships spawn at the centre from ≈ 115 s.
- AI: no ship within 550 of the centre until after 260 s. First AI squadron near the centre at 70 s.
- Level 2 build = 80 s (6.1 → 86.1 s). During it the AI bought **nothing** (no purchases 15–86 s); money rose to 9,221 at 86 s. Cause: level-1 rules allow 1 corvette + `OPENING_SQUADRON_TARGET = 6` squadrons only, and `canBuyOpeningShip` needs `!IsUpgradingStation` (`EnemyProductionStrategy.cs:190–203`).
- First AI ship 91: home mine → top-left (3,000 units, 46–98 s); 102.8 s AttackMove to the centre; 111.8 s retargeted to (−1700, 300) and wandered the west side ((−1466, 529) at 126 s).
- Second AI ship (114) only at ≈ 85 s; main fleet spawns 110–160 s.

### Purchases / corvette bug
- Start: OK (1 corvette).
- End (440–518 s, station critical): ~20 squadrons in 23 s (`counter=1`, `critical=True`, one per second); money falls to ≈ 1,100–1,700; then 6 Patrol Frigates + 4 C-9979 at faction level 4 (`Counter=False`, `critical=True`, e.g. `etaSeconds=16.8;timeToLoss=71.6`).
- Mechanism: `IsStationCritical` → `Evaluate` returns Ship; the critical counter pick buys squadrons until the wallet drains; then the only affordable ship that arrives before `timeToLoss` is a level-1 corvette.
- Mining ended 3/5: 2 lost miners never rebuilt.

### Research
- Mining-income research = `IncreasedProduction` (`ResearchStat.Income`): I = ×1.2, 250, level 1; II = ×1.4, 500, level 2.
- Bought: I at 89 s, II at 99 s (after level 2), `IncreasedStationIncome` at 413 s. **No combat research the whole battle.**
- Tier I was affordable from 0 s but waited 89 s while 9k sat in the wallet.

### Reinforcements and spawns
- Reserve = `MINIMUM_REINFORCEMENT_GROUP = 2` or `LONE_REINFORCEMENT_WAIT_SECONDS = 12` (`EnemyFactionController`); `holdReserve` false while `IsStationCritical`. Deployments came in pairs, never as a held armada.
- UltraHard commits the whole fleet (`committedFleetRatio 1`); nothing is kept back.
- Station siege 245–480 s: 221 `deploymentBlocked` (all `homeDefense=True`, rally = station), 27 `deploymentForced` → single ships at the home zone (x 230–690, z 1080–1540), 550–1,000 in **front** of the station toward the enemy. They died one by one.

### Human fleet pushes on its own
- 302.2 s: one player Attack order on AI ship 141 for 8 ships at the centre (z ≈ 270–600).
- Then 10+ automatic group retargets (306.9, 311.9, 319.6, 334.8, 349.1, 354.9, 360.4, 370.6, 374.7, 377.0 s): `complete → Attack` on a new target, same target for the whole group (`ShipOrderRunner.FindReplacement` → `GroupPace.SharedReplacement` ≤ 3,000, team contacts ≤ 1,500 around the dead target).
- Each new target was an AI reinforcement spawning farther north; ships moved 300–666 units forward without a player order (138: (−23, 273) → (311, 849); 161: z 477 → 1018).

## Evidence — battle 2026-11-19 11:42 (UltraHard, code `4cc4c5a3e`, Spatial Pace Clusters landed)
Files: `BattleTelemetry/Loading_20261119_114201_ai.csv`, `_ai_orders.csv`, `_squadrons.csv`, `Battle_20261119_114204_map.csv`, `Battle_20261119_114333.csv`; Editor.log `Difficulty=UltraHard`.
- Log time 19.8 s = battle 0 s. Times below are battle seconds. Human win ≈ 578 s.
- Map: Small Coruscant, seed `-906806385`. AI (Separatist) station (−1265, 1210) top-left; human (Republic) station (1265, −1210) bottom-right. AI home zone 2 (−494, 1210); outer zones 3 (1189, 1609) and 4 (−1189, −1609).
- User: much better overall; AI survived to level 4 and built Providence/Recusant. All other bugs remain.

### Better
- No retreat collapse: decisions EngageLocal 109, CaptureZone 66, RebuildFleet 18, Raid 3, Hold 1, DefendBase 1.
- Levels follow data build times (`StationLevelData`: L2 80 s, L3 120 s, L4 180 s, L5 220 s): L2 bought 6.2 → done 86.2; L3 88.5 → 208.5; L4 212.9 → 392.9; L5 bought 404.1. Recusant from 224 s, Providence 421 s.
- Captures fast after arrival: home site 12 owned 42.4 s; outer zone 3 pads 15/16 66.8/72.3 s, mine 14 82.9 s.

### Still wrong
- Opening idle: after 5 miners (0.1–4.2 s), Patrol Frigate 5.2 s, level 2 6.2 s and 6 Vultures (7–15 s), **no purchase until 87.5 s** (level 2 done 86.2 s). Same as 10:45.
- Mining research late again: `IncreasedProduction` I 90.5 s, II 100.5 s. No combat research all battle; `IncreasedStationIncome` 405 s.
- Centre: human squadrons there from 20 s (4–6 for the whole battle). AI squadrons from 60 s (8–9 at 100–120 s); AI ships near the centre: one ship at 160 s only. AI centre objectives start 92.9 s.
- End-game panic again (station critical, 434–504 s): ~20 squadrons one per second (Droid/Hyena), then 6 C-9979 + 6 Patrol Frigates at faction level 4–5 (465–555 s).
- Spawns: 162 `deploymentBlocked` (minutes 6–9), 19 `deploymentForced` single ships 514–951 from the station (home-zone side, toward the enemy).
- Player fleet drift remains: 143 automatic replacements ("Attack target destroyed; checking replacement contacts"); ships drifted 496–723 after ≥ 2 auto retargets (117 at 118–180 s, 155 at 291–336 s, 216/218/221 at 539–577 s).

### Build queue (answer)
- `EnemyBuildQueue` uses the player's per-ID pipelines: up to `PlayerFactionModel.MAX_ACTIVE_PIPELINES = 7` unit types in parallel; copies of the same type build one after another (FIFO). Same as the player. The 6 opening Vultures finished one by one at 20, 31, 31, 47, 47, 114 s.
- Not a queue bug; the opening gap is the level-1 purchase rules (below).

## Evidence — battle 2026-11-19 11:57 (UltraHard, AI = Republic, code `4cc4c5a3e`)
Files: `BattleTelemetry/Loading_20261119_115746_ai.csv`, `_ai_orders.csv`, `_squadrons.csv`, `Battle_20261119_115748_map.csv`, `Battle_20261119_115906.csv`; Editor.log `Difficulty=UltraHard`, `Player1:Republic`.
- Faction swap (R8): human Separatist (1235, −1016) bottom-right; AI Republic (−1235, 1016) top-left. Log time 13.9 s = battle 0 s. AI station lost ≈ 513 s.
- Map seed `1318518259`. Outer mining zones: zone 3 (−1439, −1578) = mine 14 + pads 15/16; zone 4 (1439, 1578) = mine 17 + pads 18/19. AI distance: zone 3 ≈ 2,620, zone 4 ≈ 2,750.

### Mining zones (user: AI took one outer zone, left the other)
- AI took zone 3 with one V19 squadron: pads 15/16 at 50–70 s, satellites 60 s / 84 s, mine started 75 s.
- AI never sent anything to zone 4. It was empty until human Vultures arrived at 100 s (2–3 squadrons held it all battle). Only AI objective there: site 19 at 298 s, dropped 1 s later (`ownedOrRemoved`).
- User rule: the AI goes to **every mining zone** except where hostiles are near the mining structures.

### Centre contest (user: one AI squadron vs two human squadrons)
- Centre pads 8 and 7 taken early by single V19 squadrons (satellites 42 s / 45 s).
- Single AI squadrons kept contesting pads against stronger human presence: pad 9 at 70 s (AI 1 V19 vs 2 Vultures); pad 8 at 140 s (1 vs 1); pad 9 at 210–220 s (1 ARC-170 vs 2 Belbullab-22); pad 7 at 360–370 s (1 vs 6).
- The capture system already sums strength per team (`CaptureStrength`: `LeadingPlayer`, `OpposingPlayer`, `Advantage`, `PresentTeamCount`; squadron weight `SquadronCaptureWeight = 0.5`, ship = 1). The AI only sees `ICaptureSitePresence.HasHostileUnits(site, owner)` (bool).

### Opening reserve held the first fleet (R3 regression)
- Bought: 5 miners 0–4 s, CR70 5 s, level 2 6 s, 6 V19 7–21 s; then nothing until 82 s (level 2 done ≈ 86 s). Same opening idle as the Separatist AI.
- Only 2 V19 launched early (19 s). The CR70 and 4 V19 sat finished in the reserve until **149 s**, released together with the first Acclamator/Pelta (`HoldFinishedReinforcements = true`, `EnemyUnitCommander.cs:114`).
- AI had **0 combat ships until 149 s** ("No combat ships are available" 0–147 s).
- Mining research: `IncreasedProduction` I 107 s, II 117 s (late again).

### Attacks with too few ships; no economy in the strength estimate
- 162–257 s: 5–8 AI ships vs 4–9 human, `fleetAdvantage` 0.15–0.25, still engaging; 269 s down to 2.
- 291–317 s: `CaptureZone` with 1–5 ships while 10 human ships are known (`fleetAdvantage` 0.04; local 100 = nothing visible nearby).
- `fleetAdvantage = CombatMatchup.Advantage(_ownTeamForce, _focusTeamForce)` (`EnemyStrategicContextBuilder.cs:1267`): units seen/remembered only. No economy: owned mining sites, spawned miners, income, research.
- Human held zone 4 + centre pads + home: more income than the AI, invisible to the AI's estimate.

### Same as the Separatist AI
- End-game: squadron flood 445–474 s (V19/NTB-630/Y-Wing, one per second), CR70 panic buys 462 / 479 s.
- No combat research; `IncreasedStationIncome` 409 s.
- Faction has no visible influence on the faults.

## User decisions 2026-11-19 (after 11:42)
- **Remove the level-1 production limits completely**: the 1-corvette rule, `OPENING_SQUADRON_TARGET = 6`, `allowShips` at level 1, and the `!IsUpgradingStation` gate on opening ships. Reason: a level-1 bomber rush would kill or cripple the station while the AI waits.
- **Mining research (`IncreasedProduction`) is one of the first actions**: tier I with the 5 miners (250 credits, level 1); tier II as soon as level 2 completes.
- Next test battles: user plays Confederation (Separatist), AI plays Republic, to check faction influence.

## User decisions 2026-11-19 (after 11:57)
- The AI goes to all mining zones, except where hostiles are near the mining structures.
- The AI needs a capture API that returns the other/hostile side's capture strength at a site.
- The AI's strength estimate includes economy: mining spots held by each side → money → research and fleet.

## R-items

### R1 — Opening speed (P0)
- [x] Production never idles during a station upgrade: keep buying squadrons, research and the next ships while level 2 builds.
- [x] Remove the level-1 production limits completely (user 2026-11-19): 1-corvette rule, `OPENING_SQUADRON_TARGET = 6`, `allowShips` at level 1, `!IsUpgradingStation` gate on opening ships (`EnemyProductionStrategy.cs:186–203`). Normal production rules from 0 s.
- [x] `IncreasedProduction` I is one of the first actions, bought with the 5 miners (≈ 0–5 s); II as soon as level 2 completes (11:42: 90.5 / 100.5 s; 10:45: 89 / 99 s).
- [x] First ships/squadrons go to the centre and the neighbour zone first; the far outer zone comes after (ship 91 spent 46–98 s crossing to the top-left).
- [x] No objective flip away from the centre once ordered (102.8 → 111.8 s retarget to the west).

### R2 — Corvette / critical purchase bug (P0)
- [x] Station-critical production: no squadron flood (one per second until the wallet is empty); keep money for a real ship.
- [x] No level-1 corvettes at faction level ≥ 3 as panic buys; use the reserve (R3) instead.
- [x] Rebuild lost miners while mining < 5, also under pressure (ended 3/5).

### R3 — Real reinforcement reserve (P0)
- [x] Hold finished units in a reserve; do not commit everything (`committedFleetRatio 1` on UltraHard).
- [x] Attack with the existing fleet; spawn the reserve as a group to reinforce the attack at the right moment, or keep it to defend if the attack fails.
- [x] Under station siege: release the reserve as one group, never drip single ships.
- [x] The reserve never holds opening units: 11:57 kept the CR70 + 4 V19 finished from ≈ 20 s to 149 s; the AI had 0 ships on the map until 149 s.

### R4 — Safe spawn point (P0)
- [x] Spawn behind the station: the side away from the threat centroid, inside the station's guns. Not the home-zone point in front of the station.
- [x] Same rule for forced deployments (`deploymentForced`).

### R5 — Research (P1)
- [x] Mining income first (R1); then combat research at regular points (none bought in 551 s).

### R6 — Player fleet must not walk away (P0, ship orders)
- [x] Automatic replacement after a kill stays where the player placed the ships: the new target must be within weapon/radar reach of the current position (or a leash to the order position), not a group-shared contact up to 3,000 away.
- [x] If nothing is in reach, the ships stay; no chain of chases toward new spawns.
- [x] Re-check against Spatial Pace Clusters before coding (same attack-move code).

### R7 — Raids (P1)
- [x] Spread raids over targets (all 3 squadron raids hit site 111 in zone 4); include the human home mines and spawned miners.


### R8 — Faction swap check (P1)
- [x] Analyse the next battles with the AI as Republic (user plays Confederation); compare opening, centre arrival, production and end-game panic with the Separatist AI battles above. Done 11:57: same faults (opening idle, late mining research, reserve stall, squadron flood + corvette panic); no faction influence.

### R9 — Every mining zone (P0)
- [x] Capture targets include every mining zone (outer zones, centre, home) whose mining structures have no hostiles nearby; not just the nearest one.
- [x] Send units early, while the zone is empty (11:57: zone 4 was empty until 100 s and the AI never went).

### R10 — Capture strength API (P0)
- [x] Expose per-site capture strength to the AI: own team vs strongest hostile team (from `CaptureStrength` / `CaptureStrengthSampler`), e.g. on `ICaptureSitePresence`.
- [x] Capture assignment sends more strength than the hostile side already in the circle (ship = 1, squadron = `SquadronCaptureWeight` 0.5); never one squadron into two.
- [x] Lost or tied contest → reinforce or pick another site; no single-squadron trickle.

### R11 — Economy in the strength estimate (P0)
- [x] Strength estimate adds economy: mining sites and spawned miners each side holds (seen or remembered), income, research tier.
- [x] Higher hostile economy → expect a bigger hostile fleet than seen; raise the force needed to attack/capture.
- [x] No attack/capture with 1–5 ships into a known 10-ship enemy (11:57, 291–317 s).

## Acceptance 2026-11-19 12:39 (UltraHard, `529094e87`)
- Passed (user): huge improvement; sabotage works; centre capture/retake works; mining research I at 0 s / II at 86 s; ship production during the level-2 upgrade; combat research from 40 s.
- Failed → [[Done/Bugs/Enemy_AI_Economy_Strategy]]: outer mining zones not taken (zone 3 never, zone 4 late); base push after the centre lost ≈ 27 ships; level-1 corvette counter picks at level 5; normal spawns still on the home-zone side.
- EditMode gate remains in [[TODOs/User_Acceptance_Queue#Enemy AI Active Play and Ship Combat]].

## Reopened scope outcome

- R1/R2/R5: mining income I with the opening miners and II once level 2 unlocks. Normal combat production continues through upgrades. Critical spending replaces affordable miners, prefers timely ships, caps squadron purchases, and excludes starting-tier corvette panic buys at level 3+.
- R3/R4: early reinforcements deploy as they finish. Reserve waves support active tasks or station defense. Siege ships reserve clear rear landing slots before any deployment. Forced launches keep rear geometry and still check visibility, neutral blockers, hazards, hull fit and reachability.
- R6: reviewed `4cc4c5a3e` cluster behavior. Automatic direct-Attack replacements require personal weapons reach, hold position and expire on loss of reach. AttackMove retains its destination. Saved transient replacements restore as idle.
- R7/R9/R10: surplus raid wings use distinct remembered mines. Capture wings cover centre, neighbour and other safe mining areas. Per-team strength comes from cached circle samples. A tied or losing assignment needs additional wings or redirects.
- R11: apply observed/remembered mining claims and mine income including the production research multiplier to fleet advantage and route safety. Known 2:1 warship opposition blocks voluntary attacks and contested routes.
- Verification 2026-11-19: standalone Roslyn compilation of the game and Editor fixture assemblies succeeded against the existing Unity references. Reviewed affected production, reserve, spawn, capture, raid, save and ship-order assertions. Tests, Play Mode and gameplay acceptance were not run.
- Acceptance → [[TODOs/User_Acceptance_Queue#Enemy AI Active Play and Ship Combat]].
- Reusable rules → [[Architecture/Enemy_AI_Objectives]], [[GameDesign/Combat#Attack target destroyed]].

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
- Level 1 follows normal ship and squadron production limits. The later user decision removes the original one-corvette rule.
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
