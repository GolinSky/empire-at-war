---
type: research
category: Bugs
record_state: battle-audit
created: 2026-11-01
system:
  - ai
verification: Latest battle CSV/order/damage analysis and source inspection; no
  tests executed
---
# Enemy AI — Implementation and Latest Battle Review

## Goal

- User verdict: overall good; opening and main battle improved, something went wrong near the end.
- Audit the implemented fixes against the latest Republic AI vs Empire battle. Identify remaining defects and propose the next changes.
- Original user research, preserved in full → [[GameDesign/References/EaW_Global_AI_Original_Attachment]].
- Parent implementation/backlog → [[TODOs/Bugs/Enemy_AI_And_Faction_Balance]]; original EaW source comparison and earlier battle → [[TODOs/Bugs/Enemy_AI_And_Faction_Balance - Research]].

## Evidence

- Latest: `Loading_20261101_172750_ai.csv`, `Loading_20261101_172750_ai_orders.csv`, `Battle_20261101_172909.csv`.
- Baseline: `Loading_20261101_132417_ai.csv`, matching orders and `Battle_20261101_132621.csv`.
- Folder: `C:/Users/golin/AppData/LocalLow/DefaultCompany/EmpireAtWar/BattleTelemetry/`.
- Latest AI log: 7,542 rows; 973 decision/sample rows; Player1 Republic. All times below are scaled AI-log seconds, not wall-clock minutes.
- Damage report separately records 3,376 unscaled battle seconds. Do not equate its clock with AI `Time.time`.
- Analysis uses recorded orders, purchases, health and damage plus source inspection. No new gameplay implementation or tests in this audit.

## What Changed and Why

| Implemented change | Problem addressed | Latest evidence |
|---|---|---|
| Preserve a capture-capable squadron; build safe captured mines early | Scouts consumed capturers; mine construction waited for warships | User reports dynamic opening; economy construction continues |
| Prefer visible hostile mines in raids; compare local defender strength | AI ignored enemy income, including mines behind captured zones | 348.1 s in primary RaidStructures state; direct target success needs per-structure lifecycle logging |
| Target-specific force selection using loadout DPS and defenders | Arbitrary ships were sent against unsuitable targets | 122 ready targeted-force records average 8.02 strike ships, max14 |
| Apply weapon caliber damage and accuracy modifiers | Heavy anti-capital weapons were undervalued | Venator/capital production substantially increased |
| Population-aware counter scoring and wait for selected reinforcement | Cheap replacements filled every capacity gap | 36 Venator variants deployed; ordinary reservation works, emergency timing remains deficient |
| Persistent main force at six available combat ships; recruit distant groups | Spatial groups never concentrated strategically | Main-group attacks reach14 strike ships; no dominant assembly stall |
| Fill useful committed force; bound active screens | One attacking ship with eleven passive guards | Only11 of122 ready targeted records have one strike ship; all122 report zero reserves |
| Require75% assembly for attacks and large captures | Piecemeal attacks without required force | 22 assembly-action records; attacks launch with substantial forces |
| Prefer central relays for main force; separate capture detachment | Expansion stayed fragmented | Centre attempted with11–17 committed ships, but not secured |
| Home-first defense spawning; otherwise nearest valid fleet anchor | Reinforcements appeared at remote mining relays during siege |81 home-defense deployments; no deploymentBlocked events |
|20 s station-pressure memory; recall capture ships and purchased squadrons | Brief contact loss canceled defense | Response improved, but a48.5 s damaging defense gap remains |
| Defend at station via AttackMove | Fleet fought away from station support | New tactical regression: centre anchoring cannot answer long-range siege |
| Purchase/deployment/station telemetry | Old attempts looked like completed purchases; station outcomes unknown |283 accepted/completed purchases; exact late production/health sequence now visible |

- Existing systems were extended; no full rewrite or imported EaW gameplay data.
- Current contracts → [[Architecture/Enemy_AI_Objectives]]; event schema → [[Tooling/Battle_Telemetry]].
- Previous implementation verification: Unity compilation/import complete, error console empty, scoped diff checks clean. Authored tests were not run under the checkout override.

## Production and Counter Results

