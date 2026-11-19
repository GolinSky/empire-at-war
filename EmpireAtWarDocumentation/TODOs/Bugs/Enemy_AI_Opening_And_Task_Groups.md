---
category: Bugs
status: in-progress
created: 2026-11-19
follows: "[[TODOs/Bugs/Enemy_AI_Economy_Strategy]]"
---
# Enemy AI Opening and Task Groups

## Goal
- AI acts from second 0: wings fly out while the first miners are bought; every ship has a task; no waiting fleet.
- Keep what worked on `529094e87` (every ship captured at once, centre retaken). Adjust current code; no copy of old code.
- Evidence → [[TODOs/Bugs/Enemy_AI_Economy_Strategy]] (battles 20:45, 21:11).

## User rules (2026-11-19, final for this plan)
### Opening (0–30 s)
- Wing orders happen at 0–6 s, together with research / miners / level 2. Not at 20–36 s.
- Station wings are used at once: **1–2 wings → own-side outer zone, all others → centre**.
  - 2 wings to the outer zone when wings are cheap (CIS); 1 when wings are expensive (Rebellion) or money is short.
- Centre wings: take free pads on the **base-side edge**, build **2–3 satellites**, a mine if possible. A wing that dies after 3 satellites are built is a win.
- Outer-zone wings: capture a satellite pad first and build it, even if the wing dies. Then respawn wings and take the relay and the mine under the satellites (2 turbolaser, or laser + turbolaser).
- First buys: **fighter + interceptor** wings. No bombers at the start.
- **No corvette in the first 30 s.** Wings capture; money goes to satellites and mines first. Then 1–2 corvettes against enemy wings.
- **No level-1 corvette after level 2 is reached.** No exception.
- The strongest ship goes to the centre. No ship sits beside the station.

### Wings at a contested target
- The centre zone is big. A wing does not leave it: it takes another free pad, baits, comes back.
- Wings capture pads, mines and relays. They are not pulled off to escort.

### Centre priority
- The centre is **never excluded**. It loses priority when the AI is weak.
- It gains priority with: bigger fleet + reinforcements + economy (≥ 1 outer zone + home mine).
- With own satellites or a mine at the centre the AI **fights around them with a small force**: own satellites count as offensive defence.
- If the enemy moved its ships to the outer zones → the centre is weak → take it.

### When weak or behind
- Patrol all the time: intel and bait.
- Attack an outer zone. Enemy reinforcements arrive → retreat, deploy own reinforcements near the **other** outer zone, attack there. Repeat. Capture and hold.
- Sabotage all the time: kill mines.

### Unchanged
- Flow: zones → centre → defenses and levels → remaining zones or sabotage → base only at the assault gate.
- Ships do not wait. Task force groups as before. One or two wing groups that act.

## Capture check (source, 2026-11-19)
- **Correction 2026-11-19 22:40:** the radii below were read reversed. `CaptureSiteKind.Mining = 0` → radius **60**, 10 s per net ship. `Satellite = 2` → radius **500**, 6 s. One wing = 0.5 ship → a pad in 12 s from anywhere within 500; a mine in 20 s, only inside its 60 circle.
  - Centre layout (22:29 map): mines ≈ 270 from the centre, pads ≈ 470. A group standing **on a mine spot** captures that mine and 2–3 pads at once (user: "a couple of seconds").
  - Steps built on the wrong radii: one wing per pad, the 400 "hold near a pad" move order, mines after pads. Replaced by round 2 (B1).
- ~~Capture works. `CaptureSiteData.asset`: pad radius 60, 10 s per net ship; mining site radius 500, 6 s.~~
- AI builds on an owned site within ≤ 3 s (`EnemySiteConstructionController`, 3 s tick). 21:11: pad 7 owned 45 s → satellite 45 s; pad 6 owned 52 s → satellite 54 s.
- Satellites: 300–1,600 credits, 21–47 s build. Mine 25 s.
- Gate: no site construction at level 1 until the miners are bought and the level-2 upgrade has started (`OpeningMiningPending`, `OpeningLevelReserve`). 21:11: open from 6 s.
- Risks found:
  - Pad radius 60 + `AttackMoveTo`: a wing that engages leaves the circle → capture stops. Needs a hold-inside-circle order for capturing wings.
  - "Wing at the centre switched to escort": `AssignEscorts` takes any anti-fighter wing when a hostile wing is within 2,000 of any AI ship. Not a capture fault.
  - Station wings: `IsHangarLaunched && Guard → skip` → never ordered.

