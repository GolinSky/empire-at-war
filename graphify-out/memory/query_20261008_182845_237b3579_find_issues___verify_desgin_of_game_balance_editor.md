---
type: "query"
date: "2026-10-08T18:28:45.961416+00:00"
question: "find issues - verify desgin of game balance editor we made"
contributor: "graphify"
outcome: "useful"
source_nodes: ["BalanceDraftTests", "BalancePersistenceTests"]
---

# Q: find issues - verify desgin of game balance editor we made

## Answer

Graph terms Balance Draft Preset located existing acceptance tests. Live Unity and Serena review found: missing weapon profiles 16/17/18 throw in BalanceUnitView.BaseDps; prefab override application changes BalanceField.Schema and prevents reloading a preset saved before Apply (reproduced on temporary fixture); weapon assignment drafts leave static users and comparison profiles stale; long roster names clip pin buttons at default 255px; Compare omits owning prefab mount fields and hides profile context from row labels. Existing balance tests 24/24 passed. No fixes applied.

## Outcome

- Signal: useful

## Source Nodes

- BalanceDraftTests
- BalancePersistenceTests