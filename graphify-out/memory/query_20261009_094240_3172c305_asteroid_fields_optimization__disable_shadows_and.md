---
type: "query"
date: "2026-10-09T09:42:40.638159+00:00"
question: "asteroid fields optimization: disable shadows and check whether asteroids are static"
contributor: "graphify"
outcome: "useful"
source_nodes: ["AsteroidFieldBuilder", "MapLayoutView"]
---

# Q: asteroid fields optimization: disable shadows and check whether asteroids are static

## Answer

Expanded query vocabulary: AsteroidField, AsteroidFieldBuilder, RockScatter, MapLayoutView. Verified live source: MapLayoutView.BuildAsync creates fields at runtime; SpawnRock sets the transform once. The four field visual prefabs have only Transform, MeshFilter, and MeshRenderer; no movement or physics components, and isStatic=false. Disabled shadow casting on AsteroidRockLarge.prefab and AsteroidRockMedium.prefab; AsteroidDebrisA/B already had casting off. Kept shared material shadow reception and static flags unchanged. Unity reimported and saved both edited prefabs, and inspection confirmed shadow casting Off on all four with no new console errors. Both shared materials already enable instancing; actual batching/performance was not profiled. Runtime static batching needs explicit Combine and additional memory; the Static checkbox alone is not a runtime batching solution.

## Outcome

- Signal: useful

## Source Nodes

- AsteroidFieldBuilder
- MapLayoutView