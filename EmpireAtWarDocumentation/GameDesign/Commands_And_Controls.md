---
type: design-reference
system:
  - commands
  - controls
  - selection
design_state: implemented
verified: 2026-10-20
verification: source-and-assets
---
# Commands and Controls

## Goal

- Current player selection, orders and default keyboard/mouse bindings.
- Bindings below come from the project input asset and generated `GameInputActions`; saved player rebinding can override them.

## Rules

### Selection

- Left-click selects an entity; own selection and other-player inspection use separate contexts.
- Input beginning over UI does not start world selection.
- Mouse drag starts at a distance of at least 5 screen pixels; releasing applies the selection rectangle.
- Marquee selects local-player entities whose projected positions fall inside it; objects behind the camera are excluded.
- If a marquee contains ships/squadrons, select those units only. Select structures only when no unit is inside.
- Double-click a local unit → select matching unit type on screen, for the same owner. This covers ships and squadrons.
- Slow repeated clicks in the same spot cycle overlapping entities. Squadron icons take priority; units precede structures on the ray.
- Ctrl+A selects local units on screen; Ctrl+Shift+A selects all living local units. Neither shortcut selects structures.
- Selection replacement removes prior entries; removed entities are removed from selection.
- Enemy selection/raycast hits hidden by fog are excluded; visibility rules are in [[GameDesign/Visibility]].

### Orders

- Right-click release on empty space → Move. Right-click a hostile entity → Attack.
- A hovered hardpoint marker wins over the hull hit; receivers supporting hardpoint targeting attack that hardpoint.
- Receivers without hardpoint targeting, such as squadrons, fall back to attacking the whole target.
- Orders apply to living locally selected receivers that expose the corresponding command capability.
- Move, Attack Move and Guard use compact size-aware destination slots; ordinary movement preserves each receiver's height.
- Right-click on another entity or an obstacle does not place an empty-space movement/waypoint target.
- Attack Move engages threats on the route; Guard follows a friendly/allied target and returns after engagements.
- Stop clears the current order and stops movement. Hunt seeks enemies automatically. Retreat navigates toward the station-side retreat destination.
- Guard cannot target the receiver itself.
- Action buttons are available only when at least one selected receiver supports that action; battle end disables them.
- Move is the default right-click order and has no action-panel button.
- Each ship has one current order; `ShipOrderRunner` coordinates order/state transitions.

### Waypoints and targeting

- Hold Alt and right-click empty positions to place waypoints; releasing Alt submits the route.
- Alternatively choose Waypoint Move, right-click successive positions, then press the same command button again to submit.
- Escape cancels a pending command target; selecting a new local selection cancels pending targeting.
- Attack/Guard/Attack Move buttons enter target selection. Stop/Hunt/Retreat execute immediately.
- Ability targeting cancels order targeting; command targeting cancels ability targeting.
- Group waypoints retain formation offsets relative to the first point.
- [[GameDesign/Abilities_And_Superweapons]] describes ability targeting; [[GameDesign/Combat]] describes engagement.

## Important Values

| Default input | Action |
|---|---|
| Left mouse | Select / drag selection |
| Right mouse release | Context order / submit command target |
| Ctrl+A | Select on-screen local units |
| Ctrl+Shift+A | Select all local units |
| Alt + right-click; release Alt | Queue then submit waypoints |
| W/A/S/D or arrow keys | Camera pan |
| Q / E | Camera pan left / right |
| R / F; mouse wheel | Camera zoom axis; wheel zoom |
| Middle-mouse drag | Camera drag pan |
| Escape | Cancel focused UI/targeting flow |
| F5 / F9 | Quicksave / quickload in supported battle modes |

- Camera edge-scroll band: 16 px; enabled by the saved Edge Scrolling preference and only while focused with a known pointer position.
- Ctrl+A/Ctrl+Shift+A suppress the camera's A-key movement while those selection shortcuts are pressed.
- Order settings: Guard follow radius 40 world units; Guard chase distance 240; Hunt retarget interval 1 s; station retreat clearance 150 world units.
- Pause and 1×/4× speed are UI actions; the current input asset does not bind Space, A, S or G to pause/attack/stop/guard.

## Edge Cases

- This is the clone's binding set. External EaW/RaW controls in reference notes do not define these bindings.
- A repeated click is not always a double-click: slow repeats cycle overlaps; actual double-click selects matching type.
- Allied units can be Guard targets; the player orders their own selection rather than taking command of allied units.
- Default bindings and serialized order values were read on 2026-10-20. No interactive input or movement test ran for this note.
- Save restrictions: [[GameDesign/Save_And_Load]]. Mode-specific controls: [[GameDesign/Modes_And_Victory]].

## Files

- `Assets/Settings/Input/EmpireAtWar.inputactions`; `Assets/Scripts/Services/Input/GameInputActions.cs` — default bindings.
- `Assets/Scripts/Services/Input/PointerGestures.cs`, `Assets/Scripts/Services/Input/MouseDragState.cs` — click/drag and command events.
- `Assets/Scripts/Services/Selection/SelectionService.cs`, `Assets/Scripts/Services/Selection/SelectionQuery.cs`, `Assets/Scripts/Services/Selection/SelectionInput.cs`.
- `Assets/Scripts/Services/Camera/CameraInput.cs` — camera controls and edge scrolling.
- `Assets/Scripts/Services/UnitOrders/PlayerOrderInputHandler.cs`, `Assets/Scripts/Services/UnitOrders/UnitOrderService.cs`.
- `Assets/Scripts/Entities/UnitActions/Controller/UnitActionsPresenter.cs` — command availability and targeting.
- `Assets/Scripts/Entities/Ship/Orders/ShipOrderRunner.cs` — ship order lifecycle.
- `Assets/Settings/UnitOrderSettings.asset` — Guard/Hunt/retreat values.
- [[GameDesign/00_GameDesign]].
