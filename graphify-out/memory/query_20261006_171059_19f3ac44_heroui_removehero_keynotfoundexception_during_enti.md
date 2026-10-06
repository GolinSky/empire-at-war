---
type: "query"
date: "2026-10-06T17:10:59.946621+00:00"
question: "HeroUi.RemoveHero KeyNotFoundException during Entity.LateDispose"
contributor: "graphify"
outcome: "useful"
source_nodes: ["HeroUi", "HeroUiController", "Entity"]
---

# Q: HeroUi.RemoveHero KeyNotFoundException during Entity.LateDispose

## Answer

Expanded graph vocabulary: hero ui entity remove dispose. Verified against live source: HeroUi.OnDestroy disposes and clears icons while the controller remains subscribed. Entity GameObjectContext kernels dispose independently. HeroUi.Disposed now triggers idempotent controller cleanup immediately; the controller also uses IDisposable for early Zenject cleanup. A regression reproduced KeyNotFoundException; both parent/entity container teardown orders now pass.

## Outcome

- Signal: useful

## Source Nodes

- HeroUi
- HeroUiController
- Entity