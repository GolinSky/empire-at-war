---
type: "query"
date: "2026-07-19T14:39:08.927289+00:00"
question: "Why did enemy ships spawn outside the map instead of inside their default reinforcement zone?"
contributor: "graphify"
source_nodes: ["ShipMoveModel", "ShipMoveComponent", "EnemyUnitCommander", "ReinforcementZonesSystem"]
---

# Q: Why did enemy ships spawn outside the map instead of inside their default reinforcement zone?

## Answer

Enemy ShipMoveModel used a 1000-unit hyperspace JumpPosition outside the map, and movement or stop commands could interrupt the arrival sequence. Enemy JumpPosition now starts at HyperSpacePosition, which is the opponent-owned reinforcement-zone coordinate. ShipMoveComponent queues target commands and ignores stop cancellation until hyperspace arrival completes, then applies the latest pending target. Runtime verification showed requested and initial X/Z coordinates match, with the initial transform inside both the map and opponent zone.

## Source Nodes

- ShipMoveModel
- ShipMoveComponent
- EnemyUnitCommander
- ReinforcementZonesSystem