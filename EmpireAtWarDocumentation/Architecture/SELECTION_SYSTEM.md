---
type: engineering-reference
system:
  - selection
  - input
verified: 2026-11-16
verification: Reviewed live selection query and depth classification source and checked the linked controls reference.
---
# Selection System

## Rules

- Player-facing bindings and order semantics: [[GameDesign/Commands_And_Controls]].
- `SelectionService` applies selection policy; `SelectionQuery` owns world/visibility queries; `SelectionContext` owns the selected set and primary entity.
- Orders flow through `UnitOrderService` and entity facades such as `IMoveFacade`.

- Hull point ranking: a unit hit behind a nearer station and below its `IStationHullFacade.HullBottom` loses unit priority. Classify after collection, then sort by category/distance so comparisons remain transitive.
- Units at/above the station bottom, in front of it, or on exposed hull retain unit priority. Marker priority and marquee behavior are unchanged by this depth rule.
- Depth ranking source reviewed 2026-11-14; regressions authored, not run. Owner → [[Done/Bugs/Executor_Depth_And_Big_Ship_Targeting_Fix]].

## Implementation

- Marquee model owns drag/rectangle state; its view renders; its presenter connects input to selection.
- `PointerGestures` uses `MouseDragState.StartedOverUi` to suppress world gestures for presses beginning over UI.

## Files

- `Assets/Scripts/Services/Selection/SelectionService.cs`
- `Assets/Scripts/Services/Selection/SelectionQuery.cs`
- `Assets/Scripts/Services/Selection/SelectionContext.cs`
- `Assets/Scripts/Services/Input/PointerGestures.cs` and `Assets/Scripts/Services/Input/MouseDragState.cs`
- `Assets/Scripts/Services/Camera/CameraInput.cs`; `Assets/Settings/Input/EmpireAtWar.inputactions` — camera drag action and middle-button binding.
- `Assets/Scripts/Services/UnitOrders/UnitOrderService.cs`
- `Assets/Scripts/Entities/BaseEntity/EntityFacades/IMoveFacade.cs`
- `Assets/Scripts/Components/Selection/Marquee/`
- [[GameDesign/Commands_And_Controls]]
- [[Architecture/00_Architecture]]
