---
type: "query"
date: "2026-07-28T10:27:58.115529+00:00"
question: "How does ShipMoveComponent build a direct Bezier route for a destination behind the ship, and where is avoidance integrated?"
contributor: "graphify"
source_nodes: ["ShipMoveComponent", "ShipNavigationService", "ShipBezierPath"]
---

# Q: How does ShipMoveComponent build a direct Bezier route for a destination behind the ship, and where is avoidance integrated?

## Answer

The graph traversal was too noisy to establish reliable project-local edges. Direct source inspection showed that ShipMoveComponent delegates route creation to ShipNavigationService and previously replanned on radar contacts; ShipNavigationService integrated obstacle detours, occupied-destination resolution, traffic reservations, and waits. The movement-only implementation now ignores contacts and reservations, returns zero wait/detour, and uses ShipBezierPath to create a two-segment forward-moving lateral turnaround when the destination is behind.

## Source Nodes

- ShipMoveComponent
- ShipNavigationService
- ShipBezierPath