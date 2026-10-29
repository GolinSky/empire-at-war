---
category: Refactoring
status: todo
created: 2026-10-29
---
# Hardpoint Health From Hull — Research

## Method
- Source: `Assets/Prefabs/Models/Ships/<Faction>/<Name>ShipView.prefab` ↔ `Assets/Settings/Data/Ship/<Faction>/<Name>ShipData.asset` (convention in `ShipHardPointValidator.FindShipData`).
- Counted `HardPoint` components with `unlockLevel = 1`; HP = `healthOverride` if > 0 else `hardPointHealth[type].health`.
- Hull = `ShipData.Hull` (base, no research/level). All `hullDamageMultiplier` = `1`.
- Snapshot 2026-10-29, 61 ships.

## Σ hardpoint HP / hull (current)
| Ratio | Ship | Class | Hull | Hardpoints | Σ HP |
|---|---|---|---|---|---|
| 0.16 | Bulwark | Capital | 12750 | 4 | 2000 |
| 0.18 | ImperialVenator | Capital | 9000 | 3 | 1600 |
| 0.21 | Lucrehulk | HeavyCapital | 30000 | 9 | 6200 |
| 0.21 | Recusant | Cruiser | 3800 | 2 | 800 |
| 0.30 | Invincible | Capital | 22500 | 16 | 6800 |
| 0.35 | Mandator | HeavyCapital | 75000 | 62 | 26300 |
| 0.36 | Arquitens | Frigate | 2900 | 3 | 1050 |
| 0.36 | HomeOne | Capital | 22500 | 19 | 8200 |
| 0.37 | Venator | Capital | 7500 | 6 | 2800 |
| 0.44 | MonCalCruiser | Capital | 12750 | 14 | 5600 |
| 0.46 | Acclamator | Frigate | 3500 | 3 | 1600 |
| 0.48 | Providence | Capital | 7500 | 8 | 3600 |
| 0.60 | Rothana | Capital | 15450 | 22 | 9200 |
| 0.64 | HeavyDreadnought | Cruiser | 3600 | 8 | 2300 |
| 0.67 | Allegiance | HeavyCapital | 30000 | 28 | 20200 |
| 0.72 | NebulonB | Frigate | 3600 | 10 | 2600 |
| 0.75 | Peleset | Cruiser | 4000 | 5 | 3000 |
| 0.76 | Resolute | Capital | 10500 | 19 | 8000 |
| 0.76 | Endurance | Capital | 13500 | 22 | 10300 |
| 0.84 | Wavecrest | Frigate | 3100 | 7 | 2600 |
| 0.88 | Recalcitrant | Frigate | 2800 | 7 | 2475 |
| 0.90 | QuasarFire | Frigate | 2000 | 10 | 1800 |
| 0.93 | Titan | HeavyCapital | 42000 | 33 | 39000 |
| 1.03 | Malevolence | HeavyCapital | 50130 | 73 | 51600 |
| 1.08 | ArquitensImperialCruiser | Frigate | 2600 | 10 | 2800 |
| 1.11 | Liberator | Capital | 7500 | 18 | 8350 |
| 1.14 | Imperator | Capital | 11250 | 31 | 12800 |
| 1.31 | MC50 | Cruiser | 4200 | 16 | 5500 |
| 1.32 | Munificent | Frigate | 2000 | 11 | 2650 |
| 1.36 | Procursator | Capital | 9750 | 17 | 13250 |
| 1.37 | AssaultFrigateMkII | Frigate | 3500 | 14 | 4800 |
| 1.40 | Tector | Capital | 18000 | 31 | 25250 |
| 1.41 | AcclamatorAssault | Frigate | 4700 | 14 | 6650 |
| 1.46 | StealthCorvette | Corvette | 850 | 6 | 1240 |
| 1.47 | Victory | Cruiser | 4900 | 17 | 7200 |
| 1.50 | Pelta | Frigate | 2200 | 12 | 3300 |
| 1.54 | Dispatcher | Frigate | 3500 | 22 | 5400 |
| 1.56 | Executor | HeavyCapital | 300000 | 182 | 467000 |
| 1.57 | Carrack | Corvette | 1600 | 14 | 2520 |
| 1.62 | MC30c | Frigate | 2000 | 16 | 3240 |
| 1.71 | CD273 | Corvette | 1600 | 10 | 2740 |
| 1.80 | ISDII | Capital | 14700 | 32 | 26500 |
| 2.00 | Captor | Frigate | 3400 | 15 | 6800 |
| 2.05 | Tartan | Corvette | 600 | 11 | 1230 |
| 2.09 | PatrolFrigate | Corvette | 900 | 10 | 1880 |
| 2.11 | CorellianBattlecruiser | Capital | 7500 | 20 | 15850 |
| 2.24 | Nebula | Capital | 12000 | 31 | 26900 |
| 2.24 | BothanAssaultCruiser | Cruiser | 4900 | 22 | 11000 |
| 2.26 | ISDI | Capital | 12600 | 34 | 28500 |
| 2.29 | CR70 | Corvette | 750 | 9 | 1720 |
| 2.33 | Centax | Frigate | 2000 | 13 | 4650 |
| 2.60 | C9979 | Corvette | 600 | 8 | 1560 |
| 2.75 | Thranta | Corvette | 800 | 12 | 2200 |
| 3.26 | MC80Independence | Capital | 22500 | 43 | 73300 |
| 3.67 | CorellianCorvette | Corvette | 600 | 12 | 2200 |
| 3.67 | RaiderCorvette | Corvette | 600 | 12 | 2200 |
| 3.71 | Dauntless | Cruiser | 4900 | 23 | 18200 |
| 4.15 | ISDIII | Capital | 9750 | 39 | 40500 |
| 4.31 | VictoryI | Cruiser | 3600 | 20 | 15500 |
| 4.38 | VictoryII | Cruiser | 4000 | 22 | 17500 |
| 6.11 | MC75Profundity | Cruiser | 4500 | 34 | 27500 |

