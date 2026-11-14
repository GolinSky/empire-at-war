---
type: research
category: Bugs
status: in-progress
record_state: battle-audit
created: 2026-11-13
verified: 2026-11-13
system:
  - ai
  - balance
verification: Existing November 13 battle telemetry and live C# source
  inspection; no gameplay edits or tests.
tags:
  - flag/issue
---
# Enemy AI Battle Review 2026-11-13

## Goal

- Track objective switching, retreat transitions and order churn from the latest Republic AI battle.
- Owner: [[TODOs/Bugs/Enemy_AI_And_Faction_Balance]]. Historical reviews remain dated evidence.

## Important Values

- Player1 = Republic AI. AI clock: Unity scaled session seconds, 38.2–1961.3; 1,923.1 s observed. Do not equate with damage-report unscaled duration.
- Main-state time: CaptureZone 45.5%, DefendBase 14.2%, RetreatValue 12.9%, HuntFleet 12.1%, RaidStructures 9.2%, Hold 3.9%, RebuildFleet 1.3%, EngageLocal 0.9%.
- Order CSV includes both players. Counts below filter Player1; primary-state time does not describe every independent group.
- Verification: existing CSV analysis and live C# inspection on 2026-11-13. No gameplay changes, new battle, or tests.

## Implementation

### Confirmed behavior — raid and site-defense switching

- 20 transitions between `CaptureZone` and `RaidStructures`.
- Reproduction from log: t635.6 Raid → 637.8 Capture → 638.8 Raid → 641.7 Capture → 642.6 Raid; four reversals in 7 s.
- Raid pursues exposed hostile income; capture interrupts to defend threatened owned sites. Switching is confirmed; lost travel time/damage is not quantified.
- `Assets/Scripts/Entities/EnemyFaction/Models/EnemyStrategicDecisionModel.cs:147–152`: threatened-site capture weight 0.7; economy raid weight 0.66 requires `!HasThreatenedSite`.
- Same file:116,221–222: hysteresis = 0.05 and applies only to a positive eligible score. Threat appearance makes economy-raid score zero; hysteresis cannot retain that rule.
- Proposed: short threat-clear grace or target commitment for raid/site-defense changes; preserve urgent base defense and retreat overrides. Not implemented.
- Flag issue: unresolved rapid switching; next action is controlled objective/contact trace and a narrowly scoped stability fix.

### Observed behavior — capture and retreat switching

- 19 transitions: 10 CaptureZone → RetreatValue, 9 RetreatValue → CaptureZone.
- These are transitions, not 19 complete cycles. Local strength/contact changes can justify retreat.
- Existing recovery logic requires regrouping before normal recovery ends; lost-safe-anchor paths can allow viable local action.
- Next action: compare local advantage, regroup status, objective identity and orders around each short reversal. Do not reduce retreat safety thresholds from counts alone.
- Source: `EnemyStrategicDecisionModel.cs:232–251`; historical recovery owner → [[Done/Bugs/Enemy_AI_Defense_And_Zone_Recovery_Plan]].

### Observed behavior — frequent order replacement

- 3,908 issued / 3,700 cancelled AI orders; 1,430 of 3,783 consecutive per-entity issue intervals <1 s.
- 709 consecutive Attack ↔ AttackMove changes; 216 consecutive Attack reissues retain the same target ID.
- Role changes, screening and changed destinations can require replacements. These counts do not establish a defect or damage loss.
- Next action: correlate same-target reissues with role, destination, movement/facing and weapon activity; suppress only behaviorally redundant replacements.
- No general idle stall: 45/2,472 AI periodic samples use IdleState; only 13 samples are both stopped and have no tracked contact. Samples are not whole-fleet idle duration.

### Working behavior — production and recovery

- 169 purchaseAccepted, 168 buildCompleted, 125 ship deployments; no purchaseRejected or deploymentBlocked events. Categories differ; counts are not expected to match one-to-one.
- 1,810 production-wait samples: 1,123 population-limited, 702 credit-limited, 88 both. Repeated observations are not unique failed purchases.
- `Assets/Scripts/Services/Enemy/EnemyProductionStrategy.cs:484–494`: choose the future reinforcement before free-population checks to prevent small replacements excluding capitals; critical-station candidates require affordability and capacity.
- Waiting at 119/120 population for a 4-population Arquitens is consistent with this policy; no permanent unfillable-choice defect established.
- Fleet: 0 ships at t1071.8 → 14 at t1202.9. Station survived the recorded siege; minimum hull after full build (t698.4) was 92,249.77 of 126,000 at t1178.7.
- End sample t1961.3: 12 own ships / 2 known enemy ships (`enemyShips`; `localThreat` = 1), HuntFleet. Logs do not establish final winner or general win rate.
- Production-wait `reason` uses current-culture formatting: comma decimals (`money=3531,206`) and localized infinity (`бесконечность`) mixed with `Infinity`. Normalize before parsing.

