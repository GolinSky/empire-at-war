---
category: Bugs
status: done
created: 2026-10-31
record_state: historical-evidence
system:
  - ai
  - capture
  - combat
completed: 2026-10-31
acceptance: pending-user
---
# Enemy AI Defense and Zone Recovery — Research

## Goal

- Preserve the 2026-10-30 read-only investigation for [[Done/Bugs/Enemy_AI_Defense_And_Zone_Recovery_Plan]].
- Distinguish source-confirmed defects, battle observations and unresolved individual behavior.

## Important Values

| Evidence | Observed value |
|---|---|
| Match | Player Republic; Ultra Hard Separatist AI; DestroyEnemyFleet |
| Reinforcement zones | All 161 battle status entries: ControlledZones=0; default home excluded |
| Late save | All 3 capturable reinforcement zones player-owned; 2 operational facilities enemy-owned |
| Continuous retreat | AI seconds 576.1–1060.8: about 485 s |
| Recovery example | 1189.6: local advantage 14.39, global advantage 0.16; recovery still forces retreat |
| Base defense | 0 DefendBase decisions; maximum logged base threat 0.12 |
| Threat geometry | 100-unit station-center radius; Ultra Hard defense starts scoring above 1.125 |
| Saved attackers | 9 player ships target enemy station, approximately 1,196–1,394 planar units away |
| Saved station health | Hull 96.72%; shields 17.61% |
| Guard leash | guardChaseDistance=240 |
| Saved Providence | ID 5065384297670371146; no active order/target; hull 96.92%, shields 21.53% |
| Providence contacts | Nearest player ship 884.8 units; hostile squadron 399.9 units; serialized radar 750 before runtime modifiers |

## Implementation

### Confirmed source behavior

- `EnemyStrategicContextBuilder.BuildBaseThreat` (line 446) checks only station-center proximity; it does not establish bombardment pressure from distant attackers.
- `EnemyTaskForceExecutor.Execute` (line 114) issues Guard for DefendBase; `GuardState.Tick` / `FindTarget` apply the 240-unit leash. Enlarging strategic detection alone is insufficient.
- `EnemyStrategicDecisionModel.Evaluate` (line 230) overrides capture/local engagement during recovery until regrouped and global advantage reaches HuntAdvantage (Ultra Hard 1.0).
- `EnemyStrategicDecisionModel` (line 140) scores missing reinforcement-zone ownership; `EnemyStrategicContextBuilder.TryGetClosestCaptureTarget` (line 399) chooses nearest reinforcement zone OR facility.
- Capture uses AttackMove; contacts can divert ships. `EnemyTaskForceExecutor` (line 88) accepts an assigned ship with no order without verifying arrival/progress.
- `ShipOrderRunner` routes Retreat to navigation, then Idle on completion. `EnemyTaskForceExecutor.ResolveWithout` (line 254) skips assigned ships with no order during unchanged retreat.
- `IdleState` (line 47) turns toward eligible in-range contacts without pursuit; autonomous weapons can still fire. No order does not mean disabled weapons.

### Battle interpretation and limits

- Early CaptureZone begins at 22.8 s; switches to HuntFleet at 115.7, returns at 130.3, first retreats at 154.2, captures again at 176.9.
- Two enemy-owned facilities corroborate that CaptureZone is not reinforcement-zone-specific. Missing target IDs prevent identifying each early destination.
- Logs omit capture destination/progress and per-ship order/target history; exact early capture failure and Providence wake trigger remain unproven.
- Reviewed combat/order paths do not establish damage-triggered activation. Attackers entering contact/range is plausible, not proven.
- Reported station-facing orientation is not verified for the observed moment. Saved rotation alone cannot establish earlier facing or its cause.
- Main-zone-loss recovery is missing as a dedicated policy; the exact reported loss event is not reconstructed from these logs. Recheck station/home/relay semantics before implementing.
- AI CSV times and save battle time use different clocks; do not directly align their numeric timestamps.
- Source/serialized-value inspection only; no code changes, tests or runtime reproduction performed during investigation.

## Files

- AI decisions: `C:/Users/golin/AppData/LocalLow/DefaultCompany/EmpireAtWar/BattleTelemetry/Loading_20261030_232329_ai.csv`.
- Combat totals: `C:/Users/golin/AppData/LocalLow/DefaultCompany/EmpireAtWar/BattleTelemetry/Battle_20261030_232525.csv`; Providence totals aggregate multiple ships.
- Editor log: `C:/Users/golin/AppData/Local/Unity/Editor/Editor.log`, reviewed battle section from approximately line 79061.
- Save: `C:/Users/golin/AppData/LocalLow/DefaultCompany/EmpireAtWar/Saves/Skirmish/autosave.json`; battleTime approximately 1200.016.
- User analysis: `C:/Users/golin/.codex/attachments/a7cb4706-37c7-4bac-926e-3a9dc17e5eb4/Pasted text.txt`.
- Runtime logs/autosave are mutable; these figures describe the reviewed snapshot, not guaranteed future contents.
- Source path inventory and remediation owner → [[Done/Bugs/Enemy_AI_Defense_And_Zone_Recovery_Plan]].

## Implementation

- 2026-10-31: retained AI log/save rechecked; Providence ID/order/hull/shields and the 14.39-local/0.16-global recovery example remain present.
- Implemented station interception, typed capture/recovery, safe-anchor reassessment and bounded per-ship order/range/contact telemetry.
- Historical source descriptions above remain dated pre-fix evidence. Current contracts → [[Architecture/Enemy_AI_Objectives]]; telemetry → [[Tooling/Battle_Telemetry]].
- Official Unity 6000.4.7f1 compilation and source/diff checks completed; no tests, Play Mode, new battle logs or separate facing/wake reproduction.
- Runtime gates → [[TODOs/User_Acceptance_Queue]]. New telemetry is required for before/after incoming-damage attribution.
