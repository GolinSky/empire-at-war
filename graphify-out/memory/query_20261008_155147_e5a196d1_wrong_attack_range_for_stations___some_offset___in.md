---
type: "query"
date: "2026-10-08T15:51:47.075215+00:00"
question: "wrong attack range for stations - some offset + increase range - it is too small now compare to other ships"
contributor: "graphify"
outcome: "useful"
source_nodes: ["SpaceStationData", "WeaponComponent", "EntityComponentData"]
---

# Q: wrong attack range for stations - some offset + increase range - it is too small now compare to other ships

## Answer

Expanded from graph vocabulary: station range radar attack weapon. Live source verification: SpaceStationData shared WeaponRange was 187.5, compared with normal ship ranges 500-937.5; raised to 900 to match existing radar and vision. WeaponComponent debug attack ring used its offset child transform; now explicitly injects EntityBindType.ViewTransform and draws at the entity root. Both regression tests failed before changes and passed afterward. All 49 weapon-related EditMode tests passed. Unity saved the station asset and compilation completed without errors.

## Outcome

- Signal: useful

## Source Nodes

- SpaceStationData
- WeaponComponent
- EntityComponentData