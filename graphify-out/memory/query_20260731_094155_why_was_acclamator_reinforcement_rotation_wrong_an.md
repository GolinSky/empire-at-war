---
type: "query"
date: "2026-07-31T09:41:55.505713+00:00"
question: "Why was Acclamator reinforcement rotation wrong and why did spawned ships keep the old rotation during hyperspace?"
contributor: "graphify"
source_nodes: ["AcclamatorReinforcementView", "UnitSpawnView", "ReinforcementService", "StationFacingService", "ShipMoveModel", "ShipMoveComponent", "ShipMovementTweenPlayer"]
---

# Q: Why was Acclamator reinforcement rotation wrong and why did spawned ships keep the old rotation during hyperspace?

## Answer

Acclamator stored its FBX axis correction on the UnitSpawnView root, which runtime station-facing rotation overwrote; moving that correction to mesh children preserves it. Real ships used hardcoded StartRotation and a +X hyperspace offset, so a cached StationFacingService now supplies both previews and ShipMoveComponent, while ShipMoveModel derives the jump entry vector from the cached rotation.

## Source Nodes

- AcclamatorReinforcementView
- UnitSpawnView
- ReinforcementService
- StationFacingService
- ShipMoveModel
- ShipMoveComponent
- ShipMovementTweenPlayer