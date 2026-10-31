---
type: engineering-reference
system:
  - ai
  - capture
  - combat
verified: 2026-10-31
verification: Live-source review, Unity 6000.4.7f1 compilation and import, git
  diff checks; tests and Play Mode not run.
---
# Enemy AI Objectives

## Rules

- `EnemyShipTaskGroupPlanner` owns stable ship membership. Each group has its own `EnemyStrategicContextBuilder`, `EnemyStrategicDecisionModel` and `EnemyTaskForceExecutor`: objective safety, strategy/recovery and orders.
- Station pressure is independent of hostile/own strength ratio. Visible, alive, uncloaked ship/squadron threats can trigger `DefendBase` even at a low ratio.
- Direct/weapon-tracked station attacks use effective weapon range + station footprint + attacker footprint. Other visible contacts use the narrower center/contact envelope.
- Defense attacks the selected threat; station Guard's 240-unit chase leash cannot intercept distant bombardment.
- Retreat recovery requires cohesion for normal local action, not global parity. Ordinary offensive rules retain their fleet-advantage thresholds.
- Lost home + scattered fleet + viable local objective → rally through capture/local engagement. No safe anchor or viable action → explicit Hold/rebuild.

## Implementation

- Capture identity: `EnemyCaptureObjective` carries ID, type, owner, position, radius, progress and emergency-defense flag.
- No reinforcement-zone quota on any difficulty. Rank safe zones and facilities by distance/ion cost, regardless of owned capturable-zone count; home zones remain excluded from the diagnostic count.
- Satellite capture requires ownership of every mining site at the same `PointId`, with at least one mine present. Emergency defense of an owned site bypasses this prerequisite; safety still applies.
- Continuous expansion scores `0.6`, ahead of ordinary hunting/assault (`0.5`) and local engagement (`0.55`); overwhelming-advantage attacks (`0.75`), retreat (`0.8`) and base defense (`0.9`) retain priority.
- Each strategic decision scans capture targets; capture completion releases the objective for the next safe target. `EnemySiteConstructionController` checks every `3 s` with at least `2` ships and prioritizes building mines on owned, uncontested sites when affordable; existing income and production systems fund further ships.
- 2026-10-31: removed `MinimumControlledZones` tuning and the satellite eligibility gate. `Loading_20261031_011454_ai.csv` showed repeated zone attempts starving facility selection under the previous quota. Combat interruptions remain possible; this change does not guarantee uninterrupted capture.
- Retain valid objective/active engagement; stopped ships outside its circle are reissued. Idle ships inside remain assigned.
- Capture diversion beyond closest approach + max(800, radius + 2 × ship navigation radius) → return movement; valid engagements are retained.
- No approach/capture progress for 20 s of executed capture → reject objective for 20 s and consider alternatives. `EnemyUnitCommander` reports the executed objective after each decision; defense/retreat/hold/rebuild pause the accumulated stall budget. A new objective or approach/capture progress resets it. Objective owner/availability changes invalidate commitment.
- Safety uses known target/route threat, ion cost and clear capture-area samples. It does not prove full physical path connectivity; stall recovery handles lack of movement.
- Retreat anchors: surviving production host + safe default home, otherwise safe allied capturable relay. Dead default zones are excluded.
- Allied production host remains authoritative; capture ownership uses team relationships.

### Independent task groups (2026-10-31)

- With more than one ship, reserve the fastest eligible ship with a safe capture target as a capture detachment. Ships on Attack or Retreat are excluded from initial selection.
- Capture detachment keeps its own capture/retreat/hold/rebuild policy; main combat strategy does not recall it. No target and not recovering → release on the next reconciliation.
- Combat groups join within `FleetCohesion.RADIUS = 800`; existing members split beyond `1600` from the group anchor. Merge only nearby groups with matching strategy and recovery state.
- Group origin, local force, offensive strength, route safety, capture stall budget and recovery state are independent. Distant or nonassigned ships do not lend strength to a detached capture route.
- Executed capture objectives reserve their IDs for the decision pass; later ship groups select other objectives. Capture detachment evaluates first.
- Available squadrons cover threatened sites first, retain one valid assignment per objective, then choose the least-covered target by distance. Scouts, escorts and active carrier guards are excluded.
- Squadron reservations release on role removal, pooling or target ineligibility; satellite capture retains the owning-mine prerequisite.
- Telemetry publishes the primary combat group's strategy/context with aggregate active/body/rallying counts and an action summary of all groups. It is not a separate telemetry row for each group.
- Implementation and acceptance owner → [[Done/Bugs/Enemy_AI_Independent_Task_Groups]].

## Edge Cases

- Strategic assignment/recovery caches are transient. Restored ownership and orders are reassessed from live state; no save format change.
- Player Idle/Guard behavior is unchanged. No damage-triggered pursuit or separate Providence facing fix was introduced.
- Old Providence `5065384297670371146` save proves Idle/no target, not a damage wake cause. New range/contact/order evidence → [[Tooling/Battle_Telemetry]].
- Large builder/executor classes predate this change and grew with objective policy; a later focused capture-selection extraction is preferable to unrelated refactoring here.
- Runtime acceptance and authored tests remain pending → [[TODOs/User_Acceptance_Queue]].

## Files

- `Assets/Scripts/Services/Enemy/EnemyStrategicContextBuilder.cs`; `EnemyTaskForceExecutor.cs`; `EnemyCaptureObjective.cs`.
- `Assets/Scripts/Entities/EnemyFaction/Models/EnemyStrategicDecisionModel.cs`.
- `Assets/Scripts/Entities/BaseEntity/EntityFacades/IUnitCombatObserverFacade.cs`; ship/squadron installer bindings.
- [[Done/Bugs/Enemy_AI_Defense_And_Zone_Recovery_Plan]]; [[Done/Bugs/Enemy_AI_Defense_And_Zone_Recovery_Plan - Research]].

## Edge Cases

- 2026-10-31 recovery fixes: retreat anchor checks ignore transient destination reservations through `IsPositionClear(..., includeReservations: false)` while retaining map-obstacle, ion-cost and known hostile-force checks. Formation placement still resolves ship reservations.
- Executed capture stall time pauses during other strategies; candidate selection alone never consumes the `20 s` budget. Cooldown remains `20 s` of wall time.
- Fix implementation and verification → [[Done/Bugs/Enemy_AI_Recovery_Review_Fixes]]. Runtime acceptance and authored regression execution remain pending.
