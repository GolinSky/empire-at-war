---
type: "query"
date: "2026-07-12T14:42:42.770256+00:00"
question: "How are ShipAIBrain, enemy ship commands, ship state machines, EnemyService, and space-station targeting connected?"
contributor: "graphify"
source_nodes: ["ShipAIBrain", "EnemyUnitCommander", "StateMachine1", "ShipService", "EntityLocator"]
---

# Q: How are ShipAIBrain, enemy ship commands, ship state machines, EnemyService, and space-station targeting connected?

## Answer

Ship now owns and ticks its basic state machine, beginning in IdleState. ShipAIBrain is enabled only for opponent ships and reacts locally to health and threat conditions while executing an externally assigned target. EnemyUnitCommander is the global enemy coordinator: it discovers the player space station through IEntityLocator and assigns it to opponent ships registered with ShipService; player ships are never ordered by this logic.

## Source Nodes

- ShipAIBrain
- EnemyUnitCommander
- StateMachine1
- ShipService
- EntityLocator