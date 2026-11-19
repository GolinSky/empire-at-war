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
- Capture works. `CaptureSiteData.asset`: pad radius **60**, 10 s per net ship; mining site radius 500, 6 s; relay per zone data. One wing = 0.5 ship → pad in 20 s alone, 10 s with two wings; mine in 12 s alone.
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
- `EnemyAi` tests not run (local override). Updated: `EnemyZoneAllocatorTests`, `EnemyUnitCommanderTests` (allocation, scout, raid, groups, station wings, escorts, station defence), `EnemySquadronCaptureAssignmentsTests` (`AssignInZone` ×3), `EnemyProductionStrategyTests` (opening, level-1 corvette). Reviewed, no change: `EnemyDecisionSchedulingTests` (constructor unchanged).
- Reusable guidance → [[Architecture/Enemy_AI_Objectives#Opening and wing groups (2026-11-19, A1–A7) — current]].

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