| Warship | Previous battle | Latest deployed |
|---|---:|---:|
| CR70 |58|33|
| Carrack |20|63|
| Centax |31|2|
| Venator |2|19|
| Venator II |1|17|
| Resolute |4|12|
| Rothana |0|11|
| Imperator |0|6|
| Mandator |0|6|

- Latest:205 warships deployed; all283 purchase attempts accepted and completed across all request categories. No rejected-purchase events.
- Listed large ships (Venator/II, Resolute, Rothana, Imperator, Mandator):71 deployments;509,200 of773,100 warship credits.
- CR70 share fell58/166→33/205; Carrack share rose20/166→63/205. Battles differ in length/opposition; raw counts are not controlled win-rate evidence.
-2,550 repeated production-wait samples:1,029 capacity-limited,2,152 cash-limited,631 both. These are repeated observations, not distinct queue stalls.

| Type | Effective damage | Damage per deployed ship-second | Samples with preferred target in range |
|---|---:|---:|---:|
| CR70 |10,792|2.9|10.7%|
| Carrack |86,903|11.0|12.0%|
| Venator variants pooled |873,839|172.9|39.0%|
| Resolute |1,016,994|586.7|34.0%|

- Exposure:205 deployments matched to entity starts within0.05 s, then release records; use summed lifetimes, not damage CSV `activeSeconds`.
- Outcomes include shield/hull damage, exclude separately attributed carrier fighters, and are not controlled unit DPS comparisons. Both Venator variants share a damage-report view name.
- Carracks still have a role:58 of61 kills were TIE strikecraft. Do not ban corvettes; limit escort demand and improve range/local-threat valuation.

## Fleet and Centre Results

- Main-state Hold fell22.9%→4.5% of the sampled timeline. HuntFleet rose0→87.6 s; no AssaultBase state was recorded.
- Ready targeted attacks:122 records; mean8.02 strike ships, max14;11 single-ship attacks; zero reserves in all122.
- Assembly actions:22 records (12 capture,7 main-force,3 target-counter). No evidence that assembly is the dominant inactivity cause.
- Periodic ship samples with None:2.7% at t600–1200,2.0% at1200–1800,1.9% at1800–2400;16.4% after2400. These are sample shares, not whole-fleet idle durations.
- Centre zone0:16 actual primary CaptureZone samples,11–17 committed ships. Owner remains Player0 in those records.
- Centre capture progress:0.239 at259.6→0.606 at262.0→0 at267.2; RetreatValue at272.2. Last recorded main-force centre capture attempt:812.5.
-436/973 primary rows reject zone0 because no clear sampled point fits. Later candidate mentions during defense do not prove capture execution.

## Remaining Issues and Proposed Fixes

### 1. Defense can stop while the station takes damage — highest priority

- t2489.8: ten ships leave defense for a distant relay. Pressure remains false until2538.3:48.5 s.
- Station shields18,631 at2495.7→8,686 at2510.7; hull116,642 by2525.7, while defendHome remains false.
- Cause: pressure depends on qualifying observed attackers and a20 s memory; actual station health/shield loss does not independently keep defense active.
- Proposed: damage-driven defense latch, recent siege intelligence, and a quiet/recovery interval before releasing the main force. Do not reveal hidden attacker transforms.

### 2. Returning home is not enough against siege — highest priority

- Home spawning works:81 defensive deployments total;15 during final t2538–2716. Remote mining deployment is no longer the observed failure.
- Final defense initially fights:36/100 ship samples at2538–2645 have a target in range. At2670–2712 all51 samples have zero contacts, no target in range, moving=true.
- At2705–2712 most defenders are419–485 units from the station but1,464–1,988 from an eastern attacker; their weapon ranges300–650 versus attacker1,755.
- Station cover contributes only2,131 effective damage,207 shots,zero kills this battle. High station HP does not compensate for inability to engage siege.
- Cause: the latest unconditional station-centre AttackMove can pull defenders away from attackers. Do not infer a navigation bug solely from movement samples.
- Proposed: valid threat-facing positions outside the station footprint; counter-equipped interception with bounded pursuit when the enemy outranges the defensive position. Reposition if station damage continues while defenders lack firing solutions.
- Telemetry: log actual defensive order destination separately from selected threat; current decision destination can describe the attacker while the order goes home.

