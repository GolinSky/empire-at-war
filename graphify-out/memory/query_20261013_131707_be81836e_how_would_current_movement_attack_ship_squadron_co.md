---
type: "query"
date: "2026-10-13T13:17:07.361761+00:00"
question: "How would current movement attack ship squadron code and DI change with Unity DOTS and PlayFab plus Mirror multiplayer; identify issues"
contributor: "graphify"
outcome: "useful"
source_nodes: ["Ship", "Squadron", "ShipInstaller", "Entity", "UnitOrderService", "CombatAttackCoordinator"]
---

# Q: How would current movement attack ship squadron code and DI change with Unity DOTS and PlayFab plus Mirror multiplayer; identify issues

## Answer

Expanded vocabulary: Ship ShipOrderRunner Squadron ShipInstaller Entity UnitOrderService ShipMoveComponent CombatAttackCoordinator. Verified live source: movement owns Transform state; Ship and Squadron tick with frame delta; combat uses Time.time and frameCount with presenter references; ShipInstaller combines local-player presentation and simulation dependencies. UnitOrderService executes facades before publishing and UnitOrder lacks receiver IDs and contains managed target and waypoint references. ECS migration requires unmanaged simulation data, explicit system ordering, and presentation bridges. Keep DI at application and bridge boundaries. Mirror custom messages can bridge ECS but require explicit replication, authoritative IDs, visibility filtering and snapshot lifecycle. PlayFab handles backend matchmaking and optional server allocation, not battle simulation. Existing radar and targeting jobs mean performance benefit requires profiling. This is architectural advice, not measured performance or implemented migration.

## Outcome

- Signal: useful

## Source Nodes

- Ship
- Squadron
- ShipInstaller
- Entity
- UnitOrderService
- CombatAttackCoordinator