### Ship and squadron follow-up ownership

- Ship findings and implementation work moved to [[TODOs/Bugs/Ship_Hardpoint_And_Balance_Plan]]: Recusant DPS/angles, Peleset/Recalcitrant yaw mismatch, narrow-arc candidates, Mandator economics, attribution and description drift.
- Source battle remains the November 13 recording; relocating findings does not resolve them.
- Belbullab survival/exposure and free hangar valuation remain with [[TODOs/Bugs/Carrier_and_Squadron_Rebalance]].

## Decision

- Prioritize objective stability before broad faction-stat changes.
- Keep raid/site-defense switching confirmed; keep retreat/order causes as hypotheses until episode-level evidence resolves them.
- One battle cannot separate balance from fleet composition, exposure, research, abilities and player control.
- Parent remains in progress: diagnosis is complete, proposed fixes are unimplemented. Historical November 1 findings are not silently relabeled as current defects.

## TODO

- [ ] Trace and stabilize rapid raid ↔ threatened-site switching, preserving emergency overrides.
- [ ] Classify short capture ↔ retreat reversals as justified threat response or avoidable switching.
- [ ] Identify redundant order replacements and measure any movement/firing interruption.
- Squadron survival/exposure follow-up → [[TODOs/Bugs/Carrier_and_Squadron_Rebalance]].
- Ship implementation ownership → [[TODOs/Bugs/Ship_Hardpoint_And_Balance_Plan]]; no duplicate ship checklist here.
- [ ] Verify behavior across additional battles before faction-wide tuning.

## Files

- Telemetry folder: `C:/Users/golin/AppData/LocalLow/GolinSkyCompany/EmpireAtWar/BattleTelemetry/`.
- `Loading_20261113_110752_ai.csv` — decisions, reasons, production and station timeline.
- `Loading_20261113_110752_ai_orders.csv` — per-entity orders and periodic samples.
- `Battle_20261113_110922.csv` — aggregate damage; separate clock.
- [[Tooling/Battle_Telemetry]] — schemas and interpretation limits.
- [[Architecture/Enemy_AI_Objectives]] — implementation contracts.
- [[TODOs/Bugs/Enemy_AI_And_Faction_Balance - Research]] — earlier source research.
- [[TODOs/Bugs/Enemy_AI_And_Faction_Balance - Battle Review]] — historical November 1 review.


## Evening Battle — Separatist AI, 19:09 Session

### Evidence and scope

- Latest full battle: `Loading_20261113_190921_ai.csv`, matching `_ai_orders.csv`, `Battle_20261113_191102.csv`; ended about 19:23 Kyiv. The 19:26 file contains startup only.
- Player1 = Separatist AI; Player0 = Republic. Damage window = 770 unscaled seconds; AI clock = 24.4–1155.5 scaled startup seconds. Times below use AI clock.
- Autosave: seed `1509740851`, planet `52`, mapSize `0`, AI difficulty `3`. Save battleTime has a separate origin.
- Main-state duration: DefendBase 372.9 s, CaptureZone 263.7 s, Hold 139.4 s, RetreatValue 136.4 s, RebuildFleet 102.6 s, HuntFleet 55.4 s, EngageLocal 49.9 s, RaidStructures 10.8 s.
- 13 direct capture/retreat transitions; 1540 issued / 1443 cancelled AI ship orders. 702/1478 consecutive per-entity issue intervals <1 s; these counts alone do not prove redundant commands.
- Verification: CSV aggregation, autosave and live C# / serialized balance inspection. No gameplay edits, new battle or tests. Graphify freshness failed on its busy update lock; findings use live source.

### P1 — Secondary-station defenders use the old home spawn

- After primary station loss, station189 remains; defense rally = (1978,1516), but replacements spawn around original home (1144,1098).
- C9979 deployments t1095.2 /1115.2 /1138.6 are followed by zero-ship RebuildFleet at t1099.9 /1119.7 /1142.3.
- `EnemyFactionController.cs:297–309`: defendHome always uses `TryGetDefaultZoneSpawnPosition`; production-base fallback retains owner, not station position.
- `ShipSpawnPoints.cs:71–75` and `ReinforcementZonesSystem.cs:202–213`: resolve the static default home.
- Proposed: pass the actual surviving defense/production station anchor through placement; preserve clearance checks. Confirmed source defect with matching deployment evidence.

### P1 — Retreat rejection falls back to stopping the fleet