## First 3 minutes — battle 21:11 (AI Separatist vs human Republic)
| Time | AI | Human |
|---|---|---|
| 0–6 s | `IncreasedProduction`, 5 miners, level 2 upgrade | — |
| 1–13 s | Station launches 5 Vultures → all Guard the station | Station launches 3 ARC-170 |
| 7–10 s | Buys 3 Vultures, Patrol Frigate; money 548 | 11–14 s: station wings → centre |
| 15 / 23 / 31 s | Bought Vultures appear one by one | 18–28 s: 4 bought wings → outer zones |
| 20 / 28 / 36 s | First wing orders: centre, own outer zone, far outer zone | — |
| 25–52 s | Patrol Frigate: home mine → centre pad; C-9979: centre pad; 2nd Patrol Frigate stays near home | 44 s centre relay |
| 48–51 s | Wings switched to escort | 70 / 93 s outer relays |
| 50–95 s | 7–8 of 8 wings escort | 6 mines by 94 s |
| 154–206 s | 9 ships at own-side zone → 2 | reinforcements dropped there |

## Issues and fixes
### Opening
- **I1 (P0) Station wings never leave.** Fix: station-launched wings are free wings, ordered when they launch (1–13 s). Only wings of carrier ships stay with their carrier.
- **I2 (P0) First useful order at 20 s.** Fix: opening split at the first decision — outer-zone group (1–2 wings, by wing price and money) → own-side zone pad; centre group (all others) → base-side pads.
- **I3 (P0) Wings turned into escorts.** Fix: group A/B wings are never escorts. Escorts come only from carrier hangar wings and from corvettes.
- **I4 (P0) Wings leave or avoid a contested target.** Fix: inside a zone a wing picks the best **free** pad or mine (base-side first); contested → next free site in the same zone; no free site → bait at the edge, return. The power rule decides only where **new** wings go.
- **I5 (P0) Capturing wing drifts out of the pad.** Fix: capture order keeps the wing inside the circle until the site flips (move, not attack-move, for the last leg).
- **I6 (P0) Satellites first.** Fix: every pad a wing takes gets a satellite at once (already ≤ 3 s); opening money is reserved: satellites and mines before any ship.
- **I7 (P0) First buys.** Fix: fighter + interceptor wings (two types build in parallel). No bomber before the first mine is raided. No corvette before 30 s and before the first satellites and mines are paid.
- **I8 (P0) Corvettes.** Fix: level-1 corvettes only at level 1, max 2, never from level 2. (Emergency fallback already deleted.)

### Wing groups
- **Group A — centre.** Pads (base-side) → mines → relay. Stays, retakes, rebuilds satellites.
- **Group B — outer zone, then sabotage.** Own-side pad → relay → mine; then the other outer zone; no free zone → raid mines (bombers join here).
- Respawn: a lost wing is rebought and rejoins its group.

### Ships
- **I9 (P0) Fleet waits.** Fix: no idle reserve. Each ship gets a task in the cycle it deploys:
  1. Free site or zone → nearest free ship at once.
  2. Zone the free ships beat → they go together.
  3. Nothing beatable → weak-phase loop (below).
- **I10 (P0) Strongest ship sits at home.** Fix: the most expensive free ship takes the centre task; ships deploy one by one until the first outer zone is held (no waves of three).
- **I11 (P0) Weak force dies at the centre.** Fix: centre priority = base value × strength factor (fleet vs known centre force, held reserve, held outer zones + home mine). Own satellites and platforms inside the zone are added to the AI force. Never removed from the list.
- **I12 (P0) Small zones blocked.** Fix: delete `hostileZoneOnRoute` (route around the centre by a waypoint). Station pressure pauses new forces only while critical.
- **I13 (P0) Winning force recalled.** Fix: a force leaves on its own local fight (≤ retreat line), not on a zone recount. Defender reach = zone radius + 500.
- **I14 (P1) Force destroyed at a hostile relay.** Fix: order inside a hostile zone = satellites → relay → pads; reinforcements seen → retreat (weak-phase loop).

