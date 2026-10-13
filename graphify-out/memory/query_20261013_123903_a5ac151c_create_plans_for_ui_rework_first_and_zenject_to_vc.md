---
type: "query"
date: "2026-10-13T12:39:03.903936+00:00"
question: "Create plans for UI rework first and Zenject to VContainer migration using SoulsLikeTemplate examples; audit Resolve and hierarchy injection."
contributor: "graphify"
outcome: "useful"
source_nodes: ["EntityFeatureBindings", "Entity", "UiService", "UiFactory"]
---

# Q: Create plans for UI rework first and Zenject to VContainer migration using SoulsLikeTemplate examples; audit Resolve and hierarchy injection.

## Answer

Expanded against graph labels using entity feature bindings. Graph EntityFeatureBindings at Assets/Scripts/Entities/BaseEntity/EntityFeatureBindings.cs:L21 points to selection, health, radar, weapon, combat and fog registration methods. Verified live source: three direct DI Resolve calls, sixteen hierarchy binding sites, one injectable-descendant helper and two FromComponentOn rebinding sites. Saved separate UI_Service_Refactoring and Zenject_To_VContainer_Migration plans with research in TODOs/Refactoring. UI first: concrete typed views, presenter interfaces, feature colocation, no UiType. Migration preserves player/entity ownership, lifecycle and pooling with explicit injection.

## Outcome

- Signal: useful

## Source Nodes

- EntityFeatureBindings
- Entity
- UiService
- UiFactory