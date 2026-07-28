---
type: "query"
date: "2026-07-28T09:54:26.273355+00:00"
question: "How do ship movement, Bezier avoidance routes, obstacle size or colliders, and turn-rate-limited rotation interact when routes change?"
contributor: "graphify"
source_nodes: ["Ship", "Rotation"]
---

# Q: How do ship movement, Bezier avoidance routes, obstacle size or colliders, and turn-rate-limited rotation interact when routes change?

## Answer

The active Bezier route must be checked from the ship current progress against collider-inflated contacts. Detour clearance must include both collider footprint and the minimum turn radius derived from speed and angular rate. Replanning should occur only when the remaining curved route is unsafe, while hull and bank rotation remain angular-speed limited.

## Source Nodes

- Ship
- Rotation