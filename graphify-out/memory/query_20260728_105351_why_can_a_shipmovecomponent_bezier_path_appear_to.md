---
type: "query"
date: "2026-07-28T10:53:51.607278+00:00"
question: "Why can a ShipMoveComponent Bezier path appear to change during movement, and how are route position and ship rotation updated?"
contributor: "graphify"
source_nodes: ["ShipMoveComponent", "AttackTargetState", "ShipBezierPath", "ShipRotationKinematics"]
---

# Q: Why can a ShipMoveComponent Bezier path appear to change during movement, and how are route position and ship rotation updated?

## Answer

The graph traversal did not provide reliable project-local edges. Direct inspection found two concrete causes: ShipMoveComponent rewrote the LineRenderer every tween update to display only the remaining path, and AttackTargetState issued a new MoveToPosition command every 0.5 seconds, killing and rebuilding the active tween. The fix keeps the rendered route unchanged, allows pursuit to request a new destination only after the current route finishes, uses two quarter-circle Bezier segments for a true half-circle turnaround, and derives hull bank from requested turn rate while angular rotation remains capped.

## Source Nodes

- ShipMoveComponent
- AttackTargetState
- ShipBezierPath
- ShipRotationKinematics