## Observations
- 23 ships < `1.0×` → can end with zero live hardpoints and nonzero hull.
- Corvettes already sit at `1.46–3.67×` mostly from flat per-type values → the per-class min floor (`100`) keeps them ≈ `2×`.
- `healthOverride > 0`: 11 hardpoints (`300×4, 600×2, 500×2, 350×2, 575×1`) in Peleset, Recalcitrant, Wavecrest.
- Not counted: hardpoints with `unlockLevel > 1` (none on ships found by the counter), structures (`EntityComponentData`).

## Implementation Evidence — 2026-10-29
- Ships only: `R = 1.15`, proposed type weights and class minimums; structures retain flat HP/overrides/multipliers.
- `HardPointHealthBudget.Calculate` is shared by runtime, simulator and Balance Editor. Saved settings: `Assets/Settings/Data/HardPointHealthSettings.asset`; bound in `Assets/Resources/ProjectContext.prefab`.
- Migrated 62 ship data assets / 61 view prefabs; 11 nonzero overrides → old override / old type HP as relative weights. Removed ship flat HP tables.
- Unity compiled; 123 migrated assets refreshed, reserialized and saved. Console contained zero errors at verification. Regression tests added/updated; not run under local policy.
- Simulator export: `Logs/Rebalance/2026-10-29-hardpoint-health/`; 60 roster units, 305 cross-faction same-class matchups × 10 seeds, zero definition errors.
- Historical comparison: 260 shared matchups; 68 result changes, 38 winner reversals; median current/old TTK ratio `1.6023`. Current roster has 45 additional matchups.
- Historical baseline also predates other weapon/loadout/range changes; result differences do not isolate this HP change. Retain starting values pending user skirmish/tuning acceptance.
- Hull multiplier currently comes from fleet command; `ResearchHealthData` currently decorates shields only. Future hull research must trigger `RefreshStatModifiers` with its hull multiplier.
