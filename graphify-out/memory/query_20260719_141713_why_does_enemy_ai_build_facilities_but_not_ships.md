---
type: "query"
date: "2026-07-19T14:17:13.639433+00:00"
question: "Why does enemy AI build facilities but not ships, and how should reinforcement placement work?"
contributor: "graphify"
source_nodes: ["ReinforcementService", "EnemyFactionController", "TimerPoolWrapperService", "FogOfWarSystem"]
---

# Q: Why does enemy AI build facilities but not ships, and how should reinforcement placement work?

## Answer

Enemy strategy does request ships, but delayed production callbacks stayed queued because TimerPoolWrapperService was lazily instantiated after tick registration. Binding it non-lazily restores production. Player ships are restricted to player-owned reinforcement zones; player mining facilities and defense platforms require a non-hidden FogOfWarSystem position. Enemy ships use opponent-owned zones, while enemy facilities use random map-bounded coordinates.

## Source Nodes

- ReinforcementService
- EnemyFactionController
- TimerPoolWrapperService
- FogOfWarSystem