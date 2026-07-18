---
type: "query"
date: "2026-07-12T14:50:34.984567+00:00"
question: "How does selecting player ships open ShipUi move-to-position canvas and send the move command to selected ships?"
contributor: "graphify"
source_nodes: ["ShipUiController", "PlayerShipCommand", "Ship"]
---

# Q: How does selecting player ships open ShipUi move-to-position canvas and send the move command to selected ships?

## Answer

ShipUiController requests Entities.BaseEntity.EntityCommands.IMoveCommand from the selected player entity. PlayerShipCommand previously implemented only IShipCommand, so Entity.TryGetCommand<IMoveCommand> returned false. PlayerShipCommand now implements the entity IMoveCommand and forwards MoveTo(Vector2) to Ship.MoveTo, restoring the ShipUi move-position action.

## Source Nodes

- ShipUiController
- PlayerShipCommand
- Ship