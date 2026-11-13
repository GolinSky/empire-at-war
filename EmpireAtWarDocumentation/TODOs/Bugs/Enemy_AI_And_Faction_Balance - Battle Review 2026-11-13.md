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
