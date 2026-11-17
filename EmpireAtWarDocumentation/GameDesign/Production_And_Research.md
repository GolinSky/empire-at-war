---
type: design-reference
system:
  - production
  - research
design_state: implemented
verified: 2026-11-17
verification: Inspected EnemyBuildQueue, PlayerFactionModel, FactionService, EnemyFactionController and the serialized station, population and faction research data (updated 7-class ladder).
---
# Production and Research

## Goal
- Current purchase, queue, deployment, station-upgrade, and research rules.
- Related: [[GameDesign/00_GameDesign]], [[GameDesign/Economy]], [[GameDesign/Units_And_Factions]], [[GameDesign/Capture_And_Structures]], [[GameDesign/Abilities_And_Superweapons]].

## Rules

### Human production
- A living production base is required to admit purchases; hosted players cannot buy upgrades for another player's station.
- The production UI uses `current station level >= FactionData.AvailableLevel` to expose eligible units.
- Admission checks the queue/type limit and superweapon eligibility, then spends the full price and enqueues the request.
- Maximum 7 active request-ID pipelines; different pipelines advance concurrently.
- Requests of the same ID form a FIFO queue; each pipeline builds one item at a time.
- `FactionData.MaxCount` limits the pending count in a pipeline. Completed ships normally leave this queue count.
- Structure requests reserve their type slot through queued, reserve-pool, and deployed states; cancellation or destruction releases it.
- Hero requests have a unique reservation spanning production and owned existence.
- Build progress uses elapsed game time; excess time after one completion advances the next item in the same pipeline.
- Cancel removes the active/head request, retains following requests, and refunds the full price.
- Completed station upgrade → level increases; completed research → next tier applies; completed superweapon charge → weapon becomes ready.
- Completed ships, squadrons, ordinary mines, and platforms enter the player's reinforcement reserve for placement.

### Population and deployment
- Current serialized population limit: 120.
- Human ship/squadron population is checked when creating a reinforcement placement, not when buying or building it.
- Completed reserve units can wait while deployed population is full.
- Human population increments on deployment and is released with the deployed ship/squadron.
- Completed reserves remain deployable after base loss while the battle continues, subject to placement rules.
- New purchases require a production base. Self-hosted ordinary builds can finish into reserve after base loss. Hosted builds and requests that require a specific base can refund on completion if production access is lost.
- Structure placement restrictions are separate from production admission; see [[GameDesign/Capture_And_Structures]].

### AI production
- AI reserves per-type count and population before spending; a failed payment releases that reservation.
- AI reservation counts include pending builds and living deployed units; destruction releases the reservation.
- AI builds sharing a request ID advance FIFO. Up to 7 distinct request-ID pipelines advance concurrently.
- Completed ships awaiting deployment release their production lane. For AI purchase and deployment policy, see [[Architecture/Enemy_AI_Objectives]].
- AI completed units spawn automatically; they do not enter the human placement reserve.
- AI station upgrades spend their price and complete through the timed build queue.
- AI research holds a reservation for its line until completion, preventing a second pending purchase of that line.
- A hosted AI ally follows its host's station level and production availability; it cannot buy a host station upgrade.

## Important Values

### Station progression
| Upgrade | Required level | Credits | Human build time |
|---|---:|---:|---:|
| Level 2 | 1 | 2,000 | 80 s |
| Level 3 | 2 | 3,200 | 120 s |
| Level 4 | 3 | 4,000 | 180 s |
| Level 5 | 4 | 5,500 | 220 s |

- Maximum station level: 5. Default battle builder starts at level 1; Survival starts at level 5.
- Level changes affect available roster entries, station combat stats/model, and station-derived income.
- Station upgrade requests have `MaxCount = 1` and `UnitCapacity = 0`.
- Second Station: 3,000 credits, 90 s, level 1 entry. It starts at level 1, upgrades through its own level chain at the same prices and durations, and shares the production queue, research and limits. A standing second station can keep production available after the main station is lost. Its construction and upgrades add no credit income.

