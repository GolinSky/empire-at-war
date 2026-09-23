# Selection System

The selection system now supports selecting and commanding multiple player ships while preserving the existing single-selection API for older consumers.

## Player Features

- **Single selection:** click or tap a selectable unit.
- **Ship-type selection:** activate the same player ship twice to select every player-owned ship of that `ShipType`.
- **Marquee selection:** drag the mouse more than 5 screen pixels to draw a selection rectangle. Releasing selects all visible player units whose world positions project inside the rectangle.
- **Group movement:** the ship UI move command is sent to every selected entity that provides an `IMoveCommand`.
- **Safe updates:** replacing a selection deselects removed units, avoids duplicates, and automatically removes destroyed entities.

Mouse dragging is reserved for marquee selection. Touch dragging continues to pan the camera, and input that begins over UI does not start selection.

## Design

The reusable marquee feature lives in `Assets/Scripts/Components/Selection/Marquee` and follows MVP:

- `MarqueeSelectionModel` owns drag state and normalized rectangle data without Unity dependencies.
- `IMarqueeSelectionView` / `MarqueeSelectionView` render the box only.
- `MarqueeSelectionPresenter` translates input drag events into model and view updates.
- `MarqueeSelectionUtility` is a generic, pure C# bounding-box filter that can be reused by other systems.

Selection-specific world queries remain in `SelectionQuery`, while `SelectionService` applies click, repeated-tap, and marquee policies. `SelectionContext` exposes the full `Entities` collection plus the first `Entity` as a backward-compatible primary selection.

## Verification

EditMode tests cover rectangle normalization, boundary inclusion, reverse-direction dragging, and model lifecycle behavior in `Assets/Scripts/Tests/Editor/MarqueeSelectionUtilityTests.cs`.
