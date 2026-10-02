---
type: "query"
date: "2026-10-02T17:31:45.731614+00:00"
question: "enemy spawn defend platform and spawnable mining facilities inside their station - fix it - how this happened - maybe station has wrong colliders - fix the root issues - not the symptoms"
contributor: "graphify"
outcome: "useful"
source_nodes: ["EnemyStructurePlacementService", "SpaceStation", "ReinforcementZonesSystem"]
---

# Q: enemy spawn defend platform and spawnable mining facilities inside their station - fix it - how this happened - maybe station has wrong colliders - fix the root issues - not the symptoms

## Answer

Expanded graph vocabulary: EnemyStructurePlacementService, SpaceStation, DefendPlatform, MiningFacility, ReinforcementZonesSystem. Live source and prefab inspection showed undersized offset station BoxColliders; the 2026-09-27 unit scale increase left enemy STRUCTURE_CLEARANCE=16 and search radii 64..88 stale. Fitted both station BoxColliders to their referenced hull renderers; set measured station map reservations to Republic 396 and Separatist 261. Enemy placement now receives explicit station and structure prefab colliders and derives scaled rotated bounds and clearance, starts rings outside the station hull center, and uses actual captured-zone bounds (radius 270) for fallback. Unity compilation completed without errors, saved collider geometry read back correctly, and a read-only snapshot of the running Battle showed one defense platform and three mining facilities with no station footprint overlap (closest gap about 67 units). Regression tests added for scale, offset, and zone radius but not executed per repository policy.

## Outcome

- Signal: useful

## Source Nodes

- EnemyStructurePlacementService
- SpaceStation
- ReinforcementZonesSystem