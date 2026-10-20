---
type: design-reference
system:
  - economy
design_state: implemented
verified: 2026-10-20
verification: source-and-assets
---
# Economy

## Goal
- Current credit rules, income sources, spending, and refunds.
- Related: [[GameDesign/00_GameDesign]], [[GameDesign/Production_And_Research]], [[GameDesign/Capture_And_Structures]], [[GameDesign/Modes_And_Victory]].

## Rules
- Each economy-enabled player has an independent credit balance; purchases spend from that player's wallet.
- Human and AI players use the same `EconomyModel` and `EconomyService`; the active battle's `StartingMoney` overrides the economy asset's starting amount.
- `TrySpend` fails without changing the balance when credits are below the price; an equal balance is sufficient.
- Income is added only while `BattleState.Running`. Each eligible timer completion adds one payment and restarts the timer.
- Income providers are summed; displayed `TotalIncome` is credits per payment, not a separate continuously accrued balance.
- Provider addition/removal, station-level changes, and completed research recalculate the payment.

## Important Values

### Starting credits
| Entry path | Credits |
|---|---:|
| Skirmish setup default | 2,000 per player |
| Skirmish setup range | 500–10,000; 100-credit steps |
| `SkirmishPreset.Default()` | 1,000 per player |
| Survival builder default | 1,000 per economy-enabled player |
| `EconomyData.StartMoneyAmount` | 15,000; direct data-only model constructor |
| Simulation | 0; economy feature disabled |

- Skirmish selection rounds to the nearest 100 and clamps to its range.
- Survival starts the human and optional AI ally at station level 5; wave attackers have no economy feature.
- Loading restores saved credits; the setup default does not replace a restored balance. See [[GameDesign/Save_And_Load]].

### Recurring income
| Provider | Credits per payment before research | Recipient |
|---|---:|---|
| Economy service base | 3 | Own player |
| Human/AI faction station-level provider | `15 × current station level` | Own player |
| Ordinary Mining Facility | 20 | Owner and every allied player |
| Asteroid Mine | 60 | Owner and every allied player |
| Battle Asteroid | 10 | Owner and every allied player |

- `EconomyData.IncomeDelay = 1 s`; nominal payment rates therefore equal credits/second while continuously running.
- Own fixed income = `(3 + 15 × level) × StationIncomeMultiplier`.
- Level 1 → 18 credits/payment; level 5 → 78 credits/payment before research and facilities.
- Team facility contribution = `facility base income × facility owner's IncomeMultiplier`.
- Full facility income is copied to each allied wallet; it is not divided among allies.
- The receiving ally's research does not multiply the same facility a second time.
- Research applies to separate sources: `IncreasedProduction` → facility income; `IncreasedStationIncome` → both own base and station-level income.
- Example: level 1 + one ordinary mine + one asteroid mine, no research → `18 + 20 + 60 = 98` credits/payment.

### Economy research
| Line | Tier I: level / price / time / multiplier | Tier II: level / price / time / multiplier |
|---|---|---|
| Increased Production | 1 / 250 / 10 s / ×1.2 | 2 / 500 / 15 s / ×1.4 |
| Increased Station Income | 4 / 250 / 10 s / ×1.2 | 5 / 500 / 15 s / ×1.4 |

- These entries are present in all four faction assets.
- Tier II replaces Tier I; the result is ×1.4, not ×1.68. The two lines affect different income sources.
- Prices and construction times for facilities, platforms, ships, and station upgrades live in their catalog/roster assets; see [[GameDesign/Production_And_Research]] and [[GameDesign/Capture_And_Structures]].

## Edge Cases
- Human production payment occurs on queue admission; canceling the active item refunds its full catalog price regardless of elapsed build time.
- Human completion with no production base refunds the item. AI failed builds and lost hosted production also refund; AI behavior after its own station falls differs.
- Capture-site construction spends immediately from the payer; enemy recapture discards unfinished construction without a refund path.
- A human can pay to build at an allied owned site; the finished facility belongs to the site owner and uses that owner's research.
- Destroyed facilities stop contributing to every allied economy.
- The fixed income formula depends on the faction level, not a living-station check; station loss must not be documented as automatically setting income to zero.
- Difficulty profiles change AI decision cadence and priorities; they contain no income multiplier. AI station upgrades complete immediately after payment; human upgrades take time.
- No upkeep, resource depletion, trade, or kill bounty is implemented in the reviewed economy/provider paths; treat such mechanics as proposals until separately implemented.
- Verification: live source and serialized assets read on 2026-10-20; no runtime test was run for this reference.

## Files
- `Assets/Scripts/Entities/Economy/Model/EconomyModel.cs` — constructors, `TrySpend`, `AddMoney`, `Restore`.
- `Assets/Scripts/Services/Economy/EconomyService.cs` — `Income`, `Tick`, `Refund`, `CalculateBaseIncome`.
- `Assets/Scripts/Services/Faction/FactionService.cs` — `Income`, `BuildUnit`, `CancelBuilding`.
- `Assets/Scripts/Entities/EnemyFaction/Controllers/EnemyFactionController.cs` — `Income`, `Purchase`, `ExecuteBuild`.
- `Assets/Scripts/Entities/MiningFacility/MiningFacility.cs`; `Assets/Scripts/Entities/DefendPlatform/DefendPlatform.cs` — team income registration and release.
- `Assets/Scripts/Entities/Faction/Model/Research/FactionResearchModel.cs` — `RecalculateMultipliers`.
- `Assets/Scripts/Entities/MainMenu/Skirmish/SkirmishModel.cs`; `Assets/Scripts/Entities/Game/SkirmishPreset.cs`; `Assets/Scripts/Entities/Game/SurvivalPreset.cs`; `Assets/Scripts/Entities/Game/BattleDefinitionBuilder.cs` — setup amounts.
- `Assets/Scripts/Services/VContainer/Battle/Modules/EconomyModule.cs`; `Assets/Scripts/Entities/EnemyFaction/Models/EnemyAiDifficultyProfile.cs` — enabled players and AI differences.
- `Assets/Settings/Data/Economy/EconomyData.asset` — payment interval and data-only starting amount.
- `Assets/Settings/Data/Models/MiningFacilities/MiningFacilityData.asset`; `Assets/Settings/Data/Models/MiningFacilities/AsteroidMiningFacilityData.asset`; `Assets/Settings/Data/Models/DefendPlatform/AsteroidDefendPlatformData.asset` — facility income.
- `Assets/Settings/Data/Factions/Republic/RepublicFaction.asset`; `Assets/Settings/Data/Factions/Separatist/SeparatistFaction.asset`; `Assets/Settings/Data/Factions/Empire/EmpireFaction.asset`; `Assets/Settings/Data/Factions/Rebellion/RebellionFaction.asset` — research prices and effects.