### 3. Emergency production arrives too late — highest priority

| AI time | Event |
|---|---|
|2645.7|Station hull2,964.7, shields0|
|2658.3–2710.2|AI saves for12,000-credit,25-capacity Mandator during defense|
|2665.0–2702.7|Five CommonMiners consume5,000 credits during saving|
|2711.2|Mandator purchase accepted|
|2714.2|Another1,000-credit CommonMiner purchased|
|2715.7|First telemetry observation without living station|
|2821.2|Mandator deploys110 s after purchase, after station loss|

- Mandator110 s build time matches serialized data and all six observed builds.
- Queue delay also matters: one Venator accepted640.6 deploys788.9 (148.3 s despite55 s base build time).
- Proposed: keep normal capital reservations, but override under imminent home loss. Rank useful affordable defenders by queue+build+travel time; stop nonessential miners/platforms while station survival is critical.
- Do not claim faster production guarantees survival; it removes the clearly unsuitable response deadline.

### 4. Capital size blocks central capture

- `EnemyStrategicContextBuilder.IsObjectiveSafe` uses the largest NavigationRadius of all assigned ships and tests centre plus four half-radius offsets, including transient navigation reservations.
- This is a largest-ship point-clearance test, not proof that an entire formation physically cannot reach the zone.
- Proposed: select actual capturing ships for circle-fit checks; keep large escorts on a valid perimeter. Separate temporary friendly reservations from permanent obstacle/hostile safety failures.
- Retain ion/hostile-route checks. Do not globally disable navigation safety.

### 5. Capture-detachment ownership oscillates after station loss

- t2796.3–2820.8:44 records,43 CaptureZone↔Hold changes in24.5 s.
- Main group with two ships can capture site13; a one-ship capture detachment sees that objective reserved by the main group and holds.
- Next reconciliation removes the detachment for having no available target. Its ship rejoins main; the largest-radius clearance check rejects site13. The cycle repeats.
- Proposed: allocate objective ownership before extracting a detachment; distinguish “reserved by another group” from “no viable objective”; keep membership stable across transient reservations.
- This occurs after station loss; it worsens the aftermath, not the original siege defeat.

### 6. Range-aware counters and safer raids — secondary

-63 Carracks with12% in-range samples suggests both over-allocation and poor employment. Existing score ignores range, travel, arcs and active abilities.
- Proposed: bound anti-fighter demand using observed hostile strikecraft and existing escorts; add a conservative range/arrival factor. Preserve useful corvette screening.
- Final survivor raids a distant mine while21 contacts are recorded nearby. Destination safety alone does not account for travel/local survival.
- Proposed: evaluate local danger and escape/route safety before retaining a distant raid. Do not call this proven movement failure: final survivor weapon range1,897.5 exceeds its logged target distance around1,760.

## EaW Comparison and Limits

- Original attachment describes perceptions→goals/plans→task forces/FreeStore→unit execution. Its example manager classes, force recipe and scoring formula are proposals, not verified vanilla engine code.
- Our equivalents: strategic context builder→utility decision model→group planner/target selection→existing unit orders. Unassigned reconciliation acts as a practical FreeStore analogue.
- Adopted principles: explicit economy objectives, target-suitable units, force assembly, reinforcement allocation and reevaluation.
- Still absent: goal-owned production requests with deadlines, richer failure memory, range-aware combat prediction and independent goal-category budgets.
- Map knowledge: zones/sites and ownership are globally enumerated; direct hostile mine raids still require visibility; hostile ships use team sightings/memory.
- Local original EaW XML/mod paths and verified boundaries → [[TODOs/Bugs/Enemy_AI_And_Faction_Balance - Research#Original EaW Evidence]].
- Full attachment retained separately to preserve provenance without turning its unverified mod claims into project rules.

## Decision

- User acceptance: positive overall, partial; do not mark endgame defense or central capture passed.
- Next proposed scope: defense damage latch + siege interception + emergency production first; capture clearance/detachment stability second; range-aware escort/raid tuning afterward.
- This audit proposes fixes; those new fixes are not implemented here.