### Weak phase loop (new)
- Trigger: behind on zones or fleet advantage < 1.
1. Patrol ship + patrol wing always out (intel, bait).
2. Task force attacks the weaker outer zone.
3. Hostile reinforcements arrive (local advantage ≤ retreat line) → pull back.
4. Own reinforcements deploy near the other outer zone; task force goes there.
5. Zone taken → satellites, mine, platform → hold.
6. Group B and spare ships raid mines the whole time.
- Centre is checked each cycle: defenders gone → take it.

### Station defence
- **I15 (P0) Defenders chase wings.** Fix: target = hostile ship attacking the station → nearest hostile ship → wing only when no ship threatens; target kept while it is a threat; ships that cannot hurt it hold the line.

### Sabotage
- **I16 (P1) Wing raids 0.** Fix: group B raids with the power rule (wings vs satellites and units at the mine); the blanket "structure near route" block is removed for raids.
- **I17 (P2) Ship raid drags the main force.** Fix: raids are done by spare ships and group B, nearest mine.

### Other
- **I18 (P2)** After the station dies every zone counts as enemy side. Fix: own side from the home zone.
- **I19 (P2)** Backup station bought 750 s (20:45), not alive at 918 s. Not analysed.

## Implementation steps
- [x] **A1 Wings.** `EnemySquadronCommander`: station wings free; groups A/B (`EnemyWingGroup`: members, zone, role); opening split; in-zone site choice (free first, base-side first); no escorts from A/B; capture hold order; group B raids. `EnemySquadronCaptureAssignments` → per-group site choice. `EnemyPatrolSafety`: raid rule.
- [x] **A2 Opening production.** `EnemyProductionStrategy` / `EnemyProductionDecisionModel`: fighter + interceptor first; satellite and mine reserve before ships; corvette rule (level 1 only, max 2, not before 30 s); single-ship deployment until the first outer zone (`EnemyFactionController`).
- [x] **A3 Ships.** `EnemyZoneAllocator`, `EnemyShipTaskGroupPlanner`: no idle reserve; rules 1–3; strongest ship → centre; centre priority factor with own satellites; delete route deny; pressure pause only when critical; local-fight release. `EnemyZoneObjectiveCollector`: defender reach.
- [x] **A4 Weak phase loop.** Planner state: target zone, retreat, switch zone; reinforcement rally at the other zone; permanent patrol; raids.
- [x] **A5 Hostile relay order (I14).** Partial, see Implementation notes.
- [x] **A6 Station defence (I15).** `BuildBaseThreat`, `ExecuteDefense`.
- [x] **A7 Tests (`EnemyAi`).** Station wings ordered at launch; opening split 1–2 / rest; wing stays in zone and switches pad; no escort from groups; no corvette before 30 s or from level 2; free ship gets a task in its cycle; weak fleet prefers an outer zone, centre still listed; own satellites raise the centre verdict; force stays while its local fight is won; defence target prefers ships and is kept. No stat-pinning. Written, not run (local override).
- [ ] **A8 Battle.** UltraHard Small 1v1 CIS vs Republic, then Rebellion vs Empire. User gate.
- [ ] **I18 / I19.** Not started (own side after station loss; backup station timing).

