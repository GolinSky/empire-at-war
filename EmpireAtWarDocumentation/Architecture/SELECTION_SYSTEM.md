---
type: technical-reference
system:
  - selection
  - input
verified: 2026-10-20
verification: source
---
# Selection System

## Rules

- Player-facing bindings and order semantics: [[GameDesign/Commands_And_Controls]].
- `SelectionService` applies selection policy; `SelectionQuery` owns world/visibility queries; `SelectionContext` owns the selected set and primary entity.
- Double-click selects the same ship or squadron type for the same local owner on screen; it does not select every off-screen instance.
- Slow repeated clicks can cycle overlapping objects. This is distinct from double-click same-type selection.
- Mouse marquee starts at a drag distance `>= 5` screen pixels. A rectangle containing units prefers them; structures are selected only when no units are inside.
- Ctrl+A selects visible player units; Ctrl+Shift+A selects all player units.
- Orders flow through `UnitOrderService` and entity facades such as `IMoveFacade`; the historical `IMoveCommand` description is obsolete.

## Implementation

- Marquee model owns drag/rectangle state; its view renders; its presenter connects input to selection.
- Left-button drag selects; middle-button drag pans the camera. Input beginning over UI does not initiate world selection.
- Default move commands use compact size-aware destinations at each unit's height; full controls and queue behavior are documented in GameDesign.
- Source review on 2026-10-20; no Unity tests or interaction checks were run for this note update.

## Files

- `Assets/Scripts/Services/Selection/SelectionService.cs`
- `Assets/Scripts/Services/Selection/SelectionQuery.cs`
- `Assets/Scripts/Services/Selection/SelectionContext.cs`
- `Assets/Scripts/Services/Input/MouseDragState.cs`
- `Assets/Scripts/Services/Camera/CameraInput.cs`; `Assets/Settings/Input/EmpireAtWar.inputactions` — camera drag action and middle-button binding.
- `Assets/Scripts/Services/UnitOrders/UnitOrderService.cs`
- `Assets/Scripts/Entities/BaseEntity/EntityFacades/IMoveFacade.cs`
- `Assets/Scripts/Components/Selection/Marquee/`
- [[GameDesign/Commands_And_Controls]]
- [[Architecture/00_Architecture]]
