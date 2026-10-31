---
category: Bugs
status: done
created: 2026-10-31
system:
  - ai
  - capture
  - combat
completed: 2026-10-31
acceptance: pending-user
---
# Enemy AI Defense and Zone Recovery

## Goal

- Defend stations under long-range attack; arrived retreating ships receive useful tactical orders.
- Secure viable reinforcement zones and recover territory after losing home access.
- Explain and verify Providence idle/wake behavior with per-ship evidence.

## Rules

- Status: implemented and source/compilation verified 2026-10-31; acceptance pending user. Historical evidence rechecked; no tests or Play Mode run.
- Follow-up to [[TODOs/Bugs/Enemy_AI_And_Faction_Balance]]; this plan owns defense, retreat recovery, reinforcement-zone selection and idle-ship follow-up.
- Evidence and confidence → [[Done/Bugs/Enemy_AI_Defense_And_Zone_Recovery_Plan - Research]].
- Use current source and serialized settings; recheck changes since investigation before implementation.
- Test execution and test preparation remain disabled for Codex/Claude unless explicitly requested. Author regression coverage; use source, compilation and non-test checks.
- Affected categories: `EnemyAi`, `Capture`, `Ships`, `Squadrons`, `BattleInput`; constructor/fake-only fixture changes also touch `Navigation` and `Simulation`. No categories executed.
- Preserve player-issued Idle/Guard behavior, fog-of-war constraints, allied production ownership and ordinary safe-retreat behavior.

## Decision

- Detect station pressure separately from enemy/own strength ratio. Why: a small bombardment force still needs a response; fleet strength should size the response.
- Use known attackers plus footprint/range-aware threat geometry. Avoid: radius-only tuning, omniscient targeting and copying the observed 1,400-unit distance as a universal constant.
- Intercept actual threatening hostiles; a station Guard order with a 240-unit leash is insufficient.
- Release regrouped ships for viable local objectives without requiring global parity. Keep stronger readiness requirements for full-fleet offensives.
- Give capture objectives stable identity/type and progress. The reinforcement-zone minimum must select reinforcement zones, not facilities.
- Keep damage-triggered activation and incorrect facing as unproven symptoms until telemetry identifies a cause. Avoid adding a speculative retaliate-on-damage behavior.

## Implementation

### 1. Establish evidence and diagnostics

- [x] Recheck the source paths and battle evidence in the research companion; identify existing tests and current telemetry contracts.
- [x] Extend AI telemetry with objective ID/type/owner, destination, distance, capture progress, regroup state, recovery duration and selected/rejected action reasons.
- [x] Record per-ship order transitions, completion/cancellation reason, assignment, selected target and relevant contact/range state; include timestamp and stable entity ID.
- [x] Record station pressure source and defense target; distinguish strategic state from actual issued orders and completed navigation.
- [x] Emit transitions and bounded samples through existing telemetry; avoid per-frame full-fleet logging.
- Verify: a log reader can distinguish approaching, fighting en route, arrived, capturing, stalled and idle; clock origin is explicit.

### 2. Detect and respond to station attacks

- [x] Replace the 100-unit station-center-only threat test with geometry derived from actual station footprint and attack reach; include legitimate known attack/damage evidence.
- [x] Separate attack presence/urgency from force advantage; small hostile forces cannot bombard indefinitely merely because their threat ratio is low.
- [x] Select visible station-threatening targets and issue interception/combat orders; respect lost visibility and target destruction.
- [x] Allow available arrived/rebuilding ships to defend. Ensure any retained Guard path uses compatible footprint-aware limits.
- Verify: attackers at the recorded 1,196–1,394-unit distances trigger a response when actually threatening the station, and defenders can reach engagement range.
- Verify: unrelated distant or hidden enemies do not trigger omniscient pursuit; target loss ends or redirects defense.

### 3. Recover from retreat and home loss

- [x] Separate regroup completion from global offensive readiness; remove the global-parity veto on otherwise viable capture/local defense.
- [x] Reassess threats and reachable safe objectives after regrouping; use local viability and existing hysteresis to avoid capture ↔ retreat oscillation.
- [x] Revalidate the retreat anchor when station/home access is lost, occupied or threatened. Distinguish default home spawn access, station survival and capturable relay ownership.
- [x] Select a viable reclaim target or alternative zone/anchor; reject unsafe routes instead of repeatedly retreating toward an unusable location.
- [x] Handle no safe objective explicitly with a logged hold/rebuild decision; do not promise recovery against an unbeatable force.
- Verify: local advantage 14.39/global advantage 0.16 cannot be rejected solely by global parity; actual local safety and cohesion still apply.
- Verify: home loss causes reassessment without requiring another ship to take damage; a lost/destroyed anchor cannot remain the destination.

### 4. Secure reinforcement zones and monitor progress

- [x] Carry objective ID/type through context and executor; while below `MinimumControlledZones`, choose a viable reinforcement zone explicitly.
- [x] Keep emergency facility defense separate; after the zone floor is met, rank facilities and additional zones normally.
- [x] Evaluate target-local threat and reachability; consider other viable zones when the nearest is unsafe or unavailable.
- [x] Retain objective commitment through ordinary movement; bound combat diversion around capture objectives without resetting valid engagements every decision tick.
- [x] Treat `CurrentOrder.None` as completed arrival only when position/objective state supports it. Reissue or replan for idle ships outside the objective area.
- [x] Track ownership/progress changes and measured lack of progress; log why a target is retained, abandoned or replaced.
- Verify: a closer facility cannot satisfy or continually displace the reinforcement-zone floor; ownership change/destruction invalidates stale objectives.
- Verify: ships inside the circle remain assigned; stopped ships outside it recover; an unsafe nearest zone permits a safer alternative.