## Implementation notes (2026-11-19, A1–A7)
### Wings
- Station wings: `TrackOwnedSquadrons` → own station nearer than every own ship → tracked as free (`CommandedSquadron` without orders). Else carrier wing (stays on `Guard`). No station → carrier wing.
- Groups: `EnemyWingGroup` + `EnemyWingRole` (`Centre` = A, `Outer` = B). New free wing → B until B has `_outerWingCount` non-bomber wings; bombers always → B; rest → A. Lost wing → next free wing fills the gap.
- `_outerWingCount`: `EnemyFactionController.Initialize` → `SetOuterWingCount(2)` when a Fighter/Interceptor wing costs ≤ 300 (`CHEAP_WING_PRICE`), else 1 (commander default 1).
- Zones: A → `IsMain` zone. B → nearest outer zone (to own station) with any capture target; none left → B raids.
- In-zone choice: `EnemySquadronCaptureAssignments.AssignInZone` → keep own free site; else free site with fewest wings; order pads → (A: mine → relay; B: relay → mine); base-side first. Contested sites skipped; no free site → bait at zone edge toward home; nothing left → hold at centre.
- Capture hold: within `CAPTURE_HOLD_DISTANCE` 400 of a site → `IMoveFacade.MoveTo`, else `AttackMoveTo`.
- Escorts: only carrier wings (`IsHangarLaunched`, ungrouped). Group wings never escort.
- Raids: B members once no outer zone is left; B bombers any time. Power rule `WinsRaid` (wing vs units + armed structures at the mine ≥ 1). `EnemyPatrolSafety` no longer gates wing raids (file unchanged).
### Production
- Level-1 corvette: only at level 1, from `OPENING_NO_CORVETTE_SECONDS` 30 s, max `MAX_LEVEL1_CORVETTES` 2. `CanOrderAnyShip` applies the same rule → before 30 s the squadron fallback buys wings.
- Ships (not wings) wait while `PriorityConstructionPending` (mandatory satellite or mine on an owned site), even with no other production.
- `TrySelectSquadron`: no hero wings; bombers last until a hostile mine is known; fewest reserved copies first (two types side by side), then cheapest.
- Deployment: ships deploy one by one while the team holds no outer zone (`IEnemyAiStateProvider.HoldsOuterZone`, default true).
### Ships
- `EnemyZoneAllocator`: sent force keeps its zone (no retain-line release); leftovers join the nearest force out; no force out → all to `WeakTarget`. Centre force led by the most durable free ship (hull + shields).
- `EnemyZoneObjective.OwnSupport`: own satellites/platforms within 2 × radius; `EnemyZoneEvaluator.Assess` adds them to the force.
- Tier: centre Assemble behind = 4 unless `StrongEconomy` (outer zone + own mine) → 3; centre Go behind = 2.
- Collector: `hostileZoneOnRoute` deleted (no waypoint added; ship navigation paths around). Zone defenders = units within radius + `ZONE_DEFENDER_MARGIN` 500.
- Planner: new force pause only when `StationCritical`. A group sent by the allocator (`AssignedZoneId`) commits to its zone: capture `viable` regardless of verdict; its local fight decides withdrawal.
- Weak loop: zone group in `RetreatValue` → zone marked for `WEAK_ZONE_SWITCH_SECONDS` 90 s, its ships freed; `WeakTarget` = best outer zone by pool advantage, never the centre or the marked zone; reinforcements gather at its staging point.
- Patrol: unchanged (`EnemyPatrolShipController` keeps one patrol ship).
### A5 (partial)
- Inside a hostile zone the relay ranks before pads (`HOSTILE_RELAY_FIRST_BONUS` 1000). Satellites are fought by the attack-move on the way; no explicit satellite-first target.
### Station defence
- `BuildBaseThreat` rank: ship attacking station 0 → ship 1 → wing 2. Previous target (`_defenseTargetId`) kept while it is a threat and a ship (or while no ship threatens).

