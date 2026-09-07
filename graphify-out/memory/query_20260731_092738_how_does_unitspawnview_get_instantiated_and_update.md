---
type: "query"
date: "2026-07-31T09:27:38.957743+00:00"
question: "How does UnitSpawnView get instantiated and updated during drag-and-drop reinforcement spawning?"
contributor: "graphify"
source_nodes: ["UnitSpawnView", "ReinforcementService", "IMapModelObserver"]
---

# Q: How does UnitSpawnView get instantiated and updated during drag-and-drop reinforcement spawning?

## Answer

ReinforcementService caches a horizontal Quaternion.LookRotation from the player station to the opponent station during Initialize. Each instantiated UnitSpawnView receives that cached world rotation once through SetRotation, while Tick only updates its drag position and placement validity.

## Source Nodes

- UnitSpawnView
- ReinforcementService
- IMapModelObserver