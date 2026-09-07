---
type: "query"
date: "2026-07-31T10:04:39.884425+00:00"
question: "How does defend platform reinforcement preview position flow through camera projection and DefendPlatformFacade creation?"
contributor: "graphify"
source_nodes: ["DefendPlatformFacade", "ReinforcementController", "PlaceholderFactory"]
---

# Q: How does defend platform reinforcement preview position flow through camera projection and DefendPlatformFacade creation?

## Answer

The graph links ReinforcementController with DefendPlatformFacade and PlaceholderFactory, but it does not contain the source-level edges needed to trace camera projection or application of the injected spawn position; direct source inspection was required.

## Source Nodes

- DefendPlatformFacade
- ReinforcementController
- PlaceholderFactory