## Verification (2026-11-19)
- Unity recompile: 0 errors (`Assembly-CSharp`, `Assembly-CSharp-Editor`).
- `EnemyAi` EditMode (user request, 2026-11-19): 533/542 → 9 failures fixed → **542/542 pass** (quick run, explicit tests excluded). Fixes: outer wing count pushed on first `EnemyFactionController.Tick` (not `Initialize`); test catalogs given squadron profiles; evaluator test updated to the new centre rule; production snapshot treats missing capture wings as orderable fleet before 30 s (else income research came before the opening wings). Updated: `EnemyZoneAllocatorTests`, `EnemyUnitCommanderTests` (allocation, scout, raid, groups, station wings, escorts, station defence), `EnemySquadronCaptureAssignmentsTests` (`AssignInZone` ×3), `EnemyProductionStrategyTests` (opening, level-1 corvette). Reviewed, no change: `EnemyDecisionSchedulingTests` (constructor unchanged).
- Reusable guidance → [[Architecture/Enemy_AI_Objectives#Opening and wing groups (2026-11-19, A1–A7) — current]].

## Evidence — battle 2026-11-19 22:29 (A8 run 1; UltraHard Small 1v1, code `70a3e0bb5` dirty) — user: opening better, then broken
Files: `%USERPROFILE%/AppData/LocalLow/GolinSkyCompany/EmpireAtWar/BattleTelemetry/Loading_20261119_222941_ai.csv`, `_ai_orders.csv`, `_squadrons.csv`, `Battle_20261119_222944_map.csv`, `Battle_20261119_223051.csv`.
- AI Separatist (1015, −1086) vs human Republic (−1015, 1086). Coruscant, seed −94196682. Log 10.9 s = battle 0 s. Recorded 286 s.
- Outer zones: SW (−1555, −1458), NE (1555, 1458); both ≈ 2,600 from the AI station.

### Worked
- Station wings ordered at launch (first order ≤ 6 s). Centre: pads 4 / 9 / 5 / 8 at 28–40 s, relay 37 s, 3 satellites by 39 s. SW zone: pads 51–52 s, relay 59 s, mine 64 s.
- No corvette before 30 s; none from level 2. `escorts` 0–3.

### Faults
- **R1 (P0) Centre mines never taken.** Sites 0–3 `got0` on all 48 `wingAssignment` rows. From 38 s each mine shows `defenders1+` → "contested" → skipped. Human took them at 45 / 76 / 150 / 175 s. AI first centre mine at 168 s (a ship).
  - Wings went one per pad, never onto a mine spot (wrong radii, see Capture check).
- **R2 (P0) Wing swarm stands still.** 12–27 wings circled at the bait point ≈ (370, −400) from 43 s to 250 s, ≈ 250 from the free mine at (213, −166). Rule: "every site contested → wait at the zone edge". With 23 wings vs 5–16 defenders nobody attacked.
- **R3 (P0) Wing overbuy.** 24 wings at 7–29 s (12 Vulture + 12 Belbullab, 6,000 credits), 12 more at 62–86 s (9 bombers) → 36 bought + 5 station wings. Cause: `TrySelectSquadron(ignoreShipRatio: !canOrderShips)` → no cap while ships are forbidden.
  - Effects: first ship deployed 64 s; mine construction `started=False` × 9 (66–96 s, no money); level 3 bought 118 s; first frigate 154 s.
- **R4 (P0) Second outer zone not visited at the start.** Group B took the SW zone, then moved to the NE zone at 74 s. Human took the NE pads 110 s, relay 117 s.
- **R5 (P0) Group B order swapping.** `groupBZone` flipped SW ↔ NE at 74 / 110 / 128 s; 10–11 wings sat on the NE relay point 128–182 s; at 200 s `defenders3` → all recalled. Hostile mine 14 never targeted; `raids=0` on all rows. Turbolaser satellites rated as a flat 60 DPS against wings.
- **R6 (P0) Held centre gets no ships.** AI owned the centre relay 37–273 s with 3 satellites. `zoneAlloc zone:0 ships0` on all 68 rows (`defenders 5–16, statics 2–5`). All 3–10 ships went to the NE zone. Centre lost 273 s.
  - Cause: the centre is rated as an attack on its remaining mines, not as an own zone under threat; own wings in the zone are not counted.
- **R7 (P0) Ships deploy at home only.** All 14 deployments at the station (`tier=A`), rally (674, −721), while the AI held the centre relay and the SW relay. Then a 2,600 march past the centre; human ships intercepted.
- **R8 (P1) NE zone attack.** 3–10 ships there 119–286 s; relay flipped 227 s, lost 263 s; objective switched relay ↔ mine ↔ pads; satellites not targeted first (A5 gap).
- **R9 (P1) Ships late.** Follows R3.

## Round 2 — steps (B1–B6 + B9 done 2026-11-19; B7, B8, B4 satellite types open)
### User rules added 2026-11-19 22:40
- Wings fly **onto mining spots** in the centre: mine + nearby pads are captured together. Build mine or battle asteroid at once.
- Both outer zones are visited at the start.
- A swarm never stands still: capture every mining spot, build, attack.
- A raid finishes the job. In a zone with turbolaser satellites, bombers hit the mine first. No retreat of part of the group; no order swapping.
- A held zone is defended: support comes to fight around the satellites.
- Reinforcements spawn **inside held zones**.
- Ship routes do not skirt the enemy-held centre.

- [x] **B1 Wing capture by mining spot (R1, R2).**
  - Group A target = a centre **mine spot**; the whole group (or half per spot when ≥ 6 wings) holds inside its 60 circle until the mine and the pads within 500 flip, then the next spot, base-side first.
  - Delete: one wing per pad, `CAPTURE_HOLD_DISTANCE`, the bait point, "contested → skip".
  - Contested spot: group advantage vs units within 500 ≥ 1 → fight there (attack-move onto the spot); else next spot; no spot → the pad nearest home. Never idle.
- [x] **B2 Wing budget (R3, R9).** Opening purchase cap: free wings (station + bought) ≤ 6 for cheap wings, ≤ 4 otherwise, until the first ship is deployed. Bombers ≤ 3 per known hostile mine. Remove the uncapped `ignoreShipRatio` path. Mines and satellites are paid before extra wings.
- [x] **B3 Outer zones at the start (R4).** One B group per outer zone: 1 wing each from the first decision (2 to the own-side or nearer zone when wings are cheap). Each lands on the zone's mine spot.
- [ ] **B4 Group commitment (R5).** Commitment done; satellite threat by type and satellite raid order open (see Round 2 notes). A group keeps its zone until it is taken or the group is below 50 % of its wings. No per-pass zone switch. Raid target order inside a zone: mine → satellites → relay for bombers. Satellite threat vs wings by type (turbolaser low, quad laser high) instead of flat 60.
- [x] **B5 Defend held zones (R6).** A zone where the team holds the relay or a satellite and hostiles are present = top tier. Force rated with own satellites **and own wings in the zone**. Ships are sent even below an even fight while own satellites stand (fight around them).
- [x] **B6 Deploy in held zones (R7).** `EnemyFactionController.TryFindShipSpawnPosition`: spawn in the held zone nearest the ship's task (centre for the centre force, outer relay for that zone) when its spawn rule is open; else home. Check `ReinforcementSpawnRule.IsOpen` for AI-held capturable zones.
- [ ] **B7 Route (R7).** Waypoint around a hostile-held centre for ship groups (deferred from A3).
- [ ] **B8 Hostile zone order (R8, A5 gap).** Satellites → relay → pads for ships.
- [x] **B9 Tests (`EnemyAi`).** Written, not run (local override); spawn-point and own-wing support covered only at allocator level. Group lands on a mine spot; never idle with a free spot in the zone; purchase cap; one group per outer zone; group keeps its zone across passes; held zone under attack is top tier and counts wings; spawn point picks a held zone.
- [ ] **B10 Battle.** Same matchup. Gates: all 4 centre mines AI-owned or contested by 60 s; no wing circling > 15 s with a free site in its zone; wings bought ≤ cap before the first ship; first ship ≤ 45 s; NE and SW zones both visited by 40 s; ≥ 50 % of ships at the centre while it is held and attacked; ≥ 1 deployment outside the home zone once a relay is held.

## Round 2 — implementation notes (2026-11-19)
### Wings (B1, B3, B4)
- `EnemySiteObjectives.AddSquadronTargets` → `AddMiningSpot` for every mining site (any owner); pads carry their capture radius.
- `EnemySquadronCaptureAssignments.AssignInZone(wings, centre, reach, home, addWing)`: spot with work (mine target or pad whose radius covers the spot), base-side first. ≥ `SPLIT_GROUP_WINGS` 6 wings + ≥ 2 spots → halves on the two best spots.
- Uncontested spot → hold (`TryGetHold`): attack-move, then `MoveTo` within `HOLD_ENTRY_SCALE` 2 × spot radius. Contested → attack-move only when the part's advantage ≥ 1, else next spot.
- No spot → free site nearest home → contested site nearest home → zone centre. Deleted: bait point, one wing per pad, `CAPTURE_HOLD_DISTANCE`, `IsSite`, relay/mine order.
- `EnemySquadronCommander._outerGroups`: one `EnemyWingGroup` per outer zone (`HomeZone`), nearest home first. Fill: 1 wing per outer group, then group 0 up to `_outerWingCount`; rest → A. Bombers → outer group 0.
- Commitment: zone kept while it has targets and capturers ≥ `Peak` / 2. Taken → next outer zone with targets (unworked first); weak → nearest winnable zone, else stays. `Peak` resets on change.
- Raids: outer groups without a zone with all wings; bombers any time. Telemetry `groupB=n@(x z)|n@raid`, `spots=`.
### Production (B2)
- `EnemyWingPricing`: `CHEAP_WING_PRICE` 300, opening cap 6 cheap / 4 otherwise. `HasSquadronRoom`: ship count 0 → opening cap (station + hangar wings count). `ignoreShipRatio` removed.
- Bombers ≤ `BOMBERS_PER_HOSTILE_MINE` 3 × known hostile mines.
- `PriorityConstructionPending` blocks ships and every wing except missing capture wings.
- "First ship deployed" approximated as first ship **bought** (reserved count > 0).
### Ships (B5, B6)
- `EnemyZoneObjective.IsHeld` (relay allied or own satellite/platform), `OwnStructures`, `IsHeldUnderAttack`. `OwnSupport` += own wings within 2 × radius.
- `GetTier`: held under attack → −1 whatever the verdict. `TryFill`: held under attack with `OwnStructures > 0` → all free ships committed below even.
- `EnemyZoneAllocator.DeployZone` = gather zone → best served zone → weak target. Planner `DeploymentPoint` = its centre; `TryGetReinforcementRally` prefers it over `GatherPoint` → `TryGetSpawnPositionNear` picks the held relay or station nearest it (spawn rule must be open).
### Open
- **B4 satellite threat by type:** armed structures still a flat 60 DPS. Needs a platform-type facade (only `IStructureSaveFacade.TypeId` exposes it) and `UnitCombatProfileCatalog` profiles from `DefendPlatformData.WeaponLoadout`; test fakes need both.
- **B7 route waypoint** and **B8 satellites → relay → pads** not started (B8 needs hostile satellites as blocking-facility candidates).
- A held zone with no candidate site left gives its zone group nothing to capture (not verified).
### Verification
- Unity recompile: 0 errors. Tests not run (local override).
- Updated: `EnemySquadronCaptureAssignmentsTests` (5 `AssignInZone` tests replace 3), `EnemyUnitCommanderTests` (raid, group fill, commitment), `EnemyZoneAllocatorTests` (+3), `EnemyProductionDecisionModelTests` (opening cap step). Reviewed, no change: rally test (no allocation → `GatherPoint` path), `OwnedSiteDefense_*` (data has Mining settings).

## Acceptance (telemetry)
- First wing order ≤ 6 s. Wings at the centre ≥ 3 by 40 s; outer-zone group 1–2 wings.
- AI satellites: ≥ 2 at the centre and ≥ 1 at the own-side zone by 60 s when uncontested at arrival.
- No corvette purchase before 30 s; none at level ≥ 2.
- `wingAssignment escorts` = 0 for group wings; `raids > 0` once a hostile mine is known.
- Every deployed ship has a non-guard task within 5 s.
- Weak phase: ≥ 1 outer-zone attack per 120 s; target switches after a retreat; patrol never empty.
- Siege: attack orders at squadrons < 20 %; ≤ 4 re-targets per ship per minute.

## Fixed already (2026-11-19, uncommitted)
- [x] Emergency corvette fallback deleted; no level-1 corvette from level 3; critical station saves for the best ship in time. Compiled; tests not run. To tighten in A2: level 2 → 0, not before 30 s.

## Decisions (user, 2026-11-19)
- Outer-zone wing count: 2 when a fighter wing costs ≤ 300 credits (Vulture 200), otherwise 1.
- Interceptor not unlocked at level 1 → buy a second fighter type instead of waiting.
