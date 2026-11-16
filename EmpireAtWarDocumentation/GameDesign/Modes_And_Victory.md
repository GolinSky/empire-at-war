---
type: design-reference
system:
  - modes
  - victory
design_state: implemented
verified: 2026-11-16
verification: Inspected current match rules, mode presets, BattleVictoryService, SurvivalOutcomeRule and SurvivalWaveDirector. Counted authored wave entries. No runtime checks.
---
# Modes and Victory

## Goal

- Current rules for Skirmish, Survival and runtime Battle Simulation.

## Rules

### Skirmish

- 2–8 players overall. Map caps: Small `4`, Medium `6`, Large `8`.
- Offline requires exactly one human. Online requires at least one human and permits multiple.
- At least two opposing teams. Each slot uses a unique color from the 16-color palette.
- Players choose faction/team; the setup supplies planet, map size, victory condition, starting credits and superweapon toggle.
- Full map generation uses a seed. A fresh battle chooses a new seed; a saved Skirmish rebuilds with its recorded seed.
- Human and AI opponent roles own stations and production. AI players may share the human's team.
- Starting station level is 1. Economy, fog, capture sites and mining are enabled; superweapons can be disabled.
- Code fallback setup: Republic human vs Separatist AI, Medium difficulty, Coruscant, Small map, 1,000 credits, destroy-all victory.

### Skirmish victory

- A player becomes eligible for defeat after a base has been observed alive; delayed initial spawning does not trigger defeat.
- `DestroyOpponentBase` → no living player base remains. A second station also counts as a base.
- `DestroyEnemyFleet` → no living base, ships, non-base structures, sites under construction, or non-squadron reserves.
- Deployed and reserve squadrons alone cannot keep a player in the battle.
- Teams remain alive while any player on that team remains alive.
- No surviving teams → draw. Local team eliminated → defeat. Only the local team survives → victory.
- Outcome evaluation runs during `BattleState.Running`; the first result is retained.

### Survival

- Solo or online co-op humans plus an optional AI ally versus one wave-attacker owner. Co-op requires a Medium or Large map.
- Setup selects player/ally/enemy faction, map size, AI difficulty and superweapon toggle; the Survival preset uses Coruscant.
- Each human has a station starting at level 5. The AI ally uses hosted production and has separate player state.
- Wave attackers have no station, economy, research or normal production. Waves spawn authored units directly.
- Five waves. Preparation is 120 s before wave 1 and 60 s between waves; Start Wave Now ends the remaining preparation.
- A wave clears after its pending spawn queue is empty, no living wave-attacker entities remain, and the 1 s spawn-settle delay expires.
- Surviving carrier-launched squadrons count as remaining attackers.
- Clear wave 5 → victory. Lose every human player's living base → defeat.
- Wave units receive Attack Move toward the first human with a living station.
- Credits and production follow [[GameDesign/Economy]] and [[GameDesign/Production_And_Research]]; the wave director grants no kill bounty.

### Battle Simulation

- Two opposing AI-controlled sides; both fleets must contain at least one entry.
- The setup records factions, ship/squadron fleet entries, starting distance, time limit, normal/fast speed, and whether to use hangars and abilities.
- Empty Large map at Coruscant; no economy, stations, mining, capture sites, fog or superweapons.
- Initial draft defaults: distance 2,000 world units, time limit 300 s, normal speed.
- Outcome evaluation waits for initial spawning to finish.
- Side A survives alone → A wins; side B survives alone → B wins; both empty or time limit reached → draw.
- Results retain elapsed simulation time, surviving unit counts and remaining hull totals.
- HUD supports pause, 1×/4× speed, restart with the same setup, and a new setup.
- This mode uses runtime entities; it is separate from the Editor Battle Logic Simulator.

## Important Values

- Base wave totals below are from `SurvivalWaveData.asset`. All four factions have these totals with faction-specific units.
- Each additional defender, including the AI ally, adds 50% of each authored quantity. The director rounds each entry up.
- Direct squadrons are counted as squadron groups, not individual fighter craft; carrier launches are additional.

| Wave | Ships | Direct squadrons | Spawn groups |
|---|---:|---:|---:|
| 1 | 4 | 2 | 1 |
| 2 | 14 | 3 | 2 |
| 3 | 28 | 4 | 3 |
| 4 | 38 | 5 | 4 |
| 5 | 52 | 6 | 4 |

## Edge Cases

- Ship unlock tier changes → update `SurvivalWaveData.asset` for every faction. Per-tier ship totals are 2 × the largest faction roster for that tier; `SurvivalGameplayTests` checks totals and unlock coverage.
- General battle time scale: normal 1, fast 4, paused/ended 0. Preparation and Simulation elapsed time use scaled battle time.
- Save availability and format: [[GameDesign/Save_And_Load]].
- Source verification establishes authored behavior, not combat difficulty, faction fairness or complete runtime acceptance.

## Files

- `Assets/Scripts/Entities/Game/SkirmishPreset.cs`, `Assets/Scripts/Entities/Game/SurvivalPreset.cs`, `Assets/Scripts/Entities/Game/SimulationPreset.cs` — mode definitions.
- `Assets/Scripts/Entities/Players/MatchRules.cs` — player/team/color validation.
- `Assets/Scripts/Entities/Game/BattleVictoryModel.cs`, `Assets/Scripts/Entities/Game/SurvivalOutcomeRule.cs`, `Assets/Scripts/Entities/Game/SimulationModel.cs` — outcomes.
- `Assets/Scripts/Services/BattleService/BattleVictoryService.cs` — living entity and pending-build counts.
- `Assets/Scripts/Services/BattleService/SurvivalWaveDirector.cs`; `Assets/Scripts/Entities/Game/SurvivalModel.cs` — wave progression.
- `Assets/Settings/Data/Battles/SurvivalWaveData.asset` — faction rosters, quantities and preparation seconds.
- `Assets/Scripts/Entities/Game/SimulationSetup.cs`, `Assets/Scripts/Entities/Game/SimulationDraft.cs`, `Assets/Scripts/Entities/Game/SimulationHudUiController.cs`.
- `Assets/Scripts/Entities/CoreGame/SkirmishOrchestrator.cs` — time scale and lifecycle.
- [[GameDesign/00_GameDesign]], [[GameDesign/Units_And_Factions]], [[GameDesign/Capture_And_Structures]].