- t412.1–492.7: Hold, 7 ships → 0; station remains at 93600 hull /74400 shields in its t414–489 samples.
- Entity147 Attack is cancelled at t412.14; it remains at (1295.3,842.9) until hull0 at t443.61. Entity155 remains at (1432.3,1275.0) until hull0 at t492.56.
- `EnemyStrategicContextBuilder.cs:740–746,754–760`: retreat includes every hostile within1000 of destination, even the force being escaped; the escape exemption applies only to the along-route branch. Matchup excludes stationary base firepower.
- `EnemyTaskForceExecutor.cs:140–143,646–654`: Hold stops remaining orders. `EnemyUnitCommander.cs:221–225,252–315` then only clears the default spawn zone.
- t394.3 retreat localAdvantage0.78 → t395.8 EngageLocal at worse0.76 when safe anchor disappears. Log lines929/934; Hold begins line977.
- Do not call this total weapon inactivity: 32/38 periodic samples in the first Hold episode are IdleState, but 37/38 still track a weapon contact.
- Proposed: distinguish viable withdrawal under station coverage from winning a destination battle; preserve a tactical fallback when no strategic objective is safe.

### P2 — Objective selection is poorly adapted to map scale

- t358.7 zone1 →359.2 threatened site18 →359.7 zone1; fleet is roughly3000–3500 units from zone1 (AI CSV lines827/828/833).
- `EnemyStrategicContextBuilder.cs:581–598,746–750`: retained objectives immediately lose eligibility at the same safety cutoff; no separate retain threshold.
- Lines531–534,666–672 combine squared travel distance ×0.25 with linear center distance ×2 and flat2000 facility penalty. At1000 travel units, the first term is250000; strategic-relay preference is weak relative to travel.
- Lines729–751 assess a straight segment rather than the navigated route around scenery. Source-level limitation; this run does not prove a bad detour.
- Early site12 /zone2 /site17 captures succeeded. Three explicit stalls: site7 t143.7, site8 t230.6, zone1 t306.6. Only one obstacle rejection and zero ion-cost rejections.
- Proposed: stabilize objective retention and use compatible distance/route costs. No evidence for broadly impassable generation.

### P2 — Squadron effectiveness and escort estimation

- Republic squadron damage = 243724; CIS = 66515 (3.66×). Shares of faction damage =15.8% /5.7%.
- ARC170 =128698 total /61120 structure damage; YWing =75569 /43653. DroidBomber =16346 /630.
- CIS squadron structure damage totals1419 versus111997 Republic. Deployment and target access differ; aggregate damage is not an equal-force balance test.
- AI selected12 DroidBombers,11 Belbullab22s,9 Vultures. Purchase events are attempts, not proof of deployed counts.
- `EnemySquadronCommander.cs:299–335`: escort threat uses global known strikecraft and ship class, without proximity. Larger maps can spread escorts away from local concentrations.
- Same file:367 uses `AddNew(profile,false)` for escort sufficiency; damaged squadrons count as full strength. `ForceCompositionBuilder.cs:57,90` uses current strength for ordinary fleet estimates.
- Source-confirmed escort weaknesses; no per-squadron command/health timeline to prove their contribution in this battle.
- `UnitCombatProfileCatalog.cs:56,85` correctly consumes current member stats, aggregate weapons, warhead modifiers and MaxActive hangars. No live hangar double-count found.
- Relevant pre-battle changes: Vulture hull70→28; DroidBomber hull/shields50/10→32/5, speed31→29; ARC170 hull/shields105/35→80/30, speed41→69. These warrant matchup/response-time review, not a blanket buff from one sample.
- Squadron stat follow-up remains with [[TODOs/Bugs/Carrier_and_Squadron_Rebalance]]; AI escort logic remains with the parent AI plan.
- Do not treat damage-report kill counters as squadron-entity losses or activeSeconds as summed squadron uptime.

### Working behavior and next actions

- Production:163 purchaseAccepted,154 buildCompleted,62 ship deployments; no purchaseRejected or deploymentBlocked events.
- 984 capacity-bearing waits:972 credits-only,12 credits+population;85 other wait rows lack capacity fields. Credit pressure dominated recorded ordinary waits.
- Total damage: Republic1543635 /CIS1170684. AI rebuilt and fielded capitals; this is not a general production shutdown.
- [x] Fix secondary-station spawn anchoring and no-safe-retreat Hold fallback first.
- [x] Stabilize capture retention; align route scoring with current map distances. Navigated-route safety still uses a straight segment.
- [x] Localize escort allocation and count surviving squadron strength.
- [ ] Compare subsequent battles before numerical faction/squadron tuning.
- Parent remains in progress for broader balance. Morning review above remains historical evidence from a different battle.

