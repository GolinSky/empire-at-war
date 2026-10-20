---
type: "query"
date: "2026-10-20T10:05:00.061108+00:00"
question: "economy tuning: income per level, battle asteroid, asteroid and spawnable mining facilities, income research bonuses"
contributor: "graphify"
outcome: "useful"
source_nodes: ["EconomyService", "MiningFacility", "FactionResearchModel"]
---

# Q: economy tuning: income per level, battle asteroid, asteroid and spawnable mining facilities, income research bonuses

## Answer

Query terms: Income, Mining, Research, Level, AsteroidMiningFacilityFactory, EconomyService. Verified live source: income tick 1 second. Base income is 1 plus 5 times faction level, levels 1-5 give 6,11,16,21,26 credits per second. Spawnable mine adds 10, asteroid mine adds 20, battle asteroid is a DefendPlatform with no income provider. Each allied economy receives full mining income. All four faction assets Increased Production I: level1,250 credits,10 seconds,multiplier1.2; II: level2,500 credits,15 seconds,multiplier1.4. Only latest completed tier applies, so 40 percent total at tier II. Research affects mining facility income only, not base level income.

## Outcome

- Signal: useful

## Source Nodes

- EconomyService
- MiningFacility
- FactionResearchModel