### 5. Resolve idle Providence behavior

- [x] Replace permanent assignment suppression after retreat arrival with explicit availability for defense and safe local orders.
- [x] Trace Providence `5065384297670371146` where retained evidence allows; capture new per-ship order/contact/facing evidence if the symptom recurs.
- [x] Instrument effective radar/weapon range, modifiers, completion and targeting comparisons; historical before/after evidence is unavailable. New-recording comparison → user acceptance queue.
- [x] Correct any reproduced order/target/facing defect at its responsible layer; if no separate defect is found, record how defense/recovery changes address inactivity and retain the evidence limitation.
- Verify: an arrived Providence receives a valid defense assignment during station bombardment before it is personally attacked; ordinary Idle does not gain unsolicited pursuit.

### 6. Verification and closure

- [x] Add focused regressions for station pressure/interception, regroup exit, home loss, typed zone priority, alternate targets and capture-arrival recovery.
- [x] Review target lifecycle, fog/cloak handling, team/production-host behavior, no-base/no-ships cases and save/load reconstruction of assignment state.
- [x] Check source/diff and Unity compilation; if serialized assets change, import, save and verify persistence with official Unity tooling.
- [x] Record automated tests as not run unless explicitly authorized; never treat authored coverage as passing coverage.
- [x] Queue user-only acceptance: late-game bombardment, safe-zone expansion and home-loss recovery; Ultra Hard Republic/CIS with both player factions. Not run.
- [x] Queue new decision/per-ship log comparison: defense orders, ownership/progress, retreat duration, interruption reasons and Providence activation. Not runtime-verified.
- [x] Extract verified reusable guidance into relevant canonical references; record destination links or an explicit no-reusable-guidance reason.
- [x] Close only when all implementation scope is complete; queue remaining user-only gates in [[TODOs/User_Acceptance_Queue]], move plan/research to `Done/Bugs`, move the full backlog entry and repair/read back links.

## Important Values

- Station interception uses visible known attackers, effective live reach and both footprints; no universal 1,400-unit radius.
- Capture stall = 20 s without closer approach/progress; rejected-objective cooldown = 20 s.
- Capture diversion leash = closest approach + max(800, capture radius + 2 × ship navigation radius).
- Samples = 5 s; AI/order flush = 10 s, quit and Editor Play exit. Transition rows are unsampled.
- Rechecked retained save: Providence `5065384297670371146`, no order/target, hull 96.92%, shields 21.53%; no evidence proving separate facing/damage activation defect.
- Rechecked retained AI log: 0 DefendBase rows; 1189.6 s local 14.39/global 0.16 still RetreatValue.

## Edge Cases

### Outcome and verification

- Context selects typed capture objectives, station interception target and safe home/allied-relay retreat anchors. Candidate rejection, retention and stall reasons reach telemetry.
- Executor repairs stopped-outside capture orders, retains valid engagements/arrived assignments, bounds diversion and reissues changed retreat anchors.
- Regrouped ships can act locally below global parity; scattered home-loss ships can rally via viable local capture/engagement instead of permanent Hold.
- Read-only combat observer bound to ships and squadrons; no player Idle/Guard pursuit or speculative damage response added.
- `git diff --check` clean; official Unity 6000.4.7f1 compilation completed with no errors; current captured error console empty.
- Tests authored, never executed. No test preparation, Play Mode or serialized asset edits. Source imports/.meta files persisted through Unity compilation.
- Source-reviewed target death/visibility/cloak, team/production host, no-base/no-ships and save reconstruction. Ownership restores before units; orders restore after spawn; transient AI assignments are rebuilt.
- Physical route safety checks known threats, ion cost and clear capture-area samples; full path connectivity is not proven. Stall detection replans failed approaches.
- Canonical extraction → [[Architecture/Enemy_AI_Objectives]], [[Tooling/Battle_Telemetry]], [[GameDesign/Capture_And_Structures]].
- Builder/executor were already >200 lines; later focused capture-policy extraction is suggested, outside this fix.
- User-only gates → [[TODOs/User_Acceptance_Queue]]; no before/after runtime result claimed.

## Files

- `Assets/Scripts/Services/Enemy/EnemyStrategicContextBuilder.cs` — threat/context/capture selection.
- `Assets/Scripts/Entities/EnemyFaction/Models/EnemyStrategicDecisionModel.cs`; `EnemyAiDifficultyProfile.cs` — priorities and recovery.
- `Assets/Scripts/Services/Enemy/EnemyTaskForceExecutor.cs`; `EnemyUnitCommander.cs` — assignments and issued orders.
- `Assets/Scripts/Entities/Ship/Orders/ShipOrderRunner.cs`; `Assets/Scripts/Entities/Ship/StateMachine/{IdleState,GuardState,AttackMoveState}.cs` — order lifecycle.
- `Assets/Scripts/Services/Enemy/EnemyAiTelemetry.cs`; [[Tooling/Battle_Telemetry]] — diagnostics.
- `Assets/Settings/UnitOrderSettings.asset`; `Assets/Settings/Data/Ship/Separatist/ProvidenceShipData.asset` — current range settings.
- [[GameDesign/Capture_And_Structures]]; [[TODOs/Bugs/Enemy_AI_And_Faction_Balance]]; [[TODOs/TODOs]].

## Edge Cases

- Independent source review 2026-10-31 found two recovery defects: own destination reservations invalidate retreat anchors; inactive capture candidates consume stall timers. Fix ownership → [[Done/Bugs/Enemy_AI_Recovery_Review_Fixes]]. Follow-up fixes implemented 2026-10-31 with Unity compilation and source checks; authored regressions and gameplay acceptance remain pending.
- Review used live source/diffs; no tests, Play Mode or fresh compilation performed. Runtime acceptance remains pending.