## Fixes 2026-11-13

- S1 spawn: `IShipSpawnPoints.TryGetSpawnPositionAround`; `EnemyFactionController` defenders spawn within `HomeAreaRadius` of the rally station → fallback default home.
- S2 retreat: `EnemyStrategicContextBuilder.IsRouteSafe` escaping ignores hostiles ≤1000 from origin (the pursuing force). Station firepower still not in matchup (stations have no combat profile).
- S2 Hold: `EnemyStrategicDecisionModel` rule EngageLocal 0.15 when `HasLocalThreat && !HasSafeRetreat`.
- S3 raid/site flips: `EnemySiteObjectives` keeps an owned site threatened 6 s after last hostile inside.
- S4 capture: current objective safety threshold ×0.85 (`RETAIN_SAFETY_SCALE`); strategic capture score uses linear travel (`GetTravelCost`) instead of squared ×0.25.
- S5 escorts: `EnemySquadronCommander` counts strikecraft ≤2000 from an own ship; escort targets need nearby strikecraft; escort sufficiency uses current health (`ForceCompositionBuilder.AddUnit`).
- S6 orders: `EnemyTaskForceExecutor.IssueStrikeAttack` skips re-sending Attack when target, slot and active Attack order are unchanged. Order-change reason logging not added.
- Verification: Unity recompile clean, no console errors. EditMode tests not run (local override); new test `CorneredWithoutSafeRetreat_EngagesLocalThreatInsteadOfHolding`. Behavior unverified in battle.
- Reusable guidance: none extracted yet; promote after battle confirmation.

## Carrier Usage 2026-11-13

### Evidence

- AI buys carriers: Republic AI 15 Venator, 14 Venator II, 7 Acclamator, 6 Imperator; CIS AI 7 Bulwark, 6 Wavecrest, 5 Providence.
- Carrier hulls fight like other ships: in-range sample share Venator 38%, Venator II 52%, Providence 46%.
- Squadron damage: Republic AI 382k vs player CIS 366k (1851 s). CIS AI (UltraHard) 66k vs player Republic 244k (770 s) → ~86 vs 316 per s. Confounded by Vulture/DroidBomber nerfs between battles.
- Ruled out: launch throttle (10/s); AI/player guard logic is shared; production values hangars; Fighter Reinforcements / Rapid Deployment are on no ship.

### Changes

- Logging: `SquadronTelemetry` → `_squadrons.csv`; schema → [[Tooling/Battle_Telemetry]].
- `IHangarAirWingFacade` (`HangarComponent`): `CanLaunch`, launched squadrons.
- `EnemyStrategicContextBuilder.AddEscortingAirWing`: each carrier's `ShipForces` adds launched squadrons still on Guard, at current health → selection, readiness, target DPS and screens count the air wing.
- `EnemyTaskForceExecutor`: carriers (hangar can launch or has squadrons out) never screen; they get an Attack order with offset on the gun-line side at `targetRadius + shipRadius + weaponRange`. Gunless carriers keep their battle slot.
- Verification: Unity recompile clean, no console errors. Tests not run; no new tests. Behavior unverified in battle.

### TODO

- [ ] Play a CIS-AI UltraHard battle; from `_squadrons.csv` measure launches per carrier, squadron lifetime, guard vs engaged time, hangar shutdown time; confirm carriers hold standoff in `_ai_orders.csv`.

### First check — `Loading_20261113_202230` (CIS AI, 145 s unscaled, short)

- AI launches 54: station 79 → 20 Vulture; Wavecrests 121/126/132 → 19/9/6 mixed. No hangar shutdown or bay loss.
- AI hangar squadrons: 63% of samples engaged; Guard distance median 363; median life 41 s; 39 destroyed (27 Vulture; CR70 killed 10).
- Squadron damage: CIS 6.5k (16% of faction) vs Republic 3.9k; player squadrons idle in 66% of samples → not an equal comparison.
- Wavecrests: 74% in range, mean contact distance 393 (weapon 575), hull untouched. Mostly AttackMove from CaptureZone → standoff path exercised only t237–245.
- Open: EngageLocal ↔ RetreatValue flip t230–255 (local 1.03/0.83/0.95/0.78 around the 0.8 threshold); capture objective site:7 → site:2 → site:6 in 1.5 s at t293. Carrier standoff not yet proven.
  - 2026-11-14 cause: engage/retreat scores cross at local ≈0.86 with `HYSTERESIS` 0.05 shifting it ≈0.0125; capture selection has no current-target score bonus. Fix owned by Step 0 of [[Done/Features/Enemy_AI_Patrol_And_Recovery_Relocation]].