### Research lines
| Line | Required levels | Credits by tier | Time by tier | Latest-tier effect |
|---|---|---|---|---|
| Increased Production | 1, 2 | 250, 500 | 10, 15 s | Facility income ×1.2, ×1.4 |
| Increased Station Income | 4, 5 | 250, 500 | 10, 15 s | Base/station income ×1.2, ×1.4 |
| Improved Weapons | 2, 3, 4 | 1,000, 1,500, 2,000 | 15, 20, 25 s | Weapon damage ×1.25; affected classes expand |
| Enhanced Shields | 2, 3, 4 | 1,000, 1,500, 2,000 | 15, 20, 25 s | Shield capacity ×1.25; affected classes expand |
| Enhanced Engines | 2, 3, 4 | 1,000, 1,500, 2,000 | 15, 20, 25 s | Speed ×1.15; affected classes expand |
| Improved Defenses | 1, 2, 3 | 950, 1,500, 2,800 | 15, 20, 25 s | Structure weapon damage ×1.15, ×1.3, ×1.5 |
| Reinforced Structures | 2 | 1,000 | 20 s | Structure damage taken ×0.85 |

- Republic, Empire, Rebellion: Enhanced Shields. Separatist: Enhanced Engines instead.
- The other five lines appear in all four faction assets.
- Weapons/Shields/Engines tier I affects Corvette + Frigate; tier II adds Cruiser + HeavyCruiser + Capital; tier III adds BattleCruiser + Dreadnought.
- Fighter, Bomber, and Interceptor are absent from those three lines' affected-class arrays.
- Research entries have `MaxCount = 1` and `UnitCapacity = 0`.
- Only the highest completed tier of each line contributes effects; tiers within one line do not stack.
- Effects from separate completed lines multiply if they target the same stat/class.
- Completing a line with no remaining tier throws; the normal research selection must use `TryGetNextTier`.

## Edge Cases
- Human and AI `MaxCount` semantics differ: human queue count versus AI pending-plus-living reservations.
- Human structure lifetime limits and hero uniqueness are explicit exceptions to the queue-only rule.
- Human `FactionService.TryPurchaseUnit` does not itself recheck `AvailableLevel`; level gating is in the production UI. Do not treat this API as a complete admission validator.
- Human placement capacity checks cover ships and squadrons; structure placement does not consume the catalog's population value. AI reserves the `UnitCapacity` on all supported requests.
- Self-hosted AI builds may still complete after its station falls; a squadron with no living launch station refunds instead.
- Hosted AI builds refund if the host loses production; AI build exceptions also release reservations and refund.
- Human cancellation and reinforcement-placement cancellation are different: canceling a placement keeps the completed reserve item.
- Research effects are faction-local; shared facility payments use the facility owner's research, as defined in [[GameDesign/Economy]].
- These asymmetries describe current implementation, not a decision that human/AI behavior should remain different.

## Files
- `Assets/Scripts/Entities/Faction/Model/PlayerFactionModel.cs` — `CanQueueUnit`, `QueueUnit`, `TryCancelCurrentUnit`, `AdvancePipeline`.
- `Assets/Scripts/Services/Faction/FactionService.cs` — `TryPurchaseUnit`, `BuildUnit`, `CancelBuilding`.
- `Assets/Scripts/Entities/Faction/Ui/FactionUiController.cs` — `IsUnitAvailable`.
- `Assets/Scripts/Entities/Reinforcement/ReinforcementModel.cs`; `Assets/Scripts/Services/Reinforcement/ReinforcementService.cs`; `Assets/Scripts/Services/Reinforcement/ReinforcementPlacementFactory.cs` — reserves and placement capacity.
- `Assets/Scripts/Entities/EnemyFaction/Controllers/EnemyFactionController.cs`; `Assets/Scripts/Entities/EnemyFaction/Models/EnemyUnitLimitModel.cs`; `Assets/Scripts/Entities/EnemyFaction/Models/EnemyBuildQueue.cs` — AI purchase/reservation/completion.
- `Assets/Scripts/Services/Station/SelfProductionAccess.cs`; `Assets/Scripts/Services/Station/HostedProductionAccess.cs`; `Assets/Scripts/Services/Station/HostedFactionLevel.cs` — production host rules.
- `Assets/Scripts/Entities/Faction/Model/Research/FactionResearchModel.cs`; `Assets/Scripts/Entities/Faction/Model/Research/ResearchStat.cs`; `Assets/Scripts/Components/Health/Data/ShipClass.cs` — effects and class mapping.
- `Assets/Settings/Data/Reinforcement/ReinforcementData.asset` — population cap.
- `Assets/Settings/Data/Factions/Shared/StationLevelData.asset` — station upgrade price/time/level.
- `Assets/Settings/Data/Factions/Republic/RepublicFaction.asset`; `Assets/Settings/Data/Factions/Separatist/SeparatistFaction.asset`; `Assets/Settings/Data/Factions/Empire/EmpireFaction.asset`; `Assets/Settings/Data/Factions/Rebellion/RebellionFaction.asset` — roster and research values.
